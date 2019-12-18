using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MagicaPlane
{
    /// <remarks>Adapted from: https://codepen.io/quasimondo/pen/QjqZvV, not super easy </remarks>
    public class VoxWriter
    {
        #region Constructor
        public VoxWriter(uint xSize, uint ySize, uint zSize)
        {
            X = xSize;
            Y = ySize;
            Z = zSize;
            VCount = 0;
            Voxels = new Dictionary<string, byte>();
            Palette = new List<uint>();

            // Initialize palette
            // Notice 0 represent no voxel and is included as the first element, i from 256-0 inclusive
            // The end result here is to generate from [255, 255, 255, 255] to [0, 0, 0, 255], the last one being "index 0"
            for (int i = 256; --i > -1;)    // 256 items in total
            {
                uint ui = (uint)i;
                Palette.Add(0xff000000 | ui | (ui << 8) | (ui << 16));
            }
        }
        public uint X { get; }
        public uint Y { get; }
        public uint Z { get; }
        public uint VCount { get; private set; }
        public Dictionary<string, byte> Voxels { get; private set; }
        /// <summary>
        /// Palette of RGB colors
        /// </summary>
        /// <remarks>The color palette can be written directly, format is 0xAARRGGBB; 
        /// Note that the palette values are offset by 1, so setting palette[0] will change the color index #1 (as in MagicaVoxel)
        /// Clearly palette[255] aka. index #256 is the empty voxel</remarks>
        public List<uint> Palette { get; private set; }
        #endregion

        #region Sub Routines
        private void AppendString(BinaryWriter data, string str)
        {
            for (var i = 0; i < str.Length; ++i)
                data.Write((byte)str[i]);
        }
        private void AppendUInt32(BinaryWriter data, uint n)
        {
            data.Write((byte)(n & 0xff));
            data.Write((byte)((n >> 8) & 0xff));
            data.Write((byte)((n >> 16) & 0xff));
            data.Write((byte)((n >> 24) & 0xff));
        }
        private void AppendRGBA(BinaryWriter data, uint n)
        {
            data.Write((byte)((n >> 16) & 0xff));
            data.Write((byte)((n >> 8) & 0xff));
            data.Write((byte)(n & 0xff));
            data.Write((byte)((n >> 24) & 0xff));
        }
        private void AppendVoxel(BinaryWriter data, string key)
        {
            var v = key.Split('_');
            // TODO: data.push(v[0], v[1], v[2], this.voxels[key]); - Write char or int?
            data.Write(Convert.ToByte(v[0]));
            data.Write(Convert.ToByte(v[1]));
            data.Write(Convert.ToByte(v[2]));
            data.Write(Voxels[key]);
        }
        #endregion

        #region Interface
        /// <summary>
        /// Set or remove a voxel
        /// </summary>
        /// <param name="colorIndex">Number 0 will clear the voxel</param>
        public void SetVoxel(int x, int y, int z, byte colorIndex)
        {
            if (x >= 0 && y >= 0 && z >= 0 && x < this.X && z < this.Y && z < this.Z)
            {
                string key = x + "_" + y + "_" + z;
                // Set
                if (colorIndex != 0)
                {
                    if (!Voxels.ContainsKey(key))
                        VCount++;
                    Voxels[key] = colorIndex;
                }
                // Clear
                else
                {
                    if (Voxels.ContainsKey(key))
                        VCount--;
                    Voxels.Remove(key);
                }
            }
        }
        public void Export(string filePath)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(file))
            {
                AppendString(writer, "VOX ");
                AppendUInt32(writer, 150);
                AppendString(writer, "MAIN");
                AppendUInt32(writer, 0);
                AppendUInt32(writer, VCount * 4 + 0x434);

                AppendString(writer, "SIZE");
                AppendUInt32(writer, 12);
                AppendUInt32(writer, 0);
                AppendUInt32(writer, X);
                AppendUInt32(writer, Y);
                AppendUInt32(writer, Z);
                AppendString(writer, "XYZI");
                AppendUInt32(writer, 4 + VCount * 4);
                AppendUInt32(writer, 0);
                AppendUInt32(writer, VCount);
                foreach (string key in Voxels.Keys)
                    AppendVoxel(writer, key);
                AppendString(writer, "RGBA");
                AppendUInt32(writer, 0x400);
                AppendUInt32(writer, 0);
                for (var i = 0; i < 256; i++)
                    AppendRGBA(writer, Palette[i]);
            }
        }
        #endregion
    }
}
