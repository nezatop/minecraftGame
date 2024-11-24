using System.Collections.Generic;
using System.IO;
using System.Linq;
using MultiCraft.Scripts.Core.Blocks;
using UnityEngine;

namespace MultiCraft.Scripts.Utils
{
    public class BlockManager : MonoBehaviour
    {
        public List<BlockScriptableObject> BlocksToLoad = new List<BlockScriptableObject>();

        public Material worldMaterial;

        private static Dictionary<int, Block> _blocks = new Dictionary<int, Block>();
        private static Texture2D _atlas;
        private static Dictionary<string, Vector2Int> _texturesPositionInAtlas = new Dictionary<string, Vector2Int>();

        public const int AtlasSize = 256;
        public const int TextureResolution = 16;

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            CreateAtlas();
            CreateBlocksBase();
            worldMaterial.mainTexture = _atlas;
        }

        private void CreateAtlas()
        {
            HashSet<Texture2D> texture2Ds = new HashSet<Texture2D>();
            foreach (var blockScriptableObject in BlocksToLoad)
            {
                texture2Ds.Add(blockScriptableObject.Textures[0]);
                texture2Ds.Add(blockScriptableObject.Textures[1]);
                texture2Ds.Add(blockScriptableObject.Textures[2]);
                texture2Ds.Add(blockScriptableObject.Textures[3]);
                texture2Ds.Add(blockScriptableObject.Textures[4]);
                texture2Ds.Add(blockScriptableObject.Textures[5]);
            }

            _atlas = new Texture2D(AtlasSize, AtlasSize)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };

            var currentX = 0;
            var currentY = 0;

            foreach (var texture in texture2Ds)
            {
                // Записываем в атлас
                _atlas.SetPixels(currentX * TextureResolution, currentY * TextureResolution,
                    TextureResolution, TextureResolution,
                    texture.GetPixels());

                _texturesPositionInAtlas[texture.name] = new Vector2Int(currentX, currentY);

                // Перемещаемся на следующую позицию в атласе
                currentX++;
                if (currentX >= AtlasSize / TextureResolution)
                {
                    currentX = 0;
                    currentY++;
                }
            }

            _atlas.Apply();
        }

        private void CreateBlocksBase()
        {
            foreach (var blockScriptableObject in BlocksToLoad)
            {
                var block = new Block();

                block.Id = blockScriptableObject.Id;
                block.Name = blockScriptableObject.name;
                block.ToolToDig = blockScriptableObject.ToolToDig;
                block.MinToolLevelToDig = blockScriptableObject.MinToolLevelToDig;
                block.Durability = blockScriptableObject.Durability;
                block.DropItem = blockScriptableObject.DropItem;
                block.StepSound = blockScriptableObject.StepSound;
                block.DigSound = blockScriptableObject.DigSound;
                block.PlaceSound = blockScriptableObject.PlaceSound;

                block.SetUvs(GetUv(blockScriptableObject.Textures));

                _blocks.Add(block.Id, block);
            }
        }

        private List<Vector2> GetUv(Texture2D[] textures)
        {
            return textures.Select(texture => _texturesPositionInAtlas[texture.name]).Select(dummy => (Vector2)dummy)
                .ToList();
        }

        public static Block GetBlock(int id)
        {
            return _blocks.GetValueOrDefault(id);
        }

        private static void SaveAtlasToFile(Texture2D atlas, string path)
        {
            var bytes = atlas.EncodeToPNG();

            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                if (directory != null) Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(path, bytes);
        }
    }
}