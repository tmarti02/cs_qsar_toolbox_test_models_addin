using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Control;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;//

namespace TestAddins.Qsar
{
    public class TbQsarAddinFactory : ITbQsarFactory, ITbObjectFactory, ITbObjectFactoryDomain
    {
        private readonly Dictionary<string, string> Modelinfo;
        private string? _qmrflocation;
        private string _originalUnits = string.Empty;

        public TbObjectFlags Flags { get; } = TbObjectFlags.None;

        public QsarFlags QsarFlags => QsarFlags.None;

        public TbObjectId ObjectId { get; }

        public string ModelTag => Modelinfo["tag"];

        public IReadOnlyDictionary<string, string> ModelInfo => Modelinfo;

        public TbObjectAbout ObjectAbout { get; }

        public string ClientDomainExplainer => "TEST ADI";

        public TbMetadata Metadata { get; private set; }

        public string AgreementInfo => string.Empty;

        public string ReportDisclaimer => string.Empty;

        public IReadOnlyList<string> EndpointLocation { get; }

        public TbScale ScaleDeclaration { get; }

        public IReadOnlyList<QsarDescriptorInfo> XDescriptors { get; private set; } = Array.Empty<QsarDescriptorInfo>();

        public string? QmrfLocation
        {
            get => _qmrflocation;
            private set => _qmrflocation = value;
        }

        public TbQsarStatistics Statistics =>
            QsarAddinDefinitions.M4RatioModelStatistics(Modelinfo["tag"]);

        public TbQsarAddinFactory(Dictionary<string, string> modelinfo)
        {
            Modelinfo = modelinfo ?? throw new ArgumentNullException(nameof(modelinfo));

            ObjectId = new TbObjectId(
                "TEST - " + Modelinfo["Modelname"],
                new Guid(Modelinfo["Guid"]),
                new Version(Modelinfo["ModelVersion"]));

            ObjectAbout = QsarAddinDefinitions.GetM4ObjectAbout(Modelinfo);

            var endpointLocations = new List<string>();
            foreach (var key in new[] { "Endpoint location1", "Endpoint location2", "Endpoint location3", "Endpoint location4" })
            {
                if (Modelinfo.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
                    endpointLocations.Add(value);
            }
            EndpointLocation = endpointLocations;

            Metadata = BuildMetadata(Modelinfo);

            ScaleDeclaration = GetModelScale(Modelinfo);

            _originalUnits = Modelinfo.TryGetValue("TestUnit", out var testUnit) ? testUnit ?? string.Empty : string.Empty;
        }

        public bool InitFactory(IList<string> errorLog, out int? hash, ITbInitTask initTask)
        {
            if (Modelinfo.TryGetValue("QMRFlink", out var qmrflink) && !string.IsNullOrWhiteSpace(qmrflink))
            {
                QmrfLocation = Path.Combine(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                    "QMRF",
                    qmrflink);
            }
            else
            {
                QmrfLocation = string.Empty;
            }

            hash = null;
            return true;
        }

        public IReadOnlyList<ChemicalWithData> TrainingSet(ITbWorkTask task) =>
            QsarAddinDefinitions.GetSet(Modelinfo, ScaleDeclaration, "Training");

        public IReadOnlyList<ChemicalWithData> GetTestSet(ITbWorkTask task) =>
            QsarAddinDefinitions.GetSet(Modelinfo, ScaleDeclaration, "Test");

        public ITbQsar GetQsar(ITbWorkTask task)
        {
            var qsar = new QsarAddin(Modelinfo, ScaleDeclaration, ObjectId)
            {
                OriginalUnits = _originalUnits
            };
            return qsar;
        }

        public TbScale GetModelScale(Dictionary<string, string> modelinfo)
        {
            if (modelinfo.TryGetValue("TestUnit", out var testUnit) && string.IsNullOrEmpty(testUnit))
            {
                var ordinalScales = Utilities.GetOrdinalScales();

                if (modelinfo.TryGetValue("UnitFamily", out var family) &&
                    ordinalScales.TryGetValue(family, out var ordinalScale))
                {
                    return ordinalScale;
                }

                throw new KeyNotFoundException(
                    $"No ordinal scale found for unit family '{modelinfo["UnitFamily"]}'.");
            }

            string unitFamily = modelinfo["UnitFamily"];
            string tbUnit = modelinfo["TBUnit"];

            switch (unitFamily)
            {
                case "Temperature":
                    return new TbRatioScale(TbScale.Temperature, tbUnit);

                case "Time":
                    return new TbRatioScale(TbScale.Time, tbUnit);

                case "Bioaccumulation":
#pragma warning disable CS0612
                    return new TbRatioScale(TbScale.Bioaccumulation, tbUnit);
#pragma warning restore CS0612

                case "Pressure":
                    return new TbRatioScale(TbScale.Pressure, tbUnit);

                case "Molar concentration":
                    return new TbRatioScale(TbScale.MolarConcentration, tbUnit);

                case "Mass fraction":
                    return new TbRatioScale(TbScale.MassFraction, tbUnit);

                case "ConcentrationInBody_mass":
                    return new TbRatioScale(TbScale.ConcentrationInBody_mass, tbUnit);

                case "Mass concentration":
                    return new TbRatioScale(TbScale.MassConcentration, tbUnit);

                case "Administered dose(mass)":
                    return new TbRatioScale(TbScale.AdministeredDose_mass, tbUnit);

                case "Molality":
                    return new TbRatioScale(TbScale.Molality, tbUnit);

                case "Partition Coefficient":
                    return new TbRatioScale(TbScale.EmptyRatioScale, string.Empty);

                case "Surface tension":
                    return new TbRatioScale(Utilities.SurfaceTension, tbUnit);

                case "Viscosity":
                    return new TbRatioScale(Utilities.Viscosity, tbUnit);

                case "Thermal conductivity":
                    return new TbRatioScale(Utilities.ThermalConductivity, tbUnit);

                default:
                    return TbRatioScale.EmptyRatioScale;
            }
        }

        public IReadOnlyList<ISuportingChemicals> AdditionalLists(ITbWorkTask task) =>
            new List<ISuportingChemicals>();

        private static TbMetadata BuildMetadata(Dictionary<string, string> modelinfo)
        {
            var metaValues = QsarAddinDefinitions.getMetaDataValues(modelinfo);
            var metadataValues = new Dictionary<string, TbData>();

            if (modelinfo.TryGetValue("Duration(unit)", out var durationUnit) &&
                !string.IsNullOrWhiteSpace(durationUnit) &&
                modelinfo.TryGetValue("Duration(value)", out var durationValue) &&
                !string.IsNullOrWhiteSpace(durationValue))
            {
                metadataValues["Duration"] = new TbData(
                    new TbUnit(TbScale.Time.Name, durationUnit),
                    double.Parse(durationValue));
            }

            return new TbMetadata(
                (IReadOnlyDictionary<string, string>)metaValues,
                (IReadOnlyDictionary<string, TbData>)metadataValues);
        }
    }
}