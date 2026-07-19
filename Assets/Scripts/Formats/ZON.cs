using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityRose.RoseFile;
using UnityRose.Import;

namespace UnityRose.Formats
{
    /// <summary>
    /// ZON class.
    /// </summary>
    // NOTE: this used to be wrapped in #if UNITY_EDITOR for no real reason - it's a binary
    // format parser, same as ZMS/ZMD/ZMO/CHR which were never gated. That gate is exactly
    // why RosePatch.Load()/UpdateAtlas(), which use ZON.Textures[...].Tex, could only ever
    // run in the Editor, and why they were pulling texture paths through the old
    // ROSEImport.ImportTexture (with its own un-synced dataPath) instead of RoseDataSource.
    public class ZON
    {
        /// <summary>
        /// Block type.
        /// </summary>
        public enum BlockType
        {
            /// <summary>
            /// Misc. information.
            /// </summary>
            Block0,

            /// <summary>
            /// Spawn points.
            /// </summary>
            SpawnPoints,

            /// <summary>
            /// Textures.
            /// </summary>
            Textures,

            /// <summary>
            /// Tiles.
            /// </summary>
            Tiles,

            /// <summary>
            /// Economy information.
            /// </summary>
            Economy
        }

        /// <summary>
        /// Tile rotation type.
        /// </summary>
        public enum RotationType
        {
            /// <summary>
            /// Normal.
            /// </summary>
            Normal = 1,

            /// <summary>
            /// Left to right.
            /// </summary>
            LeftRight = 2,

            /// <summary>
            /// Top to bottom.
            /// </summary>
            TopBottom = 3,

            /// <summary>
            /// Left to right and top to bottom.
            /// </summary>
            LeftRightTopBottom = 4,

            /// <summary>
            /// 90 degrees clockwise.
            /// </summary>
            Rotate90Clockwise = 5,

            /// <summary>
            /// 90 degrees counter clockwise.
            /// </summary>
            Rotate90CounterClockwise = 6
        }

        #region Sub Classes

        public class Block
        {
            public BlockType Type { get; set; }
            public int Offset { get; set; }
        }

        public class Block0
        {
            public struct ZonePart
            {
                public byte UseMap { get; set; }
                public Vector2 Position { get; set; }
            };

            public int ZoneType { get; set; }
            public int ZoneWidth { get; set; }
            public int ZoneHeight { get; set; }
            public int GridCount { get; set; }
            public float GridSize { get; set; }
            public int XCount { get; set; }
            public int YCount { get; set; }
            public ZonePart[,] ZoneParts { get; set; }
        }

        public class SpawnPoint
        {
            public Vector3 Position { get; set; }
            public string Name { get; set; }
        }

        public class Texture
        {
            public Texture2D Tex { get; set; }
            public string TexPath { get; set; }
        }

        public class Tile
        {
            public int BaseID1 { get; set; }
            public int BaseID2 { get; set; }
            public int Offset1 { get; set; }
            public int Offset2 { get; set; }

            public int ID1 { get { return BaseID1 + Offset1; } }
            public int ID2 { get { return BaseID2 + Offset2; } }

            public bool IsBlending { get; set; }
            public RotationType Rotation { get; set; }
            public int TileType { get; set; }
        }

        public class Economy
        {
            public string AreaName { get; set; }
            public int IsUnderground { get; set; }
            public string ButtonBGM { get; set; }
            public string ButtonBack { get; set; }
            public int CheckCount { get; set; }
            public int StandardPopulation { get; set; }
            public int StandardGrowthRate { get; set; }
            public int MetalConsumption { get; set; }
            public int StoneConsumption { get; set; }
            public int WoodConsumption { get; set; }
            public int LeatherConsumption { get; set; }
            public int ClothConsumption { get; set; }
            public int AlchemyConsumption { get; set; }
            public int ChemicalConsumption { get; set; }
            public int IndustrialConsumption { get; set; }
            public int MedicineConsumption { get; set; }
            public int FoodConsumption { get; set; }
        }

