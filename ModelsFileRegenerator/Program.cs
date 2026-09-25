using System.Text;
using System.Globalization;
using System.Text.Json;

// This script adds in GUID values to the testmodels.txt file, 
// and also retrieves training statistics from the Toolbox API for each model. 
// It then writes the updated information to a new output file.

var inputPath = args.Length > 0
    ? args[0]
    : Path.Combine("tb-test-addin-master", "Resources", "testmodels.txt");
var outputPath = args.Length > 1
    ? args[1]
    : Path.Combine("tb-test-addin-master", "Resources", "testmodels.txt");

const string apiBaseUrl = "http://localhost:8081";
const string defaultSmiles = "c1ccccc1";

var rows = LoadRows(inputPath, out var headers, out var guidColumnNames);
using var httpClient = new HttpClient();

foreach (var row in rows)
{
    if (row["UnitFamily"].Equals("Temperature", StringComparison.OrdinalIgnoreCase))
    {
        row["TestUnit"] = "°C";
        row["TBUnit"] = "°C";
    }

    var trainingStats = await GetTrainingStatsAsync(
        httpClient,
        apiBaseUrl,
        row["EndpointAbbreviation"],
        defaultSmiles);
    if (trainingStats.PearsonRsq.HasValue)
        row["R2(Train)"] = trainingStats.PearsonRsq.Value.ToString("G17", CultureInfo.InvariantCulture);
    else
        Console.WriteLine($"No PearsonRSQ_Training returned for '{row["tag"]}'; leaving R2(Train) unchanged.");
    if (trainingStats.Rmse.HasValue)
        row["RMSE(Train)"] = trainingStats.Rmse.Value.ToString("G17", CultureInfo.InvariantCulture);
    else
        Console.WriteLine($"No RMSE_Training returned for '{row["tag"]}'; leaving RMSE(Train) unchanged.");
    if (trainingStats.PearsonRsqTest.HasValue)
        row["R2(Test)"] = trainingStats.PearsonRsqTest.Value.ToString("G17", CultureInfo.InvariantCulture);
    else
        Console.WriteLine($"No PearsonRSQ_Test returned for '{row["tag"]}'; leaving R2(Test) unchanged.");
    if (trainingStats.RmseTest.HasValue)
        row["RMSE(Test)"] = trainingStats.RmseTest.Value.ToString("G17", CultureInfo.InvariantCulture);
    else
        Console.WriteLine($"No RMSE_Test returned for '{row["tag"]}'; leaving RMSE(Test) unchanged.");
    if (trainingStats.AccuracyTrain.HasValue)
        row["Balanced Accuracy(Train)"] = trainingStats.AccuracyTrain.Value.ToString("G17", CultureInfo.InvariantCulture);
    if (trainingStats.SpecificityTrain.HasValue)
        row["Specificity(Train)"] = trainingStats.SpecificityTrain.Value.ToString("G17", CultureInfo.InvariantCulture);
    if (trainingStats.SensitivityTrain.HasValue)
        row["Sensitivity(Train)"] = trainingStats.SensitivityTrain.Value.ToString("G17", CultureInfo.InvariantCulture);
    if (trainingStats.AccuracyTest.HasValue)
        row["Balanced Accuracy(Test)"] = trainingStats.AccuracyTest.Value.ToString("G17", CultureInfo.InvariantCulture);
    if (trainingStats.SpecificityTest.HasValue)
        row["Specificity(Test)"] = trainingStats.SpecificityTest.Value.ToString("G17", CultureInfo.InvariantCulture);
    if (trainingStats.SensitivityTest.HasValue)
        row["Sensitivity(Test)"] = trainingStats.SensitivityTest.Value.ToString("G17", CultureInfo.InvariantCulture);

    foreach (var guidColumnName in guidColumnNames)
        row[guidColumnName] = Guid.NewGuid().ToString();
}

WriteRows(outputPath, headers, rows);
Console.WriteLine($"Regenerated {rows.Count * guidColumnNames.Count} GUID values in '{outputPath}'.");

