using System;
using System.Collections.Generic;
using Toolbox.Docking.Api.Chemical;
using Toolbox.Docking.Api.Data;
using Toolbox.Docking.Api.Objects;
using Toolbox.Docking.Api.Objects.Qsar;
using Toolbox.Docking.Api.Units;

namespace TestAddins.Qsar
{

    public static class QsarAddinDefinitions
    {


        public static Dictionary<string, string> getMetaDataValues(Dictionary<string, string> Modelinfo)
        {
            string[] Metadatalist = new string[] 
            {
                "Effect",
                "Test organisms (species)",
                "Endpoint comment",
                "Test type",
                // "Sex",
                "Route of administration",
                // "Organ",
                "Strain",
                "Metabolic activation",
                "Type of method",
                // "Assay provider",
                "Test guideline",
                "Reference",
                "Reference Link",
                "R2(Train)",
                "RMSE(Train)",
                // "Q2(Train)",
                // "Fisher(Train)",
                // "S(Train)",
                // "R2(Invisible training)",
                // "RMSE(Invisible training)",
                // "Q2(Invisible training)",
                // "Fisher(Invisible training)",
                // "S(Invisible training)",
                // "R2(Calibration)",
                // "RMSE(Calibration)",
                // "Q2(Calibration)",
                // "Fisher(Calibration)",
                // "S(calibration)",
                "R2(Test)",
                "RMSE(Test)",
                // "Fisher(Test)",
                // "S(Test)",
                "Balanced Accuracy(Train)",
                "Specificity(Train)",
                "Sensitivity(Train)",
                // "Accuracy(Internal Valid)",
                // "Specificity(Internal Valid)",
                // "Sensitivity(Internal Valid)",
                "Balanced Accuracy(Test)",
                "Specificity(Test)",
                "Sensitivity(Test)"
            };
            
            Dictionary<string, string> dict = new Dictionary<string, string>()
            {
                {
                    "Endpoint",
                    Modelinfo["Endpoint"]
                } 
            };

            foreach (string Colname in Metadatalist)
                if (Modelinfo[Colname] != "")
                {
                    dict.Add(Colname, Modelinfo[Colname]);
                }

            return dict;
        }

        public static IReadOnlyList<ChemicalWithData> GetSet(Dictionary<string, string> Modelinfo, TbScale ScaleDeclaration, String Set)
        {
            // No training or test set are currently provided
            // Possibility to extract and provide them from the jar application
            return new List<ChemicalWithData>();
        }



        //works only with a training set
        public static TbQsarStatistics M4RatioModelStatistics(String tag)
        {
            // just skipped - we can implement this by retriving the number of compounds in the training and test set
            return new TbQsarStatistics(0, 0, 0, 0);
        }

        public static TbObjectAbout GetM4ObjectAbout(Dictionary<string, string> Modelinfo)
        {

            // DA RIVEDERE CHE INFO VANNO ESPOSTE

            return new TbObjectAbout(
              /* description*/   Modelinfo["Description(long)"],
              /* donator  */ "US EPA",
              /* disclaimer*/ "TEST implementation based on version 1.2.3, please check the TEST-HUB website for all details about the application",
              /* authors*/ "Todd Martin <martin.todd@epa.gov>",
              /* url */ "https://www.epa.gov/comptox-tools/toxicity-estimation-software-tool-test",
              /* name*/ "TEST - " + Modelinfo["Modelname"],
              /* help file */ "",
              (IEnumerable<TbObjectAboutTextPair>)new TbObjectAboutTextPair[2]
                {
                    new TbObjectAboutTextPair("Adopted", "Toolbox 4.6"),
                    new TbObjectAboutTextPair("QMRF", Modelinfo["QMRFlink"])
                });
        }
    }


}