        #endregion

        public string FilePath { get; set; }
        public string RootPath { get; set; }

        public List<Block> Blocks { get; set; }
        public Block0 ZoneInfo { get; set; }
        public List<SpawnPoint> SpawnPoints { get; set; }
        public List<Texture> Textures { get; set; }
        public List<Tile> Tiles { get; set; }
        public Economy EconomyInfo { get; set; }

        public ZON()
        {
        }

        public ZON(string filePath)
        {
            string rootPath = "";
            char[] sep = { '/' };
            string[] tokens = filePath.Split(sep);
            for (int i = 0; i < tokens.Length; i++)
            {
                if (tokens[i].ToLower() == "3ddata")
                    break;

                rootPath += tokens[i] + "/";
            }

            RootPath = rootPath;
            RootPath = "Assets/";
            Load(filePath);
        }

        public void Load(string filePath)
        {
            FileHandler fh = new FileHandler(FilePath = filePath, FileHandler.FileOpenMode.Reading, Encoding.UTF8);

            int blockCount = fh.Read<int>();

            Blocks = new List<Block>(blockCount);

            for (int i = 0; i < blockCount; i++)
            {
                Blocks.Add(new Block()
                {
                    Type = (BlockType)fh.Read<int>(),
                    Offset = fh.Read<int>()
                });
            }

            for (int i = 0; i < blockCount; i++)
            {
                fh.Seek(Blocks[i].Offset, SeekOrigin.Begin);

                switch (Blocks[i].Type)
                {
                    case BlockType.Block0:
                        {
                            ZoneInfo = new Block0()
                            {
                                ZoneType = fh.Read<int>(),
                                ZoneWidth = fh.Read<int>(),
                                ZoneHeight = fh.Read<int>(),
                                GridCount = fh.Read<int>(),
                                GridSize = fh.Read<float>(),

                                XCount = fh.Read<int>(),
                                YCount = fh.Read<int>()
                            };

                            ZoneInfo.ZoneParts = new Block0.ZonePart[ZoneInfo.ZoneWidth, ZoneInfo.ZoneHeight];

                            for (int j = 0; j < ZoneInfo.ZoneWidth; j++)
                            {
                                for (int k = 0; k < ZoneInfo.ZoneHeight; k++)
                                {
                                    ZoneInfo.ZoneParts[j, k] = new Block0.ZonePart()
                                    {
                                        UseMap = fh.Read<byte>(),
                                        Position = fh.Read<Vector2>()
                                    };
                                }
                            }
                        }
                        break;
                    case BlockType.SpawnPoints:
                        {
                            int spawnPointCount = fh.Read<int>();
                            SpawnPoints = new List<SpawnPoint>(spawnPointCount);

                            for (int j = 0; j < spawnPointCount; j++)
                            {
                                SpawnPoints.Add(new SpawnPoint()
                                {
                                    Position = new Vector3()
                                    {
                                        x = (fh.Read<float>() + 520000.00f) / 100.0f,
                                        z = fh.Read<float>() / 100.0f,
                                        y = (fh.Read<float>() + 520000.00f) / 100.0f
                                    },

                                    Name = fh.Read<BString>()
                                });
                            }
                        }
                        break;
                    case BlockType.Textures:
                        {
                            int textureCount = fh.Read<int>();
                            Textures = new List<Texture>(textureCount);

                            for (int j = 0; j < textureCount; j++)
                            {
                                Texture tex = new Texture();
                                string path = fh.Read<BString>();

                                // was: Utils.ResolvePathWithCorrectCase(ROSEImport.GetDataPath(), path)
                                // ROSEImport.GetDataPath() is the old, unsynced static path -
                                // RoseDataSource.DataPath is the one every importer now shares.
                                string resolvedPath = Utils.ResolvePathWithCorrectCase(RoseDataSource.DataPath, path);

                                if (System.IO.File.Exists(resolvedPath))
                                {
                                    string relativePath = Path.GetRelativePath(RoseDataSource.DataPath, resolvedPath);
                                    // was: Utils.duplicateTexture(ROSEImport.ImportTexture(relativePath, false))
                                    tex.Tex = Utils.duplicateTexture(RoseTextureImporter.Import(relativePath));
                                }
                                else
                                {
                                    if (path != "end")
                                    {
                                        Debug.Log("Missing ZON Texture : " + path + " (resolved to: " + resolvedPath + ")");
                                    }
                                }

                                tex.TexPath = path;

                                Textures.Add(tex);
                            }
                        }
                        break;
                    case BlockType.Tiles:
                        {
                            int tileCount = fh.Read<int>();
                            Tiles = new List<Tile>(tileCount);

                            for (int j = 0; j < tileCount; j++)
                            {
                                Tiles.Add(new Tile()
                                {
                                    BaseID1 = fh.Read<int>(),
                                    BaseID2 = fh.Read<int>(),
                                    Offset1 = fh.Read<int>(),
                                    Offset2 = fh.Read<int>(),
                                    IsBlending = fh.Read<int>() > 0,
                                    Rotation = (RotationType)fh.Read<int>(),
                                    TileType = fh.Read<int>()
                                });
                            }
                        }
                        break;
                    case BlockType.Economy:
                        {
                            try
                            {
                                EconomyInfo = new Economy()
                                {
                                    AreaName = fh.Read<BString>(),
                                    IsUnderground = fh.Read<int>(),
                                    ButtonBGM = fh.Read<BString>(),
                                    ButtonBack = fh.Read<BString>(),
                                    CheckCount = fh.Read<int>(),
                                    StandardPopulation = fh.Read<int>(),
                                    StandardGrowthRate = fh.Read<int>(),
                                    MetalConsumption = fh.Read<int>(),
                                    StoneConsumption = fh.Read<int>(),
                                    WoodConsumption = fh.Read<int>(),
                                    LeatherConsumption = fh.Read<int>(),
                                    ClothConsumption = fh.Read<int>(),
                                    AlchemyConsumption = fh.Read<int>(),
                                    ChemicalConsumption = fh.Read<int>(),
                                    IndustrialConsumption = fh.Read<int>(),
                                    MedicineConsumption = fh.Read<int>(),
                                    FoodConsumption = fh.Read<int>()
                                };
                            }
                            catch
                            {
                                Debug.LogError("-- Error reading the Economy Info block");

                                EconomyInfo = new Economy()
                                {
                                    AreaName = string.Empty,
                                    IsUnderground = 0,
                                    ButtonBGM = string.Empty,
                                    ButtonBack = string.Empty,
                                    CheckCount = 0,
                                    StandardPopulation = 0,
                                    StandardGrowthRate = 0,
                                    MetalConsumption = 0,
                                    StoneConsumption = 0,
                                    WoodConsumption = 0,
                                    LeatherConsumption = 0,
                                    ClothConsumption = 0,
                                    AlchemyConsumption = 0,
                                    ChemicalConsumption = 0,
                                    IndustrialConsumption = 0,
                                    MedicineConsumption = 0,
                                    FoodConsumption = 0
                                };
                            }
                        }
                        break;
                }
            }

            fh.Close();
        }

