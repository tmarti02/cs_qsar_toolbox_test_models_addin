using System;
using System.IO;
using System.IO.Compression;

class Program
{
    static void Main()
    {
        string projectRoot = @"C:\Users\tmarti02\OneDrive - Environmental Protection Agency (EPA)\0 c#\cs_qsar_toolbox_test_models_addin";

        // Build output staging area (where TestAddins.csproj writes the DLL)
        string buildStagingRoot = Path.Combine(projectRoot, "PackageStaging");
        string buildServerFolder = Path.Combine(buildStagingRoot, "ToolboxServerAddinsFolder");

        // Separate packaging area so we do NOT delete the build output
        string packageRoot = Path.Combine(projectRoot, "PackageBuild");
        string packageServerFolder = Path.Combine(packageRoot, "ToolboxServerAddinsFolder");

        // Source files in your repo
        string manifestSource = Path.Combine(projectRoot, "manifest.txt");
        // string resourcesSource = Path.Combine(projectRoot, "Resources", "testmodels.txt");
        string webTestJarSource = Path.Combine(projectRoot, "test_bin", "WebTEST.jar");
        string jdkSource = Path.Combine(projectRoot, "test_bin", "jdk-26.0.2.1");

        // Final package outputs
        string zipTemp = Path.Combine(projectRoot, "Toolbox.Addin.TestAddins.zip");
        string tbaddinPath = Path.Combine(projectRoot, "Toolbox.Addin.TestAddins.tbaddin");

        // Clean only the packaging folder, not the build staging folder
        if (Directory.Exists(packageRoot))
            Directory.Delete(packageRoot, recursive: true);

        if (File.Exists(zipTemp))
            File.Delete(zipTemp);

        if (File.Exists(tbaddinPath))
            File.Delete(tbaddinPath);

        Directory.CreateDirectory(packageServerFolder);

        // 1) Copy manifest.txt to package root
        if (!File.Exists(manifestSource))
        {
            Console.WriteLine("Missing manifest.txt: " + manifestSource);
            return;
        }

        File.Copy(manifestSource, Path.Combine(packageRoot, "manifest.txt"), overwrite: true);

        // 2) Verify the build output DLL exists
        string dllPath = Path.Combine(buildServerFolder, "Toolbox.Addin.TestAddins.dll");
        if (!File.Exists(dllPath))
        {
            Console.WriteLine("Missing DLL: " + dllPath);
            Console.WriteLine("Build TestAddins.csproj first, then rerun this packager.");
            return;
        }

        // Copy DLL, deps, and pdb from build staging into package staging
        CopyFileIfExists(Path.Combine(buildServerFolder, "Toolbox.Addin.TestAddins.dll"), packageServerFolder);
        CopyFileIfExists(Path.Combine(buildServerFolder, "Toolbox.Addin.TestAddins.deps.json"), packageServerFolder);
        CopyFileIfExists(Path.Combine(buildServerFolder, "Toolbox.Addin.TestAddins.pdb"), packageServerFolder);

        // 3) Copy Resources/testmodels.txt into package server folder
        // string packageResourcesTarget = Path.Combine(packageServerFolder, "Resources");
        // Directory.CreateDirectory(packageResourcesTarget);
        // CopyFileIfExists(resourcesSource, packageResourcesTarget);

        // 4) Copy WebTEST.jar and JDK into package server folder
        string packageTestBinTarget = Path.Combine(packageServerFolder, "test_bin");
        Directory.CreateDirectory(packageTestBinTarget);

        CopyFileIfExists(webTestJarSource, packageTestBinTarget);
        CopyDirectoryIfExists(jdkSource, Path.Combine(packageTestBinTarget, "jdk-26.0.2.1"));

        // 5) Zip the package root
        ZipFile.CreateFromDirectory(
            packageRoot,
            zipTemp,
            CompressionLevel.Optimal,
            includeBaseDirectory: false);

        // 6) Rename .zip to .tbaddin
        File.Move(zipTemp, tbaddinPath);

        Console.WriteLine("Created:");
        Console.WriteLine(tbaddinPath);
    }

    static void CopyFileIfExists(string sourceFile, string targetDir)
    {
        if (!File.Exists(sourceFile))
        {
            Console.WriteLine("Missing file: " + sourceFile);
            return;
        }

        Directory.CreateDirectory(targetDir);
        File.Copy(sourceFile, Path.Combine(targetDir, Path.GetFileName(sourceFile)), overwrite: true);
    }

    static void CopyDirectoryIfExists(string sourceDir, string targetDir)
    {
        if (!Directory.Exists(sourceDir))
        {
            Console.WriteLine("Missing directory: " + sourceDir);
            return;
        }

        foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(sourceDir, targetDir));
        }

        foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            string targetFile = file.Replace(sourceDir, targetDir);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(file, targetFile, overwrite: true);
        }
    }
}