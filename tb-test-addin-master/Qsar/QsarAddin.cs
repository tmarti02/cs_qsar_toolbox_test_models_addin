using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Toolbox.Docking.Api.Control;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Calculator;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using EpaTestApp.Results;

namespace TestAddins.Qsar
{
    public class QsarAddin : ITbQsar, ITbCalculator, ITbObject, IDisposable, ITbObjectDomain
    {
        private readonly TbScale ScaleDeclaration;
        private readonly TbObjectId objectId;
        private readonly Dictionary<string, string> Modelinfo;

        private string _originalUnits = string.Empty;

        private Dictionary<string, string>? CurModelPrediction;
        public ChemicalPrediction? LastChemicalPrediction { get; private set; }

        private readonly string TEST_APP = "WebTEST.jar";

        public string OriginalUnits
        {
            get => _originalUnits;
            set => _originalUnits = value ?? string.Empty;
        }

        public QsarAddin(Dictionary<string, string> modelinfo, TbScale scaleDeclaration, TbObjectId objectId)
        {
            Modelinfo = modelinfo ?? throw new ArgumentNullException(nameof(modelinfo));
            ScaleDeclaration = scaleDeclaration;
            this.objectId = objectId;
        }

        public void Dispose()
        {
        }

        public TbScalarData Calculate(ITbBasket target)
        {
            target.WorkTask?.TbToken.ThrowIfCancellationRequested();

            LastChemicalPrediction = RunTestModel(
                Modelinfo["tag"],
                target.Chemical.Smiles)
                .GetAwaiter()
                .GetResult();

            CurModelPrediction = ToPredictionDictionary(LastChemicalPrediction);
            PrintPredictionDictionary();

            return Utilities.ConvertData(CurModelPrediction, ScaleDeclaration, Modelinfo);
        }

        public ITbPrediction Predict(ITbBasket target)
        {
            string smiles = target.Chemical.Smiles;

            target.WorkTask?.TbToken.ThrowIfCancellationRequested();

            LastChemicalPrediction = RunTestModel(Modelinfo["tag"], target.Chemical.Smiles)
                .GetAwaiter()
                .GetResult();

            CurModelPrediction = ToPredictionDictionary(LastChemicalPrediction);
            PrintPredictionDictionary();

            TbData predictedTbData = (TbData)Utilities.ConvertData(CurModelPrediction, ScaleDeclaration, Modelinfo);

            Dictionary<string, string> MetadataString = new Dictionary<string, string>();

            string TestPred = string.Empty;
            if (CurModelPrediction.TryGetValue("PredictionClass", out string? predictionClass) &&
                !string.IsNullOrWhiteSpace(predictionClass))
            {
                TestPred = predictionClass;
            }
            else if (CurModelPrediction.TryGetValue("Prediction", out string? prediction) &&
                     !string.IsNullOrEmpty(prediction))
            {
                TestPred = prediction + " " +
                    (CurModelPrediction.TryGetValue("Units", out string? units) ? units ?? string.Empty : string.Empty);
            }
            else
            {
                TestPred = "N/A";
            }

            MetadataString.Add("TEST original prediction", TestPred);

            if (CurModelPrediction.TryGetValue("ExperimentalClass", out string? experimentalClass) &&
                !string.IsNullOrWhiteSpace(experimentalClass))
            {
                MetadataString["TEST experimental (if found)"] = experimentalClass;
            }

            if (CurModelPrediction.TryGetValue("Experimental", out string? experimental) &&
                !string.IsNullOrWhiteSpace(experimental))
            {
                MetadataString["TEST experimental (if found)"] =
                    experimental + " " +
                    (CurModelPrediction.TryGetValue("Units", out string? units) ? units ?? string.Empty : string.Empty);
            }

            Console.WriteLine(JsonSerializer.Serialize(MetadataString, new JsonSerializerOptions { WriteIndented = true }));

            Dictionary<string, TbData> MetadataValue = new Dictionary<string, TbData>();

            TbMetadata metadata = new TbMetadata(
                (IReadOnlyDictionary<string, string>)MetadataString,
                (IReadOnlyDictionary<string, TbData>)MetadataValue);

            TbData Mockdescriptordata = new TbData(
                new TbUnit(TbScale.EmptyRatioScale.FamilyGroup, TbScale.EmptyRatioScale.BaseUnit),
                null);

            Dictionary<TbObjectId, TbData> matrixdescriptorvalues = new Dictionary<TbObjectId, TbData>()
            {
                { objectId, Mockdescriptordata }
            };

            return new PredictionAddin(predictedTbData, metadata, matrixdescriptorvalues, null);        
        }

