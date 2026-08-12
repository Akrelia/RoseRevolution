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

        private struct PixelFormat
        {
            public uint Flags;
            public uint FourCC;
            public uint RGBBitCount;
            public uint RMask, GMask, BMask, AMask;
        }

        public static Texture2D LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Debug.LogWarning("RoseDdsLoader: File not found: " + path);

                return null;
            }

            try
            {
                var texture = LoadFromBytes(File.ReadAllBytes(path));

                texture.name = Path.GetFileNameWithoutExtension(path);

                return texture;
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

            reader.BaseStream.Seek(11 * 4, SeekOrigin.Current); // reserved

            var pf = new PixelFormat();
            uint pfSize = reader.ReadUInt32();
            pf.Flags = reader.ReadUInt32();
            pf.FourCC = reader.ReadUInt32();
            pf.RGBBitCount = reader.ReadUInt32();
            pf.RMask = reader.ReadUInt32();
            pf.GMask = reader.ReadUInt32();
            pf.BMask = reader.ReadUInt32();
            pf.AMask = reader.ReadUInt32();

            reader.BaseStream.Seek(5 * 4, SeekOrigin.Current); // caps

            byte[] pixelData = reader.ReadBytes((int)(ddsBytes.Length - stream.Position));

            bool compressed = (pf.Flags & 0x4) != 0; // DDPF_FOURCC

            Texture2D tex;

            if (compressed)
            {
                TextureFormat format = pf.FourCC switch
                {
                    FOURCC_DXT1 => TextureFormat.DXT1,
                    FOURCC_DXT3 or FOURCC_DXT5 => TextureFormat.DXT5,
                    _ => throw new NotSupportedException($"Unsupported FourCC: 0x{pf.FourCC:X8}")
                };

                bool hasMipmaps = mipMapCount > 1;
                tex = new Texture2D((int)width, (int)height, format, hasMipmaps, false);
                tex.LoadRawTextureData(pixelData);
                tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
                tex = FlipCompressed(tex);
            }
            else
            {
                tex = DecodeUncompressed((int)width, (int)height, pixelData, pf);
                tex = FlipUncompressed(tex);
            }

            ConfigureTexture(tex);
            return tex;
        }

        private static Texture2D DecodeUncompressed(int width, int height, byte[] pixelData, PixelFormat pf)
        {
            int bytesPerPixel = (int)(pf.RGBBitCount / 8);
            if (bytesPerPixel < 1 || bytesPerPixel > 4)
                throw new NotSupportedException($"Unsupported uncompressed bit depth: {pf.RGBBitCount}");

            bool hasAlpha = pf.AMask != 0 && (pf.Flags & 0x1) != 0; // DDPF_ALPHAPIXELS

            var (rShift, rBits) = MaskToShiftAndBits(pf.RMask);
            var (gShift, gBits) = MaskToShiftAndBits(pf.GMask);
            var (bShift, bBits) = MaskToShiftAndBits(pf.BMask);
            var (aShift, aBits) = hasAlpha ? MaskToShiftAndBits(pf.AMask) : (0, 0);

            var rgba = new byte[width * height * 4];
            int srcStride = width * bytesPerPixel;

            for (int y = 0; y < height; y++)
            {
                int srcRowStart = y * srcStride;
                int dstRowStart = y * width * 4;

                for (int x = 0; x < width; x++)
                {
                    int srcOffset = srcRowStart + x * bytesPerPixel;
                    if (srcOffset + bytesPerPixel > pixelData.Length)
                        continue; // truncated/corrupt file - leave as transparent black rather than throw

                    uint pixel = 0;
                    for (int b = 0; b < bytesPerPixel; b++)
                        pixel |= (uint)pixelData[srcOffset + b] << (b * 8);

                    byte r = ExtractChannel(pixel, pf.RMask, rShift, rBits);
                    byte g = ExtractChannel(pixel, pf.GMask, gShift, gBits);
                    byte b2 = ExtractChannel(pixel, pf.BMask, bShift, bBits);
                    byte a = hasAlpha ? ExtractChannel(pixel, pf.AMask, aShift, aBits) : (byte)255;

                    int dstOffset = dstRowStart + x * 4;
                    rgba[dstOffset + 0] = r;
                    rgba[dstOffset + 1] = g;
                    rgba[dstOffset + 2] = b2;
                    rgba[dstOffset + 3] = a;
                }
            }

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            tex.LoadRawTextureData(rgba);
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return tex;
        }

        private static (int shift, int bits) MaskToShiftAndBits(uint mask)
        {
            if (mask == 0) return (0, 0);

            int shift = 0;
            while ((mask & 1) == 0) { mask >>= 1; shift++; }

            int bits = 0;
            while ((mask & 1) == 1) { mask >>= 1; bits++; }

            return (shift, bits);
        }

        /// <summary>
        /// Extracts a channel from a raw pixel value and rescales it to a full 0-255
        /// byte, regardless of the source bit width (1-bit alpha, 4-bit, 5-bit, 8-bit,
        /// all handled the same way).
        /// </summary>
        private static byte ExtractChannel(uint pixel, uint mask, int shift, int bits)
        {
            if (bits == 0) return 255;

            uint raw = (pixel & mask) >> shift;
            uint maxValue = (1u << bits) - 1;

            // Rescale raw value (0..maxValue) to 0..255
            return (byte)(raw * 255 / maxValue);
        }

        private static Texture2D FlipCompressed(Texture2D tex)
        {
            RenderTexture rt = RenderTexture.GetTemporary(tex.width, tex.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture.active = rt;

            Graphics.Blit(tex, rt, new Vector2(1, -1), new Vector2(0, 1));

            Texture2D flipped = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, false, false);
            flipped.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
            flipped.Apply();

            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            return flipped;
        }

        private static Texture2D FlipUncompressed(Texture2D tex)
        {
            Color32[] pixels = tex.GetPixels32();
            Color32[] flipped = new Color32[pixels.Length];
            int w = tex.width;
            int h = tex.height;

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    flipped[y * w + x] = pixels[(h - 1 - y) * w + x];

            tex.SetPixels32(flipped);
            tex.Apply();
            return tex;
        }

        private static void ConfigureTexture(Texture2D tex)
        {
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.anisoLevel = 8;
        }
    }
}