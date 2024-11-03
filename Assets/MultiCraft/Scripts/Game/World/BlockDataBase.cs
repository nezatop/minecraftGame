using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Game.World
{
    
    [CreateAssetMenu(menuName = "MultiCraft/World/Blocks/BlockDataBase")]
    public class BlockDataBase : ScriptableObject
    {
        [FormerlySerializedAs("pixelsOffset")] public Vector2 textureAtlasSize;
        [FormerlySerializedAs("TextureResolution")] public float textureResolution;
        [FormerlySerializedAs("Block")] public Block[] blocks;

        private Dictionary<BlockType, Block> _blockDictionary = new Dictionary<BlockType, Block>();

        private void InitializeBlockDictionary()
        {
            _blockDictionary.Clear();

            foreach (var block in blocks)
            {
                _blockDictionary.Add(block.blockType, block);
            }
        }

        public Block GetBlock(BlockType blockType)
        {
            if (_blockDictionary.Count == 0) InitializeBlockDictionary();

            return _blockDictionary.TryGetValue(blockType, out var block) ? block : null;
        }
    }
}