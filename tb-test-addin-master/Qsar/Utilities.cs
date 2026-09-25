using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Units;
using System.Text.Json;

namespace TestAddins.Qsar
{
    class Utilities
    {
        // custom Scales (not available in the standard TB scales)

        public static readonly TbRatioScale SurfaceTension =
            new TbRatioScale("Surface tension", "Surface tension", Guid.Parse("3f8b9c4f-8f2f-4f5b-9d2a-6d9a2c4b7e11"), "dyn/cm");

        public static readonly TbRatioScale Viscosity =
            new TbRatioScale("Viscosity", "Viscosity", Guid.Parse("b6c3f5a2-2d9e-4a56-9f4e-1f2a7c8d9e10"), "cP");

        public static readonly TbRatioScale ThermalConductivity =
            new TbRatioScale("Thermal conductivity", "Thermal conductivity", Guid.Parse("c7d4c6b3-3e0f-5b67-af89-2d3e4f5a6b7c"), "mW/(m.K)");

        public static double BoxCox(double lambda, double value)
        {
            return Math.Pow(value * lambda + 1.0, 1.0 / lambda);
        }

        public static double DoubleParser(string value)
        {
            CultureInfo culture = new CultureInfo("en-US");
            return double.Parse(value, culture.NumberFormat);
        }

