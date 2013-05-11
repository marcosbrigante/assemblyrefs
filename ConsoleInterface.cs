using System;
using System.IO;
using System.Reflection;

namespace AssemblyRefs
{
    class ConsoleInterface
    {
        #region Constants
        private const string USAGE_MESSAGE = @"
Use: AssemblyRefs assembly_path

    assembly_path           The path to the assembly to be analyzed.";
        #endregion

        static string _assemblyDirectory = string.Empty;

        [STAThread]
        static void Main(string[] args)
        {
            var targetAssemblyPath = string.Empty;

            try
            {
                #region Argument Validation
                if (args.Length == 0)
                {
                    Console.WriteLine(USAGE_MESSAGE);
                    return;
                }

                targetAssemblyPath = args[0];

                if (!File.Exists(targetAssemblyPath))
                {
                    Console.WriteLine("Error: Assembly file not found.");
                    return;
                }

                #endregion

                Console.WriteLine(
                    string.Format("\nAnalyzing {0}...",
                    targetAssemblyPath));

                _assemblyDirectory = Path.GetDirectoryName(targetAssemblyPath);
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);

                var result = AssemblyModel.Analyze(targetAssemblyPath);

                var fileName = "ReferenceAnalysis-" + Path.GetFileNameWithoutExtension(targetAssemblyPath) + ".xml";
                var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), fileName);
                new ResultWriter().Write(result, fullPath); ;

                Console.WriteLine(string.Format("Analysis written to {0}.", fullPath));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal: " + ex.Message);
                Console.ReadLine();
            }
        }

        static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            return Assembly.LoadFrom(Path.Combine(_assemblyDirectory, args.Name.Split(',')[0] + ".dll"));
        }
    }


}