        public void Save()
        {
            Save(FilePath);
        }

        public void Save(string filePath)
        {
            FileHandler fh = new FileHandler(FilePath = filePath, FileHandler.FileOpenMode.Writing, Encoding.UTF8);

            fh.Write<int>(Blocks.Count);

            for (int i = 0; i < Blocks.Count; i++)
            {
                fh.Write<int>(0);
                fh.Write<int>(0);
            }

            for (int i = 0; i < Blocks.Count; i++)
            {
                Blocks[i].Offset = fh.Tell();

                switch (Blocks[i].Type)
                {
                    case BlockType.Block0:
                        {
                            fh.Write<int>(ZoneInfo.ZoneType);
                            fh.Write<int>(ZoneInfo.ZoneWidth);
                            fh.Write<int>(ZoneInfo.ZoneHeight);
                            fh.Write<int>(ZoneInfo.GridCount);
                            fh.Write<float>(ZoneInfo.GridSize);

                            fh.Write<int>(ZoneInfo.XCount);
                            fh.Write<int>(ZoneInfo.YCount);

                            for (int x = 0; x < ZoneInfo.ZoneWidth; x++)
                            {
                                for (int y = 0; y < ZoneInfo.ZoneHeight; y++)
                                {
                                    fh.Write<byte>(ZoneInfo.ZoneParts[x, y].UseMap);
                                    fh.Write<Vector2>(ZoneInfo.ZoneParts[x, y].Position);
                                }
                            }
                        }
                        break;
                    case BlockType.SpawnPoints:
                        {
                            fh.Write<int>(SpawnPoints.Count);

                            for (int j = 0; j < SpawnPoints.Count; j++)
                            {
                                fh.Write<float>(-520000.0f + (SpawnPoints[j].Position.x * 100.0f));
                                fh.Write<float>(SpawnPoints[j].Position.z * 100.0f);
                                fh.Write<float>(-520000.0f + (SpawnPoints[j].Position.y * 100.0f));

                                fh.Write<BString>(SpawnPoints[j].Name);
                            }
                        }
                        break;
                    case BlockType.Textures:
                        {
                            fh.Write<int>(Textures.Count);

                            for (int j = 0; j < Textures.Count; j++)
                                fh.Write<BString>(Textures[j].TexPath);
                        }
                        break;
                    case BlockType.Tiles:
                        {
                            fh.Write<int>(Tiles.Count);

                            for (int j = 0; j < Tiles.Count; j++)
                            {
                                fh.Write<int>(Tiles[j].BaseID1);
                                fh.Write<int>(Tiles[j].BaseID2);
                                fh.Write<int>(Tiles[j].Offset1);
                                fh.Write<int>(Tiles[j].Offset2);
                                fh.Write<int>(Tiles[j].IsBlending ? 1 : 0);
                                fh.Write<int>((int)Tiles[j].Rotation);
                                fh.Write<int>(Tiles[j].TileType);
                            }
                        }
                        break;
                    case BlockType.Economy:
                        {
                            fh.Write<BString>(EconomyInfo.AreaName);
                            fh.Write<int>(EconomyInfo.IsUnderground);
                            fh.Write<BString>(EconomyInfo.ButtonBGM);
                            fh.Write<BString>(EconomyInfo.ButtonBack);
                            fh.Write<int>(EconomyInfo.CheckCount);
                            fh.Write<int>(EconomyInfo.StandardPopulation);
                            fh.Write<int>(EconomyInfo.StandardGrowthRate);
                            fh.Write<int>(EconomyInfo.MetalConsumption);
                            fh.Write<int>(EconomyInfo.StoneConsumption);
                            fh.Write<int>(EconomyInfo.WoodConsumption);
                            fh.Write<int>(EconomyInfo.LeatherConsumption);
                            fh.Write<int>(EconomyInfo.ClothConsumption);
                            fh.Write<int>(EconomyInfo.AlchemyConsumption);
                            fh.Write<int>(EconomyInfo.ChemicalConsumption);
                            fh.Write<int>(EconomyInfo.IndustrialConsumption);
                            fh.Write<int>(EconomyInfo.MedicineConsumption);
                            fh.Write<int>(EconomyInfo.FoodConsumption);
                        }
                        break;
                }
            }

            fh.Seek(4, SeekOrigin.Begin);

            for (int i = 0; i < Blocks.Count; i++)
            {
                fh.Write<int>((int)Blocks[i].Type);
                fh.Write<int>(Blocks[i].Offset);
            }

            fh.Close();
        }
    }
}