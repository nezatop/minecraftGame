using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Blocks
{
    public static class BlockDataBase
    {
        public static Dictionary<BlockType, Block> Blocks;
        public static Vector2Int TextureAtlasSize;
        public static float TextureResolution;
        public static Material BlockMaterial;

        public static void InitializeBlockDataBase()
        {
            Blocks = new Dictionary<BlockType, Block>();

            BlockMaterial = Resources.Load<Material>("BlockMaterial");

            TextAsset textureData = Resources.Load<TextAsset>("Texture");
            if (textureData != null)
            {
                var atlasInfo = JsonUtility.FromJson<TextureAtlasInfo>(textureData.text);
                TextureAtlasSize = atlasInfo.TextureAtlasSize;
                TextureResolution = atlasInfo.TextureResolution;
            }

            TextAsset[] jsonFiles = Resources.LoadAll<TextAsset>("Blocks");
            foreach (var jsonFile in jsonFiles)
            {
                if (jsonFile == null) continue;
                Block block = JsonUtility.FromJson<Block>(jsonFile.text);
                if (!Blocks.ContainsKey(block.Type))
                    Blocks.Add(block.Type, block);
            }
        }

        public static Block GetBlock(BlockType blockType)
        {
            if (Blocks == null) InitializeBlockDataBase();
            if (Blocks.Count == 0) InitializeBlockDataBase();
            return Blocks.GetValueOrDefault(blockType);
        }

        [System.Serializable]
        public class TextureAtlasInfo
        {
            public Vector2Int TextureAtlasSize;
            public float TextureResolution;
        }
    }
}