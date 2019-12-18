using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MagicaPlane
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check null argument
            if(args.Length == 0)
                Console.WriteLine("Missing argument for input folder!\n" +
                    "Accepted commands: MagicaPlane inputFolder [rows cols height]\n" +
                    "Use optional [rows cols height] arguments to initialize an EMPTY folder.");
            // Check folder existence
            else if(!Directory.Exists(args[0]))
                Console.WriteLine("Input folder doesn't exist!");
            // Initialize command
            else if (args.Length == 4)
            {                
                string dir = Path.GetFullPath(args[0]);
                // Check empty
                if (Directory.GetFiles(dir).Length != 0)
                    Console.WriteLine("Input folder not empty!");
                else
                    InitializeFolder(Convert.ToInt32(args[1]), Convert.ToInt32(args[2]), Convert.ToInt32(args[3]), dir);
            }
            else
            {
                string dir = Path.GetFullPath(args[0]);
                // Check empty
                if(Directory.GetFiles(dir).Length == 0)
                    Console.WriteLine("Input folder is empty.");
                else if(!Directory.GetFiles(dir).Any(f => Path.GetFileName(f) == "material.csv"))
                    Console.WriteLine("Input folder missing material definition.");
                else
                    ParseFolder(dir);
            }
        }

        private static void InitializeFolder(int rows, int cols, int heights, string dir)
        {
            // Write layers
            for (int f = 0; f < heights; f++)
            {
                StringBuilder builder = new StringBuilder();
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                        builder.Append("empty,");
                    // Remove trailing additional comma
                    builder.Remove(builder.Length - 1, 1);
                    // Append new line to start a new row
                    builder.Append('\n');
                }
                // Remove trailing empty line
                builder.Remove(builder.Length - 1, 1);
                // Write to file
                File.WriteAllText(Path.Combine(dir, $"{f + 1}.csv"), builder.ToString());
            }
            // Write material
            File.WriteAllText(Path.Combine(dir, "material.csv"), "empty,0\ncustom,5");
        }

        private static void ParseFolder(string dir)
        {
            var layers = Directory.EnumerateFiles(dir)
                                .Where(f => Path.GetFileNameWithoutExtension(f) != "material" && int.TryParse(Path.GetFileNameWithoutExtension(f), out _))
                                .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                                .ToArray();
            string material = Directory.EnumerateFiles(dir).Single(f => Path.GetFileNameWithoutExtension(f) == "material");
            // Determine dimension
            int height = layers.Length;
            int rows, columns;
            GetDimensions(File.ReadAllLines(layers.First()), out rows, out columns);
            // Read material definition
            Dictionary<string, int> materials = ReadMaterial(material);
            // Build index list
            StringBuilder builder = new StringBuilder();
            // Load layers
            for (int z = 0; z < layers.Length; z++)
            {
                string[] csvLines = File.ReadAllLines(layers[z]);
                for (int r = 0; r < csvLines.Length; r++)
                {
                    string line = csvLines[r];
                    // Skip empty
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    // Get columns
                    string[] cols = line.Split(',');
                    for (int c = 0; c < cols.Length; c++)
                    {
                        string col = cols[c].ToLower(); // Material is case insensitive
                        if(!materials.ContainsKey(col))
                        {
                            string replacement = materials.First().Key;
                            Console.WriteLine($"Missing material definition `{col}` on line ({r+1}), col ({c+1}) of file `{layers[z]}`, " +
                                $"using `{replacement}` instead.");
                            col = replacement;
                        }
                        int index = materials[col];
                        // Set volume value
                        builder.Append($"{index},"); // Order: col, row, height
                    }
                }
            }
            // remove additional comma
            builder.Remove(builder.Length - 1, 1);
            // Generate result
            string shaderPath = Path.Combine(dir, $"{Path.GetFileName(dir)}.txt");  // Name shader as name of folder
            string shader = ReadResource("Template.txt");
            shader = shader.Replace("{{TotalGridSize}}", $"{rows * columns * layers.Length}");
            shader = shader.Replace("{{IndexList}}", builder.ToString());
            shader = shader.Replace("{{Width}}", $"{columns.ToString()}/*x*/");
            shader = shader.Replace("{{Length}}", $"{rows.ToString()}/*y*/");
            shader = shader.Replace("{{Height}}", $"{layers.Length.ToString()}/*z*/");
            File.WriteAllText(shaderPath, shader);
        }

        private static Dictionary<string, int> ReadMaterial(string materialPath)
        {
            Dictionary<string, int> materials = new Dictionary<string, int>();
            string[] lines = File.ReadAllLines(materialPath);
            foreach (var line in lines)
            {
                string[] cols = line.Split(',');
                string name = cols[0].ToLower();    // Take lower case
                int index = Convert.ToInt32(cols[1]);
                materials[name] = index;
            }
            return materials;
        }

        static void GetDimensions(string[] csvLines, out int rows, out int columns)
        {
            rows = csvLines.Length;
            columns = csvLines[0].Split(',').Count();
        }

        static string ReadResource(string name)
        {
            // Determine path
            var assembly = Assembly.GetExecutingAssembly();
            string resourcePath = name;
            // Format: "{Namespace}.{Folder}.{filename}.{Extension}"
            if (!name.StartsWith(nameof(MagicaPlane)))
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
