using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Control;
using Toolbox.Docking.Api.Objects.Qsar;
using TestAddins;
using TestAddins.Qsar;
using Toolbox.Docking.Api.Data;

runModels();
// testDll();
// PrintModelTagsAndOnePrediction();

void PrintModelTagsAndOnePrediction()
{
    var addin = new TestAddin();
    var factories = addin.GetToolboxObjectFactories().ToList();

    Console.WriteLine("Available model tags:");

    foreach (var factory in factories.OfType<TbQsarAddinFactory>())
    {
        Console.WriteLine($"- {factory.ModelTag}");
    }

    string modelTag = "WS_TEST_CONSENSUS";
    string smiles = "CCCO";

    var selectedFactory = factories
        .OfType<TbQsarAddinFactory>()
        .SingleOrDefault(f => f.ModelTag == modelTag);

    if (selectedFactory is null)
    {
        Console.WriteLine($"Model '{modelTag}' was not found.");
        return;
    }

    var qsar = selectedFactory.GetQsar(null!);
    var basket = new TestBasket(smiles);
    var prediction = qsar.Predict(basket);

    Console.WriteLine();
    Console.WriteLine($"Running model: {selectedFactory.ModelTag}");
    Console.WriteLine($"SMILES: {smiles}");
    Console.WriteLine("Prediction:");

    if (prediction is null)
    {
        Console.WriteLine("  <null>");
        return;
    }

    Console.WriteLine($"  Type: {prediction.GetType().FullName}");
    Console.WriteLine($"  ToString: {prediction}");
}

void testDll()
{
    string dllPath = @"C:\Program Files\QSAR Toolbox\QSAR Toolbox 4.9\Toolbox Server\Bin\Addins\TestAddins\TestAddins.dll";
    string librariesPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "libraries"));

    AssemblyLoadContext.Default.Resolving += (_, assemblyName) =>
    {
        string dependencyPath = Path.Combine(librariesPath, assemblyName.Name + ".dll");
        return File.Exists(dependencyPath) ? AssemblyLoadContext.Default.LoadFromAssemblyPath(dependencyPath) : null;
    };

    var asm = Assembly.LoadFrom(dllPath);

    Console.WriteLine("Assembly: " + asm.FullName);

    try
    {
        foreach (var t in asm.GetTypes())
        {
            Console.WriteLine(t.FullName);
            foreach (var attr in t.GetCustomAttributes(false))
            {
                Console.WriteLine("  ATTR: " + attr.GetType().FullName);
            }
        }
    }
    catch (ReflectionTypeLoadException exception)
    {
        Console.Error.WriteLine("Some types could not be loaded:");
        foreach (Exception? loaderException in exception.LoaderExceptions)
        {
            if (loaderException is not null)
            {
                Console.Error.WriteLine(loaderException.Message);
            }
        }
    }
}




void runModels()
{
    string[] desiredModelTags =
{
    "WS_TEST_CONSENSUS",
    "VP_TEST_CONSENSUS",
    "MP_TEST_CONSENSUS",
    "MUTA_TEST_CONSENSUS",
    "DEVTOX_TEST_CONSENSUS",
    "LD50_TEST_CONSENSUS",
    "BCF_TEST_CONSENSUS",
    "LC50_FHM_TEST_CONSENSUS",
    "LC50_DM_TEST_CONSENSUS",
    "IGC50_TEST_CONSENSUS",
    "BP_TEST_CONSENSUS",
    "Density_TEST_CONSENSUS",
    "FP_TEST_CONSENSUS",
    "ST_TEST_CONSENSUS",
    "TC_TEST_CONSENSUS",
    "VISCOSITY_TEST_CONSENSUS"

};

    string smiles = "c1ccccc1";//benzene
    // string smiles = "CCCO.CC";//propanol
    // string smiles = "CSSSSS";

    Console.WriteLine($"Running models for SMILES: {smiles}");

    foreach (string desiredModelTag in desiredModelTags)
        RunModel(desiredModelTag, smiles);
}



static void RunModel(string desiredModelTag, string smiles)
{
    var factories = new TestAddin().GetToolboxObjectFactories().ToList();

    if (factories.Count == 0)
        throw new InvalidOperationException("GetToolboxObjectFactories returned no factories.");

    if (factories.Any(factory => factory is null))
        throw new InvalidOperationException("GetToolboxObjectFactories returned a null factory.");

    // Console.WriteLine($"PASS: GetToolboxObjectFactories returned {factories.Count} factories.");

    var qsarFactory = factories
        .OfType<TbQsarAddinFactory>()
        .SingleOrDefault(factory => factory.ModelTag == desiredModelTag);

    if (qsarFactory is null)
        throw new InvalidOperationException($"No factory found for model tag '{desiredModelTag}'.");

    Console.WriteLine($"Selected model: {qsarFactory.ModelTag}");
    // foreach (var entry in qsarFactory.ModelInfo.OrderBy(entry => entry.Key))
    //     Console.WriteLine($"{entry.Key}: {entry.Value}");

    var qsar = qsarFactory.GetQsar(null!);
    var basket = new TestBasket(smiles);
    var prediction = qsar.Predict(basket);

    if (prediction is null)
        throw new InvalidOperationException("QsarAddin.Predict returned null.");

    if (qsar is not QsarAddin qsarAddin)
        throw new InvalidOperationException("The factory returned an unexpected QSAR implementation.");

    var domainStatus = qsarAddin.CheckDomain(basket);
    Console.WriteLine($"Domain status: {domainStatus}\n");

    // Console.WriteLine("PASS: QsarAddin.Predict returned a prediction.");
}

sealed class TestBasket : ITbBasket
{
    public TestBasket(string smiles)
    {
        Chemical = new Chemical(Guid.NewGuid(), Enum.GetValues<ChemicalType>().First(), smiles);
    }

    // public TestBasket(string smiles, string casrn) 
    // // does the qsar toolbox generated baskets have Chemical or ChemicalWithData?
    // {
    //     Chemical = new Chemical(
    //         Guid.NewGuid(),
    //         Enum.GetValues<ChemicalType>().First(),
    //         smiles);

    //     int cas = int.Parse(
    //         new string(casrn.Where(char.IsDigit).ToArray()));

    //     // Initialize this using the required TbData values.
    //     ChemicalWithData = new ChemicalWithData(
    //         cas,
    //         Array.Empty<string>(),
    //         smiles,
    //         Array.Empty<TbDescribedData>(),
    //         null);
    // }


    public ITbWorkTask WorkTask => null!;

    public Chemical Chemical { get; }

    public ChemicalWithData? ChemicalWithData { get; }

    public Chemical TargetChemical => Chemical;

    public ITbBasket SubBasket(Chemical chemical) => throw new NotSupportedException();

    public ITbBasket SubBasket(Chemical chemical, Chemical targetChemical) => throw new NotSupportedException();
}
