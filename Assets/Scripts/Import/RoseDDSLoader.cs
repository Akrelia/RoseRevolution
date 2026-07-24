using System;
using System.IO;
using UnityEngine;

namespace UnityRose.Import
{
    public static class RoseDdsLoader
    {
        private const uint DDS_MAGIC = 0x20534444;
        private const uint FOURCC_DXT1 = 0x31545844;
        private const uint FOURCC_DXT3 = 0x33545844;
        private const uint FOURCC_DXT5 = 0x35545844;

        public static Texture2D LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("RoseDdsLoader: File not found: " + path);

                return null;
            }

            try
            {
                return LoadFromBytes(File.ReadAllBytes(path));
            }

            catch (Exception ex)
            {
                Debug.LogWarning($"RoseDdsLoader: Failed to load '{path}': {ex.Message}");

                return null;
            }
        }

        public static Texture2D LoadFromBytes(byte[] ddsBytes)
        {
            using var stream = new MemoryStream(ddsBytes);
            using var reader = new BinaryReader(stream);

            if (reader.ReadUInt32() != DDS_MAGIC)
                throw new InvalidDataException("Not a valid DDS file.");

            uint headerSize = reader.ReadUInt32();
            uint flags = reader.ReadUInt32();
            uint height = reader.ReadUInt32();
            uint width = reader.ReadUInt32();
            uint pitchOrLinearSize = reader.ReadUInt32();
            uint depth = reader.ReadUInt32();
            uint mipMapCount = reader.ReadUInt32();

            reader.BaseStream.Seek(11 * 4, SeekOrigin.Current);

            // Pixel Format
            uint pfSize = reader.ReadUInt32();
            uint pfFlags = reader.ReadUInt32();
            uint fourCC = reader.ReadUInt32();
            uint rgbBitCount = reader.ReadUInt32();
            reader.BaseStream.Seek(4 * 4, SeekOrigin.Current); // masks

            reader.BaseStream.Seek(5 * 4, SeekOrigin.Current); // caps

            byte[] pixelData = reader.ReadBytes((int)(ddsBytes.Length - stream.Position));

            TextureFormat format = GetTextureFormat(fourCC, rgbBitCount, pfFlags);

            bool hasMipmaps = mipMapCount > 1;

            var tex = new Texture2D((int)width, (int)height, format, hasMipmaps, false);
            tex.LoadRawTextureData(pixelData);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: true);

            tex = FlipVertically(tex, format);
            ConfigureTexture(tex, format);

            return tex;
        }

        private static TextureFormat GetTextureFormat(uint fourCC, uint rgbBitCount, uint pfFlags)
        {
            bool compressed = (pfFlags & 0x4) != 0;

            if (compressed)
            {
                return fourCC switch
                {
                    FOURCC_DXT1 => TextureFormat.DXT1,
                    FOURCC_DXT3 or FOURCC_DXT5 => TextureFormat.DXT5,
                    _ => throw new NotSupportedException($"Unsupported FourCC: 0x{fourCC:X8}")
                };
            }

            if (rgbBitCount == 32) return TextureFormat.BGRA32;

            throw new NotSupportedException("Unsupported texture format.");
        }

        /// <summary>
        /// Flip vertical if texture if compresed (DXT) or not. This is necessary because DDS textures are stored upside down compared to Unity's texture coordinate system.
        /// </summary>
        private static Texture2D FlipVertically(Texture2D tex, TextureFormat format)
        {
            // Flip vertical via GPU for compressed textures (DXT1, DXT5)
            if (format == TextureFormat.DXT1 || format == TextureFormat.DXT5)
            {
                RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture.active = rt;

                Graphics.Blit(tex, rt, new Vector2(1, -1), new Vector2(0, 1));

                Texture2D flipped = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, tex.mipmapCount > 1, false);
                flipped.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
                flipped.Apply();

                RenderTexture.ReleaseTemporary(rt);
                UnityEngine.Object.DestroyImmediate(tex);

                return flipped;
            }
            else
            {
                Color[] pixels = tex.GetPixels();
                Color[] flipped = new Color[pixels.Length];
                int w = tex.width;
                int h = tex.height;

                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        flipped[y * w + x] = pixels[(h - 1 - y) * w + x];

                tex.SetPixels(flipped);
                tex.Apply();
                return tex;
            }
        }

        private static void ConfigureTexture(Texture2D tex, TextureFormat format)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 8;

            if (format == TextureFormat.DXT5 || format == TextureFormat.BGRA32)
            {
                //tex.alphaIsTransparency = true;
            }
        }
    }
}