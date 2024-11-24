using MultiCraft.Scripts.Core.Blocks;
using MultiCraft.Scripts.Core.Worlds;
using MultiCraft.Scripts.Utils;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Players
{
    public class DestroyAndPlaceBlockController : MonoBehaviour
    {
        private static readonly int DamageAmount = Shader.PropertyToID("_DamageAmount");
        public GameObject destroyedBlockPrefab;

        public Inventories.Inventory playerInventory;

        private GameObject _destroyBlock;

        private Material _material;

        public float breakSpeed = 1f;
        public float MiningMultiplier = 1;
        private float _currentDamage = 0f;
        private Block _currentBlock;
        private Vector3 _targetBlockPosition;

        public float placeDelay = 0.5f;
        private float _nextPlaceTime = 0f;

        public void Start()
        {
            if (_destroyBlock == null)
                _destroyBlock = Instantiate(destroyedBlockPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f));

            _destroyBlock.SetActive(false);
            _material = _destroyBlock.GetComponent<MeshRenderer>().material;
        }

        private void OnDisable()
        {
            if (_destroyBlock != null)
                _destroyBlock.SetActive(false);
            _currentBlock = null;
            _currentDamage = 0f;
        }

        private void Update()
        {
            var selectedItem = playerInventory.GetSelectedItem();
            if (selectedItem != null)
            {
                MiningMultiplier = selectedItem.Item.MiningMultiplier;
                MiningMultiplier = MiningMultiplier == 0 ? 1 : MiningMultiplier;
            }
            else
            {
                MiningMultiplier = 1;
            }

            if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f))
            {
                var inCube = hitInfo.point - (hitInfo.normal * 0.5f);
                var removeBlock = Vector3Int.FloorToInt(inCube);

                _destroyBlock.transform.position = removeBlock + new Vector3(.5f, .5f, .5f);
            }
            else
            {
                _destroyBlock.SetActive(false);
            }

            if (Input.GetMouseButton(1))
            {
                //TryOpenBlock();
                TryPlaceBlock();
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _nextPlaceTime = 0f;
            }

            if (Input.GetMouseButton(0))
            {
                TryDestroyBlock();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _currentDamage = 0f;
                _currentBlock = null;
                _destroyBlock.SetActive(false);
            }
        }

        private void TryPlaceBlock()
        {
            if (Time.time < _nextPlaceTime) return;

            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f)) return;

            var blockPosition = hitInfo.point + hitInfo.normal * 0.5f;
            if (Vector3Int.FloorToInt(transform.position) != Vector3Int.FloorToInt(blockPosition) &&
                Vector3Int.FloorToInt(transform.position + Vector3.down) != Vector3Int.FloorToInt(blockPosition))
            {
                var selectedItem = playerInventory.GetSelectedItem();
                if (selectedItem != null)
                    if (selectedItem.Item != null)
                        if (selectedItem.Item.IsPlaceable)
                        {
                            World.Instance.SpawnBlock(blockPosition, selectedItem.Item.BlockType.Id);
                            playerInventory.RemoveSelectedItem();
                        }

                _nextPlaceTime = Time.time + placeDelay;
            }
        }

        private void TryOpenBlock()
        {
            if (Time.time < _nextPlaceTime) return;

            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f)) return;

            var blockPosition = hitInfo.point + hitInfo.normal * 0.5f;

            if (Vector3Int.FloorToInt(transform.position) != Vector3Int.FloorToInt(blockPosition) &&
                Vector3Int.FloorToInt(transform.position + Vector3.down) != Vector3Int.FloorToInt(blockPosition))
            {
                World.Instance.SpawnBlock(blockPosition, 2);
                _nextPlaceTime = Time.time + placeDelay;
            }
        }


        private void TryDestroyBlock()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f)) return;
            _destroyBlock.SetActive(true);
            var blockPosition = Vector3Int.FloorToInt(hitInfo.point - hitInfo.normal * 0.5f);

            if (_currentBlock == null || _targetBlockPosition != blockPosition)
            {
                _currentDamage = 0f;
                _targetBlockPosition = blockPosition;
                _currentBlock = World.Instance.GetBlockAtPosition(blockPosition);
            }

            if (_currentBlock.Durability > 0) _currentDamage += breakSpeed * Time.deltaTime * MiningMultiplier;
            _material.SetFloat(DamageAmount, _currentDamage / _currentBlock.Durability);

            if (_currentDamage >= _currentBlock.Durability && _currentBlock.Durability > 0)
            {
                playerInventory.AddItem(
                    ItemManager.Instance.GetItem(BlockManager.GetBlock(World.Instance.DestroyBlock(blockPosition))
                        .DropItem), 1);
                playerInventory.RemoveDurability();
                _currentDamage = 0f;
                _currentBlock = null;
                _destroyBlock.SetActive(false);
            }
        }
    }
}