        public bool IsRelevantToChemical(ITbBasket target, out string reason)
        {
            if (CurModelPrediction == null)
            {
                LastChemicalPrediction = RunTestModel(Modelinfo["tag"], target.Chemical.Smiles)
                    .GetAwaiter()
                    .GetResult();
                CurModelPrediction = ToPredictionDictionary(LastChemicalPrediction);
            }

            if (CurModelPrediction.TryGetValue("valid", out string? valid) && valid == "false")
            {
                reason = CurModelPrediction.TryGetValue("ErrorMessage", out string? errorMessage)
                    ? (errorMessage ?? string.Empty)
                    : string.Empty;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        public TbDomainStatus CheckDomain(ITbBasket target)
        {
            if (CurModelPrediction == null)
            {
                LastChemicalPrediction = RunTestModel(Modelinfo["tag"], target.Chemical.Smiles)
                    .GetAwaiter()
                    .GetResult();
                CurModelPrediction = ToPredictionDictionary(LastChemicalPrediction);
            }

            if (CurModelPrediction.TryGetValue("valid", out string? valid) && valid == "false")
                return TbDomainStatus.OutOfDomain;

            return TbDomainStatus.InDomain;
        }

        public async Task<ChemicalPrediction> RunTestModel(string TAG, string SMI)
        {
            if (!Utilities.ensureAPI_is_running(TEST_APP).GetAwaiter().GetResult())
                throw new InvalidOperationException("The WebTEST API is not ready.");

            string baseUrl = "http://localhost:8081/predictGet";
            string endpointAbbrev = Modelinfo["EndpointAbbreviation"];
            string casrn = "N/A";

            string url = $"{baseUrl}?smiles={Uri.EscapeDataString(SMI)}&casrn={Uri.EscapeDataString(casrn)}&endpointAbbrev={Uri.EscapeDataString(endpointAbbrev)}";

            using var client = new HttpClient();
            string json = await client.GetStringAsync(url);

            JsonSerializerOptions jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
                WriteIndented = true
            };

            using JsonDocument jsonDocument = JsonDocument.Parse(json);

            ChemicalPrediction prediction = DeserializePrediction(jsonDocument.RootElement, jsonOptions)
                ?? throw new InvalidOperationException("The prediction API returned an empty prediction.");

            return prediction;
        }

        private Dictionary<string, string> ToPredictionDictionary(ChemicalPrediction prediction)
        {
            Dictionary<string, string> result = new Dictionary<string, string>
            {
                ["valid"] = "true",
                ["PredictionClass"] = string.Empty,
                ["Units"] = string.Empty,
                ["ErrorMessage"] = string.Empty
            };

            if (!string.IsNullOrEmpty(prediction.error))
            {
                result["valid"] = "false";
                result["ErrorMessage"] = prediction.error;
                return result;
            }

            if (prediction.predictionResultsPrimaryTable == null)
            {
                return result;
            }

            string[] classes = Modelinfo["Classes"].Replace("\"", "").Split(new[] { "|" }, StringSplitOptions.None);

            if (prediction.endpoint == "Ames Mutagenicity" || prediction.endpoint == "Developmental Toxicity")
            {
                if (prediction.predictionResultsPrimaryTable.predToxValue.HasValue)
                {
                    double predValue = prediction.predictionResultsPrimaryTable.predToxValue.Value;

                    result["PredictionClass"] = predValue < 0.5 ? classes[0] : classes[1];
                    result["Prediction"] = predValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }

                if (prediction.predictionResultsPrimaryTable.expToxValue.HasValue)
                {
                    double expValue = prediction.predictionResultsPrimaryTable.expToxValue.Value;

                    result["ExperimentalClass"] = expValue < 0.5 ? classes[0] : classes[1];
                    result["Experimental"] = expValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else
            {
                if (prediction.predictionResultsPrimaryTable.predToxValMass.HasValue)
                {
                    result["Prediction"] = prediction.predictionResultsPrimaryTable.predToxValMass.Value
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);
                    result["Units"] = prediction.predictionResultsPrimaryTable.massUnits ?? string.Empty;
                }

                if (prediction.predictionResultsPrimaryTable.expToxValMass.HasValue)
                {
                    result["Experimental"] = prediction.predictionResultsPrimaryTable.expToxValMass.Value
                        .ToString(System.Globalization.CultureInfo.InvariantCulture);
                    result["Units"] = prediction.predictionResultsPrimaryTable.massUnits ?? string.Empty;
                }
            }

            return result;
        }

        private void PrintPredictionDictionary()
        {
            Console.WriteLine(JsonSerializer.Serialize(
                CurModelPrediction,
                new JsonSerializerOptions { WriteIndented = true }));
        }

        private static ChemicalPrediction? DeserializePrediction(
            JsonElement root,
            JsonSerializerOptions options)
        {
            if (root.ValueKind == JsonValueKind.Object)
                return root.Deserialize<ChemicalPrediction>(options);

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in root.EnumerateArray())
                    return item.Deserialize<ChemicalPrediction>(options);
            }

            throw new InvalidOperationException(
                $"The prediction API returned JSON with root type '{root.ValueKind}', expected an object or array.");
        }
    }
}