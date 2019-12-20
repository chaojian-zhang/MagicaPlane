using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MagicaPlaneProject
{
    public class Program
    {
        private const string DocFileName = "CommandDoc.txt";
        private const int PartSpecArgCount = 4;

        public static void Main(string[] args)
        {
            // Show doc
            if(args.Length == 0)
                Console.WriteLine(ReadResource(DocFileName));
            // Compile project
            else if(args.Length == 1)
            {
                string projectFolder = Path.GetFullPath(args[0]);
                string materialFilePath = Path.Combine(projectFolder, MagicaPlane.Program.MaterialDefinitionFile);
                Dictionary<string, byte> materials = MagicaPlane.Program.ReadMaterial(materialFilePath);
                // Compile and keep track of generated files
                List<string> resultFiles = new List<string>();
                foreach (string subfolder in Directory.EnumerateDirectories(projectFolder))
                    resultFiles.AddRange(MagicaPlane.Program.ParseFolder(subfolder, materials));
                // Make copy of files to project folder
                foreach (var resultFile in resultFiles)
                {
                    string name = Path.GetFileName(resultFile);
                    File.Copy(resultFile, Path.Combine(projectFolder, name));
                }
            }
            // Initialize project subfolders
            else if((args.Length - 1) % PartSpecArgCount == 0)
            {
                string folder = Path.GetFullPath(args[0]);
                for (int part = 0; part < (args.Length - 1) / PartSpecArgCount; part++)
                {
                    // Extract parameters
                    string partName = args[part * PartSpecArgCount + 1];
                    int rows = int.Parse(args[part * PartSpecArgCount + 2]);
                    int cols = int.Parse(args[part * PartSpecArgCount + 3]);
                    int height = int.Parse(args[part * PartSpecArgCount + 4]);
                    // Create folder
                    string partFolder = Path.Combine(folder, partName);
                    if (Directory.Exists(partName))
                        Directory.CreateDirectory(partFolder);
                    // Initialize folder if it's empty
                    if (Directory.GetFiles(partFolder).Length == 0)
                        MagicaPlane.Program.InitializeFolder(rows, cols, height, partFolder);
                }
            }
            else
            {
                Console.WriteLine("Wrong argument count.");
                Console.WriteLine(ReadResource(DocFileName));
            }
        }

        /// <summary>
        /// Get embedded resource from the assembly
        /// </summary>
        private static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(MagicaPlaneProject)))
            {
                resourcePath = assembly.GetManifestResourceNames()
                    .Single(str => str.EndsWith(name));
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
