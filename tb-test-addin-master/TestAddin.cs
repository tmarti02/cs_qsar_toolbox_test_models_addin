using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Toolbox.Docking.Api;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Units;
using TestAddins.Qsar;

namespace TestAddins
{

    public class TestAddin : IToolboxAddin
    {

        static TestAddin()
        {
            Log("Static constructor called");
        }

        public TestAddin()
        {
            Log("Instance constructor called");
        }

        private static void Log(string message)
        {
            try
            {

                string logDir = @"C:\Users\tmarti02\OneDrive - Environmental Protection Agency (EPA)\Comptox\000000 qsar toolbox plug in creation";
                string logPath = Path.Combine(logDir, "test-plugin-debug.log");

                File.AppendAllText(logPath,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
            }
        }

        public IEnumerable<ITbObjectFactory> GetToolboxObjectFactories()
        {
            Log("GetToolboxObjectFactories() called");

            try
            {
                List<Dictionary<string, string>> TestModels = Utilities.RetrieveModelInfo();
                // Log($"Retrieved model count: {TestModels.Count}");

                List<ITbObjectFactory> tbObjectFactoryList = new List<ITbObjectFactory>();

                foreach (Dictionary<string, string> model in TestModels)
                {
                    tbObjectFactoryList.Add((ITbObjectFactory)new TbQsarAddinFactory(model));
                }
                Log($"Returning factory count: {tbObjectFactoryList.Count}");
                return tbObjectFactoryList;
            }
            catch (Exception ex)
            {
                Log("ERROR in GetToolboxObjectFactories(): " + ex);
                throw;
            }
        }
    }
}