        public static TbScalarData ConvertData(Dictionary<string, string> Results, TbScale ScaleDeclaration, Dictionary<string, string> Modelinfo)
        {
            string valid = Results["valid"];

            // Classification models
            if (ScaleDeclaration is TbQualitativeScale scaleDeclarationQualitative)
            {
                string PredClass = Results["PredictionClass"];

                if (valid.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                    return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, PredClass), null);

                if (!scaleDeclarationQualitative.Labels.Any(l => l.Equals(PredClass, StringComparison.InvariantCultureIgnoreCase)))
                    throw new Exception(string.Format("\"{0}\" is not a prediction for the declared scale.", PredClass));

                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, PredClass), null);
            }

            // Quantitative models
            if (!Results.ContainsKey("Prediction") || valid.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, Modelinfo["TBUnit"]), double.NaN);

            double PredValue = DoubleParser(Results["Prediction"]);
            double ResValue = double.NaN;

            if (Modelinfo["TBUnit"] == Modelinfo["TestUnit"])
            {
                ResValue = PredValue;
            }
            else
            {
                switch (ScaleDeclaration.FamilyGroup)
                {
                    case "Bioaccumulation":
                        if (Modelinfo["TBUnit"] == "L/kg")
                            if (Modelinfo["TestUnit"] == "log(L/kg)")
                                ResValue = Math.Pow(10, PredValue);
                        break;

                    default:
                        Console.WriteLine("Need conversion for " + ScaleDeclaration.Name + " from TEST unit " + Modelinfo["TestUnit"] + " to TB unit " + Modelinfo["TBUnit"]);
                        ResValue = PredValue;
                        break;
                }
            }

            return (TbData)new TbData(new TbUnit(ScaleDeclaration.Name, Modelinfo["TBUnit"]), ResValue);
        }

        public static async Task<bool> ensureAPI_is_running(string jarFileName)
        {
            bool running = await IsApiHealthyAsync().ConfigureAwait(false);

            if (!running)
            {
                Console.WriteLine("Server not responding; launching WebTEST.jar...");
                LaunchWebTestJar(jarFileName);

                bool ready = await WaitForApiReadyAsync(
                    timeout: TimeSpan.FromMinutes(2),
                    pollInterval: TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                if (!ready)
                {
                    Console.WriteLine("API did not become ready in time.");
                    return false;
                }

                Console.WriteLine("API is now ready.");
            }
            else
            {
                bool ready = await WaitForApiReadyAsync(
                    timeout: TimeSpan.FromMinutes(2),
                    pollInterval: TimeSpan.FromSeconds(2)).ConfigureAwait(false);

                if (!ready)
                {
                    Console.WriteLine("API did not become ready in time.");
                    return false;
                }

                Console.WriteLine("API is ready.");
            }

            return true;
        }

        static void LaunchWebTestJar(string jarFileName)
        {
            string projectDir = FindWebTestDirectory(jarFileName);

            if (string.IsNullOrEmpty(projectDir))
            {
                Console.WriteLine($"{jarFileName} not found. Set WEBTEST_HOME or place {jarFileName} beside the smoke test runtime.");
                return;
            }

            string javaExe = FindJavaExecutable(projectDir);
            string jarPath = Path.Combine(projectDir, jarFileName);

            if (!File.Exists(jarPath))
            {
                Console.WriteLine("WebTEST.jar not found: " + jarPath);
                return;
            }

            if (string.IsNullOrEmpty(javaExe))
            {
                Console.WriteLine("Java not found. Set WEBTEST_JAVA_HOME or place a jdk-* folder beside WebTEST.jar.");
                return;
            }

            Console.WriteLine("Using WebTEST.jar: " + jarPath);
            Console.WriteLine("Using Java: " + javaExe);

            var psi = new ProcessStartInfo
            {
                FileName = javaExe,
                Arguments = $"-jar \"{jarPath}\" --server.port=8081",
                WorkingDirectory = Path.GetDirectoryName(jarPath)!,
                UseShellExecute = true,
                CreateNoWindow = false
            };

            try
            {
                Process.Start(psi);
                Console.WriteLine("WebTEST.jar launched.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to launch jar: " + ex.Message);
            }
        }

        static string? FindWebTestDirectory(string jarFileName)
        {
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            string testBinDirectory = Path.Combine(pluginDir, "test_bin");

            if (File.Exists(Path.Combine(testBinDirectory, jarFileName)))
                return testBinDirectory;

            return null;
        }

        static string FindJavaExecutable(string webTestDirectory)
        {
            string configuredJavaHome = Environment.GetEnvironmentVariable("WEBTEST_JAVA_HOME") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(configuredJavaHome))
            {
                if (File.Exists(configuredJavaHome))
                    return configuredJavaHome;

                string configuredJava = Path.Combine(configuredJavaHome, "bin", "java.exe");
                if (File.Exists(configuredJava))
                    return configuredJava;
            }

            return Directory.EnumerateDirectories(webTestDirectory, "jdk-*", SearchOption.TopDirectoryOnly)
                .Select(directory => Path.Combine(directory, "bin", "java.exe"))
                .FirstOrDefault(File.Exists) ?? string.Empty;
        }

        static async Task<bool> WaitForApiReadyAsync(TimeSpan timeout, TimeSpan pollInterval)
        {
            DateTime start = DateTime.UtcNow;

            while (DateTime.UtcNow - start < timeout)
            {
                if (await IsApiReadyAsync().ConfigureAwait(false))
                    return true;

                await Task.Delay(pollInterval).ConfigureAwait(false);
            }

            return false;
        }

        public static async Task<bool> IsApiHealthyAsync()
        {
            using var client = new HttpClient();

            try
            {
                var json = await client.GetStringAsync("http://localhost:8081/health").ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                return doc.RootElement.TryGetProperty("status", out var status)
                       && status.GetString() == "ok";
            }
            catch
            {
                return false;
            }
        }

        public static async Task<bool> IsApiReadyAsync()
        {
            using var client = new HttpClient();

            try
            {
                var json = await client.GetStringAsync("http://localhost:8081/ready").ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);

                return doc.RootElement.TryGetProperty("status", out var status)
                       && status.GetString() == "ready";
            }
            catch
            {
                return false;
            }
        }

        public static List<Dictionary<string, string>> RetrieveModelInfo()
        {
           
            Assembly asm = typeof(Utilities).Assembly;
            string csvpath = asm.GetName().Name + ".Resources.testmodels.txt";
            Stream? resource = asm.GetManifestResourceStream(csvpath);
           
            if (resource == null)
                throw new System.Exception("unable to access text file with models info");

            using StreamReader reader = new StreamReader(resource);

            string? headerLine = reader.ReadLine();
            if (headerLine == null)
                throw new Exception("testmodels.txt is empty or missing header row");

            string[] columnHeaders = headerLine.Split('\t');

            List<Dictionary<string, string>> models = new List<Dictionary<string, string>>();
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                var newDict = new Dictionary<string, string>();
                var cells = line.Split('\t');

                for (int i = 0; i < columnHeaders.Length; i++)
                {
                    if (i >= cells.Length)
                        continue;

                    if (cells[i] == "micromol/L")
                    {
                        newDict.Add(columnHeaders[i], "µmol/L");
                    }
                    else
                    {
                        newDict.Add(columnHeaders[i], cells[i]);
                    }
                }

                models.Add(newDict);
            }

            return models;
        }

        public static Dictionary<string, TbOrdinalScale> GetOrdinalScales()
        {
            List<Dictionary<string, string>> info = Utilities.RetrieveModelInfo();

            List<string> origFamilies = new List<string>();
            foreach (Dictionary<string, string> modelinfo in info)
            {
                if (modelinfo["TestUnit"] == "")
                {
                    if (modelinfo.TryGetValue("UnitFamily", out string? curClass) && !string.IsNullOrWhiteSpace(curClass))
                    {
                        if (!origFamilies.Contains(curClass))
                            origFamilies.Add(curClass);
                    }
                }
            }

            Dictionary<string, TbOrdinalScale> scales = new Dictionary<string, TbOrdinalScale>();

            foreach (string family in origFamilies)
            {
                List<string> classes = new List<string>();
                string? lastGuid = null;

                foreach (Dictionary<string, string> modelinfo in info)
                {
                    if ((modelinfo["TestUnit"] == "") && (modelinfo["UnitFamily"] == family))
                    {
                        string[] curClasses = modelinfo["Classes"].Replace("\"", "").Split(new[] { "|" }, StringSplitOptions.None);
                        foreach (string s in curClasses)
                            classes.Add(s);

                        lastGuid = modelinfo["ScaleGUID"];
                    }
                }

                if (string.IsNullOrWhiteSpace(lastGuid))
                    throw new InvalidOperationException($"No GUID found for ordinal scale family '{family}'.");

                TbOrdinalScale curScale = new TbOrdinalScale(family + " TEST", family + " TEST", Guid.Parse(lastGuid), classes);
                scales.Add(family, curScale);
            }

            return scales;
        }
    }
}