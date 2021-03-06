﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MagicaPlane
{
    public static class Program
    {
        public const string MaterialDefinitionFile = "material.csv";

        public static void Main(string[] args)
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
                else if(!Directory.GetFiles(dir).Any(f => Path.GetFileName(f) == MaterialDefinitionFile))
                    Console.WriteLine("Input folder missing material definition.");
                else
                    ParseFolder(dir, null);
            }
        }
        /// <summary>
        /// Initialize structure for an empty folder
        /// </summary>
        public static void InitializeFolder(int rows, int cols, int height, string dir)
        {
            // Write layers
            for (int f = 0; f < height; f++)
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
            File.WriteAllText(Path.Combine(dir, MaterialDefinitionFile), GetDefaultMaterialDefinition());
        }

        /// <summary>
        /// Generate default material definition string
        /// </summary>
        public static string GetDefaultMaterialDefinition()
            => "empty,0\ncustom,5";

        /// <summary>
        /// Parse a given voxel layers folder, optionally override folder specific material file (provided as interface for MagicaPlaneProject program)
        /// </summary>
        /// <returns>Returns generated files; Used by MagicaPlaneProject</returns>
        public static List<string> ParseFolder(string dir, Dictionary<string, byte> materialFileOverride)
        {
            var layers = Directory.EnumerateFiles(dir)
                                .Where(f => Path.GetFileName(f) != MaterialDefinitionFile && int.TryParse(Path.GetFileNameWithoutExtension(f), out _))
                                .OrderBy(f => int.Parse(Path.GetFileNameWithoutExtension(f)))
                                .ToArray();
            string material = Directory.EnumerateFiles(dir).SingleOrDefault(f => Path.GetFileName(f) == MaterialDefinitionFile);
            // Validate folder
            if(layers.Length == 0)
            {
                Console.WriteLine($"Folder `{dir}` doesn't contain any layer file.");
                return null;
            }
            if (material == null)
            {
                Console.WriteLine($"Folder `{dir}` doesn't contain any material definition.");
                return null;
            }
            // Determine dimension
            int height = layers.Length;
            int rows, columns;
            GetDimensions(File.ReadAllLines(layers.First()), out rows, out columns);
            // Read material definition
            Dictionary<string, byte> materials = materialFileOverride ?? ReadMaterial(material);
            // Build index list
            StringBuilder builder = new StringBuilder();
            // Prepare exporting .vox
            Dictionary<Tuple<byte, byte, byte>, byte> voxels = new Dictionary<Tuple<byte, byte, byte>, byte>();
            // Load layers
            for (int z = 0; z < layers.Length; z++)
            {
                string[] csvLines = File.ReadAllLines(layers[z]);
                // Size validation
                int tempRows, tempColumns;
                GetDimensions(csvLines, out tempRows, out tempColumns);
                if(tempRows != rows || tempColumns != columns)
                {
                    Console.WriteLine($"Layer dimension in file `{layers[z]}` ({tempRows} rows x {tempColumns} columns) " +
                        $"doesn't match expected lowest master layer `{layers.First()}` ({rows} rows x {columns} columns). Abort parsing.");
                    return null;
                }
                if(tempRows > 256)
                {
                    Console.WriteLine($"Row count in file `{layers[z]}` ({tempRows} rows x {tempColumns} columns) exceeds max (256).");
                    return null;
                }
                if (tempColumns > 256)
                {
                    Console.WriteLine($"Column count in file `{layers[z]}` ({tempRows} rows x {tempColumns} columns) exceeds max (256).");
                    return null;
                }
                // Parse each line
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
                        byte index = materials[col];
                        // Set volume value
                        builder.Append($"{index},"); // Order: col, row, height
                        // Add to voxel data
                        if(index != 0)
                            voxels.Add(new Tuple<byte, byte, byte>((byte)c, (byte)r, (byte)z), index);
                    }
                }
            }
            // Remove additional comma
            builder.Remove(builder.Length - 1, 1);
            // Generate result as shader
            string folderName = Path.GetFileName(dir);  // Name shader as name of folder
            string shaderPath = Path.Combine(dir, $"{folderName}.txt");
            string shader = ReadResource("Template.txt");
            shader = shader.Replace("{{TotalGridSize}}", $"{rows * columns * layers.Length}");
            shader = shader.Replace("{{IndexList}}", builder.ToString());
            shader = shader.Replace("{{Width}}", $"{columns.ToString()}/*x*/");
            shader = shader.Replace("{{Length}}", $"{rows.ToString()}/*y*/");
            shader = shader.Replace("{{Height}}", $"{layers.Length.ToString()}/*z*/");
            File.WriteAllText(shaderPath, shader);
            // Generate result as .vox
            string voxelPath = Path.Combine(dir, $"{folderName}.vox");
            WriteVoxels(voxelPath, voxels, rows, columns, height);
            // Return generated files
            return new List<string>() { shaderPath, voxelPath };
        }

        private static void WriteVoxels(string voxelPath, Dictionary<Tuple<byte, byte, byte>, byte> voxels, int rows, int cols, int height)
        {
            var vox = new VoxWriter((uint)cols, (uint)rows, (uint)height);

            foreach (var v in voxels)
            {
                var key = v.Key;
                var index = v.Value;
                vox.SetVoxel(key.Item1, key.Item2, key.Item3, index);
            }
            
            // Save to file
            vox.Export(voxelPath);
        }

        /// <summary>
        /// Read material definitions from file
        /// </summary>
        public static Dictionary<string, byte> ReadMaterial(string materialPath)
        {
            Dictionary<string, byte> materials = new Dictionary<string, byte>();
            string[] lines = File.ReadAllLines(materialPath);
            foreach (var line in lines)
            {
                string[] cols = line.Split(',');
                string name = cols[0].ToLower();    // Take lower case
                byte index = Convert.ToByte(cols[1]);
                materials[name] = index;
            }
            return materials;
        }

        /// <summary>
        /// Get dimension of file
        /// </summary>
        public static void GetDimensions(string[] csvLines, out int rows, out int columns)
        {
            rows = csvLines.Length;
            columns = csvLines[0].Split(',').Count();
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
