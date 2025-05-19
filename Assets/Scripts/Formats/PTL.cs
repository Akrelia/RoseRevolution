using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

public class PTL
{
    public List<Emitter> Emitters = new();

    public void Read(BinaryReader r)
    {
        uint emitterCount = r.ReadUInt32();
        for (int i = 0; i < emitterCount; i++)
        {
            Emitter e = new();
            e.Name = ReadLStr(r);
            e.LifeTime = new(r.ReadSingle(), r.ReadSingle());
            e.EmitRate = new(r.ReadSingle(), r.ReadSingle());
            e.LoopCount = r.ReadUInt32();
            e.MinSpawnDir = ReadVector3(r);
            e.MaxSpawnDir = ReadVector3(r);
            e.MinEmitRadius = ReadVector3(r);
            e.MaxEmitRadius = ReadVector3(r);
            e.MinGravity = ReadVector3(r);
            e.MaxGravity = ReadVector3(r);
            e.Texture = ReadLStr(r);
            e.ParticleNumber = r.ReadUInt32();
            e.AlignType = r.ReadUInt32();
            e.UpdateCoordinate = r.ReadUInt32();
            e.TextureWidth = r.ReadUInt32();
            e.TextureHeight = r.ReadUInt32();
            e.SpriteType = r.ReadUInt32();
            e.DestinationBlend = (BlendMode)r.ReadUInt32();
            e.SourceBlend = (BlendMode)r.ReadUInt32();
            e.BlendOp = (BlendOpType)r.ReadUInt32();
            uint infoCount = r.ReadUInt32();

            for (int j = 0; j < infoCount; j++)
            {
                ParticleInfo info = new();
                info.Type = (AnimType)r.ReadUInt32();
                info.TimeRange = r.ReadSingle();
                info.Fade = r.ReadByte();
                switch (info.Type)
                {
                    case AnimType.SIZE:
                        info.Size = new(r.ReadSingle(), r.ReadSingle()); break;
                    case AnimType.EVENTTIMER:
                    case AnimType.RED:
                    case AnimType.GREEN:
                    case AnimType.BLUE:
                    case AnimType.ALPHA:
                    case AnimType.VELOCITYX:
                    case AnimType.VELOCITYY:
                    case AnimType.VELOCITYZ:
                        info.Value = r.ReadSingle(); break;
                    case AnimType.COLOR:
                        info.ColorMinimum = ReadColor(r);
                        info.ColorMaximum = ReadColor(r); break;
                    case AnimType.VELOCITY:
                        info.Velocity = ReadVector3(r); break;
                    case AnimType.UV:
                        info.UV = new(r.ReadSingle(), r.ReadSingle()); break;
                }
                e.Infos.Add(info);
            }

            Emitters.Add(e);
        }
    }

    static string ReadLStr(BinaryReader r) => System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
    static Vector3 ReadVector3(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    static Color ReadColor(BinaryReader r) => new() { R = r.ReadByte(), G = r.ReadByte(), B = r.ReadByte(), A = r.ReadByte() };

    public class Emitter
    {
        public string Name;
        public Vector2 LifeTime, EmitRate;
        public uint LoopCount;
        public Vector3 MinSpawnDir, MaxSpawnDir;
        public Vector3 MinEmitRadius, MaxEmitRadius;
        public Vector3 MinGravity, MaxGravity;
        public string Texture;
        public uint ParticleNumber, AlignType, UpdateCoordinate;
        public uint TextureWidth, TextureHeight, SpriteType;
        public BlendMode DestinationBlend, SourceBlend;
        public BlendOpType BlendOp;
        public List<ParticleInfo> Infos = new();
    }

    public class ParticleInfo
    {
        public AnimType Type;
        public float TimeRange;
        public byte Fade;
        public Vector2 Size;
        public float Value;
        public Color ColorMinimum, ColorMaximum;
        public Vector3 Velocity;
        public Vector2 UV;
    }

    public enum BlendMode : uint
    {
        ZERO = 1, ONE, SRCCOLOR, INVSRCCOLOR, SRCALPHA,
        INVSRCALPHA, DESTALPHA, INVDESTALPHA, DESTCOLOR,
        INVDESTCOLOR, SRCALPHASAT, BOTHSRCALPHA, BOTHINVSRCALPHA,
        BLENDFACTOR, INVBLENDFACTOR, SRCCOLOR2, INVSRCCOLOR2
    }

    public enum BlendOpType : uint
    {
        ADD = 1, SUBTRACT, REVSUBTRACT, MIN, MAX
    }

    public enum AnimType : uint
    {
        SIZE = 1, EVENTTIMER, RED, GREEN, BLUE,
        ALPHA, COLOR, VELOCITYX, VELOCITYY, VELOCITYZ,
        VELOCITY, UV, TEXTUREINDEX
    }

    public struct Color
    {
        public byte R, G, B, A;
    }
}