static async Task<(double? PearsonRsq, double? Rmse, double? PearsonRsqTest, double? RmseTest,
    double? AccuracyTrain, double? SpecificityTrain, double? SensitivityTrain,
    double? AccuracyTest, double? SpecificityTest, double? SensitivityTest)> GetTrainingStatsAsync(
    HttpClient httpClient,
    string apiBaseUrl,
    string endpointAbbreviation,
    string smiles)
{
    var url = $"{apiBaseUrl}/predictGet?smiles={Uri.EscapeDataString(smiles)}" +
              $"&casrn={Uri.EscapeDataString("N/A")}" +
              $"&endpointAbbrev={Uri.EscapeDataString(endpointAbbreviation)}";
    using var response = await httpClient.GetAsync(url);
    var json = await response.Content.ReadAsStringAsync();
    response.EnsureSuccessStatusCode();

    using var document = JsonDocument.Parse(json);
    var root = document.RootElement;
    if (root.ValueKind == JsonValueKind.Array)
        root = root.EnumerateArray().First();
    if (root.TryGetProperty("value", out var values) &&
        values.ValueKind == JsonValueKind.Array)
        root = values.EnumerateArray().First();

    if (!root.TryGetProperty("htStats", out var stats) &&
        !root.TryGetProperty("hmStats", out stats))
        return (null, null, null, null, null, null, null, null, null, null);

    double? rsq = null;
    if (stats.TryGetProperty("PearsonRSQ_Training", out var rsqValue) &&
        rsqValue.TryGetDouble(out var parsedRsq))
        rsq = parsedRsq;

    double? rmse = null;
    if (stats.TryGetProperty("RMSE_Training", out var rmseValue) &&
        rmseValue.TryGetDouble(out var parsedRmse))
        rmse = parsedRmse;

    double? rsqTest = null;
    if (stats.TryGetProperty("PearsonRSQ_Test", out var rsqTestValue) &&
        rsqTestValue.TryGetDouble(out var parsedRsqTest))
        rsqTest = parsedRsqTest;

    double? rmseTest = null;
    if (stats.TryGetProperty("RMSE_Test", out var rmseTestValue) &&
        rmseTestValue.TryGetDouble(out var parsedRmseTest))
        rmseTest = parsedRmseTest;

    double? accuracyTrain = GetStat(stats, "BA_Training");
    double? specificityTrain = GetStat(stats, "SP_Training");
    double? sensitivityTrain = GetStat(stats, "SN_Training");
    double? accuracyTest = GetStat(stats, "BA_Test");
    double? specificityTest = GetStat(stats, "SP_Test");
    double? sensitivityTest = GetStat(stats, "SN_Test");

    return (rsq, rmse, rsqTest, rmseTest,
        accuracyTrain, specificityTrain, sensitivityTrain,
        accuracyTest, specificityTest, sensitivityTest);
}

static double? GetStat(JsonElement stats, string propertyName)
{
    return stats.TryGetProperty(propertyName, out var value) &&
           value.TryGetDouble(out var parsedValue)
        ? parsedValue
        : null;
}

static List<Dictionary<string, string>> LoadRows(
    string inputPath,
    out string[] headers,
    out IReadOnlyList<string> guidColumnNames)
{
    using var input = new FileStream(inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    using var reader = new StreamReader(
        input,
        Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
        detectEncodingFromByteOrderMarks: true);

    var headerLine = reader.ReadLine()
        ?? throw new InvalidDataException("The input file is empty.");
    headers = headerLine.Split('\t');
    guidColumnNames = new[] { "Guid", "ScaleGUID" }
        .Where(headers.Contains)
        .ToArray();

    var missingGuidColumns = new[] { "Guid", "ScaleGUID" }
        .Except(guidColumnNames)
        .ToArray();
    if (missingGuidColumns.Length > 0)
        throw new InvalidDataException(
            $"The input file does not contain the required column(s): {string.Join(", ", missingGuidColumns)}.");

    var rows = new List<Dictionary<string, string>>();
    string? line;
    var lineNumber = 1;

    while ((line = reader.ReadLine()) is not null)
    {
        lineNumber++;
        var cells = line.Split('\t');
        if (cells.Length != headers.Length)
            throw new InvalidDataException(
                $"Line {lineNumber} has {cells.Length} columns; expected {headers.Length}.");

        var row = new Dictionary<string, string>(headers.Length, StringComparer.Ordinal);
        for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
            row.Add(headers[columnIndex], cells[columnIndex]);

        rows.Add(row);
    }

    return rows;
}

static void WriteRows(
    string outputPath,
    IReadOnlyList<string> headers,
    IReadOnlyList<Dictionary<string, string>> rows)
{
    var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
    if (!string.IsNullOrEmpty(outputDirectory))
        Directory.CreateDirectory(outputDirectory);

    using var output = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
    using var writer = new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    writer.WriteLine(string.Join('\t', headers));

    foreach (var row in rows)
        writer.WriteLine(string.Join('\t', headers.Select(header => row[header])));
}
