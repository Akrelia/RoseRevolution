using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EFT
{
    public string Name;
    public uint UseSoundFile;
    public string SoundFileName;
    public uint SoundLoopCount;

    public List<SystemEntry> Systems = new();
    public List<AnimationEntry> Animations = new();

    public void Read(BinaryReader r)
    {
        Name = ReadLStr(r);
        UseSoundFile = r.ReadUInt32();
        SoundFileName = ReadLStr(r);
        SoundLoopCount = r.ReadUInt32();

        uint systemCount = r.ReadUInt32();
        for (int i = 0; i < systemCount; i++)
        {
            SystemEntry s = new()
            {
                Name = ReadLStr(r),
                UniqueName = ReadLStr(r),
                StbIndex = r.ReadUInt32(),
                PtlFile = ReadLStr(r),
                UseAnimation = r.ReadUInt32(),
                ZmoFile = ReadLStr(r),
                AnimationLoopCount = r.ReadUInt32(),
                AnimationStbIndex = r.ReadUInt32(),
                Position = ReadVector3(r),
                Rotation = ReadQuaternion(r),
                Delay = r.ReadUInt32(),
                IsLinked = r.ReadUInt32()
            };
            Systems.Add(s);
        }

        uint animationCount = r.ReadUInt32();
        for (int i = 0; i < animationCount; i++)
        {
            AnimationEntry a = new()
            {
                Name = ReadLStr(r),
                UniqueName = ReadLStr(r),
                StbIndex = r.ReadUInt32(),
                ZmsFile = ReadLStr(r),
                ZmoFile = ReadLStr(r),
                DdsFile = ReadLStr(r),
                AlphaEnabled = r.ReadUInt32(),
                TwoSided = r.ReadUInt32(),
                AlphaTestEnabled = r.ReadUInt32(),
                ZTestEnabled = r.ReadUInt32(),
                ZWriteEnabled = r.ReadUInt32(),
                SourceBlend = (PTL.BlendMode)r.ReadUInt32(),
                DestinationBlend = (PTL.BlendMode)r.ReadUInt32(),
                BlendOp = (PTL.BlendOpType)r.ReadUInt32(),
                UseAnimation = r.ReadUInt32(),
                AnimationName = ReadLStr(r),
                AnimationLoopCount = r.ReadUInt32(),
                AnimationStbIndex = r.ReadUInt32(),
                Position = ReadVector3(r),
                Rotation = ReadQuaternion(r),
                Delay = r.ReadUInt32(),
                RepeatCount = r.ReadUInt32(),
                IsLinked = r.ReadUInt32()
            };
            Animations.Add(a);
        }
    }

    static string ReadLStr(BinaryReader r) => System.Text.Encoding.UTF8.GetString(r.ReadBytes(r.ReadInt32()));
    static Vector3 ReadVector3(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    static Quaternion ReadQuaternion(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle(), r.ReadSingle());

    public class SystemEntry
    {
        public string Name;
        public string UniqueName;
        public uint StbIndex;
        public string PtlFile;
        public uint UseAnimation;
        public string ZmoFile;
        public uint AnimationLoopCount;
        public uint AnimationStbIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public uint Delay;
        public uint IsLinked;
    }

    public class AnimationEntry
    {
        public string Name;
        public string UniqueName;
        public uint StbIndex;
        public string ZmsFile;
        public string ZmoFile;
        public string DdsFile;
        public uint AlphaEnabled;
        public uint TwoSided;
        public uint AlphaTestEnabled;
        public uint ZTestEnabled;
        public uint ZWriteEnabled;
        public PTL.BlendMode SourceBlend;
        public PTL.BlendMode DestinationBlend;
        public PTL.BlendOpType BlendOp;
        public uint UseAnimation;
        public string AnimationName;
        public uint AnimationLoopCount;
        public uint AnimationStbIndex;
        public Vector3 Position;
        public Quaternion Rotation;
        public uint Delay;
        public uint RepeatCount;
        public uint IsLinked;
    }
}