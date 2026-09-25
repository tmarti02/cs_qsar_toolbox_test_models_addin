using System.Collections.Generic;

namespace EpaTestApp.Results
{
    public class ChemicalPrediction
    {
        public string? version { get; set; }
        public string? CAS { get; set; }
        public string? Smiles { get; set; }
        public string? imageURL { get; set; }
        public string? error { get; set; }
        public string? method { get; set; }
        public string? endpoint { get; set; }
        public bool isBinaryEndpoint { get; set; }
        public bool isLogMolarEndpoint { get; set; }

        public PredictionResultsPrimaryTable? predictionResultsPrimaryTable { get; set; }
        public IndividualPredictionsForConsensus? individualPredictionsForConsensus { get; set; }
        public List<SimilarChemicalsGroup>? similarChemicals { get; set; }

        public object? moaTable { get; set; }
        public int imgSize { get; set; }
        public string? webImagePathByCID { get; set; }
        public string? webImagePathBySID { get; set; }
        public string? webPathDashboardPage { get; set; }
        public double SCmin { get; set; }
        public bool createDetailedReport { get; set; }
        public HmStats? hmStats { get; set; }
    }

    public class PredictionResultsPrimaryTable
    {
        public string? source { get; set; }
        public string? endpointSubscripted { get; set; }
        public string? expCAS { get; set; }
        public string? expSet { get; set; }
        public bool writePredictionInterval { get; set; }
        public double? expToxValue { get; set; }
        public double? predToxValue { get; set; }
        public double? expToxValMass { get; set; }
        public double? predToxValMass { get; set; }
        public string? molarLogUnits { get; set; }
        public string? massUnits { get; set; }
        public string? predictedValueSuperscript { get; set; }
        public string? predictedValueNote { get; set; }
    }

    public class IndividualPredictionsForConsensus
    {
        public List<ConsensusPrediction>? consensusPredictions { get; set; }
        public string? units { get; set; }
    }

    public class ConsensusPrediction
    {
        public string? method { get; set; }
        public double prediction { get; set; }
    }

    public class SimilarChemicalsGroup
    {
        public string? similarChemicalsSet { get; set; }
        public int? similarChemicalsCount { get; set; }
        public string? units { get; set; }
        public double expVal { get; set; }
        public double predVal { get; set; }
        public List<SimilarChemicalItem>? similarChemicalsList { get; set; }
        public ExternalPredChart? externalPredChart { get; set; }
    }

    public class SimilarChemicalItem
    {
        public string ?DSSTOXSID { get; set; }
        public string ?DSSTOXCID { get; set; }
        public string? CAS { get; set; }
        public string? preferredName { get; set; }
        public string? backgroundColor { get; set; }
        public string? similarityCoefficient { get; set; }
        public double? expVal { get; set; }
        public double? predVal { get; set; }
        public string? imageUrl { get; set; }
        public double? MAEEntireTestSet { get; set; }
        public double? MAE { get; set; }
    }

    public class ExternalPredChart
    {
        public string? externalPredChartImageSrc { get; set; }
    }

    public class HmStats
    {
        public double MAE_Test { get; set; }
        public double Q2_F3_Test { get; set; }
        public double R2_Training { get; set; }
        public double PearsonRSQ_Test { get; set; }
        public double MAE_Training { get; set; }
        public double Coverage_Test { get; set; }
        public double RMSE_Test { get; set; }
        public double PearsonRSQ_Training { get; set; }
        public double RMSE_Training { get; set; }
        public double Coverage_Training { get; set; }
        public double Q2_Test { get; set; }
    }
}