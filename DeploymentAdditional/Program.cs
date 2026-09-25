using System;
using System.IO;

namespace DeploymentAdditional
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            foreach (string argument in args)
            {
                int separator = argument.IndexOf('=');
                if (separator <= 1)
                    return 2;

                string value = argument[(separator + 1)..].Trim();
                if (string.IsNullOrWhiteSpace(value))
                    return 2;

                Directory.CreateDirectory(value);
            }

            return 0;
        }
    }
}
