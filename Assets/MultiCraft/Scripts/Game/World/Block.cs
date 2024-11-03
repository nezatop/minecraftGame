using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Game.World
{
    [CreateAssetMenu(menuName = "MultiCraft/World/Blocks/Normal Block")]
    public class Block : ScriptableObject
    {
        [FormerlySerializedAs("BlockType")] public BlockType blockType;
        [FormerlySerializedAs("PixelsOffset")] public Vector2Int pixelsOffset;

        [FormerlySerializedAs("Hardness")] public int hardness = -1;

        [FormerlySerializedAs("DestroySound")] public AudioClip destroySound;
        [FormerlySerializedAs("SpawnSound")] public AudioClip spawnSound;
        [FormerlySerializedAs("StepSound")] public AudioClip stepSound;

        [FormerlySerializedAs("DropItems")] public int[] dropItems;

        public virtual Vector2Int GetPixelsOffset(Vector3Int normal)
        {
            return pixelsOffset;
        }
    }
    
}