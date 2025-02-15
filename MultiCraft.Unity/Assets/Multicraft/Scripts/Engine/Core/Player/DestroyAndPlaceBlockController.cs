﻿using System;
using System.Collections.Generic;
using MultiCraft.Scripts.Engine.Core.Blocks;
using MultiCraft.Scripts.Engine.Core.Entities;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Items;
using MultiCraft.Scripts.Engine.Core.Worlds;
using MultiCraft.Scripts.Engine.UI;
using MultiCraft.Scripts.Engine.Utils;
using UnityEngine;
using YG;

namespace MultiCraft.Scripts.Engine.Core.Player
{
    public class DestroyAndPlaceBlockController : MonoBehaviour
    {
        private static readonly int DamageAmount = Shader.PropertyToID("_DamageAmount");

        public HandAnimationController handAnimationController; // Ссылка на контроллер анимации

        public HungerSystem HungerSystem;

        public LayerMask worldLayer;
        public LayerMask mobLayer;

        public GameObject destroyedBlockPrefab;

        private Inventory _playerInventory;

        private GameObject _destroyBlock;

        private Material _material;

        public float breakSpeed = 1f;
        public float miningMultiplier = 1;
        private float _currentDamage = 0f;
        private Block _currentBlock;
        private Vector3 _targetBlockPosition;

        public float placeDelay = 0.5f;
        private float _nextPlaceTime = 0f;

        public bool _isSurvival = false;

        private bool _isBreaking;
        private bool _isAnimating;

        private float _nextDestroyTime = 0f;
        public float destroyDelayNonSurvival = 0.1f; // Задержка разрушения блоков в режиме без выживания


        private bool isMobile => YG2.envir.isMobile;

        public void Start()
        {
            if (_destroyBlock == null)
                _destroyBlock = Instantiate(destroyedBlockPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f));

            _playerInventory = gameObject.GetComponentInParent<Inventory>();

            _destroyBlock.SetActive(false);
            _material = _destroyBlock.GetComponent<MeshRenderer>().material;
        }
        
        public void StopBreaking()
        {
            _currentDamage = 0f;
            _currentBlock = null;
            if (_isSurvival) _destroyBlock.SetActive(false);
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
            if (isMobile) return;
            var selectedItem = _playerInventory.GetSelectedItem();
            if (selectedItem != null)
            {
                if (selectedItem.Item != null)
                {
                    miningMultiplier = selectedItem.Item.MiningMultiplier;
                    miningMultiplier = miningMultiplier == 0 ? 1 : miningMultiplier;
                }
            }
            else
            {
                miningMultiplier = 1;
            }

            if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f, worldLayer))
            {
                var inCube = hitInfo.point - (hitInfo.normal * 0.5f);
                var removeBlock = Vector3Int.FloorToInt(inCube);

                _destroyBlock.transform.position = removeBlock + new Vector3(.5f, .5f, .5f);
            }
            else
            {
                if (_isSurvival) _destroyBlock.SetActive(false);
            }
            
            if(YG2.envir.isMobile)return;
            
            if (Input.GetMouseButtonDown(1))
            {
                if (TryOpenBlock()) return;
            }

            if (Input.GetMouseButtonDown(1))
            {
                TryEatItem();
            }

            if (Input.GetMouseButton(1))
            {
                TryPlaceBlock();
            }
            else if (Input.GetMouseButtonUp(1))
            {
                _nextPlaceTime = 0f;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryAttact();
            }

            if (Input.GetMouseButtonDown(0)) // ЛКМ для начала разрушения
            {
                _isBreaking = true;
                handAnimationController.StartBreakingAnimation();
                _isAnimating = true;
            }

            if (Input.GetMouseButtonUp(0)) // Отпускание ЛКМ
            {
                _isBreaking = false;
                handAnimationController.StopBreakingAnimation();
                _isAnimating = false;
            }

            if (Input.GetMouseButton(0))
            {
                if (_isBreaking)
                {
                    if (_isAnimating == false)
                        handAnimationController.StartBreakingAnimation();
                    TryDestroyBlock(); // Продолжать ломать блок
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _currentDamage = 0f;
                _currentBlock = null;
                if (_isSurvival) _destroyBlock.SetActive(false);
            }
        }


        public void TryEatItem()
        {
            var selectedItem = _playerInventory.GetSelectedItem();
            if (selectedItem != null && selectedItem.Item != null && selectedItem.Item.Type == ItemType.Consumable)
            {
                HungerSystem.EatFood(Mathf.FloorToInt(selectedItem.Item.HungerRestoration));

                _playerInventory.RemoveSelectedItem();
            }
        }

        public bool TryAttact()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f, mobLayer)) return false;
            GameObject hitObject = hitInfo.collider.gameObject;

            // Здесь можно добавить логику атаки, если объект является мобом или врагом
            // Например:
            var Mob = hitObject.GetComponent<Mob>();
            if (Mob != null)
            {
                StartCoroutine(Mob.TakeDamage(1, -hitInfo.normal));
            }

            return true;
        }

        public void TryPlaceBlock()
        {
            if (Time.time < _nextPlaceTime) return;

            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f, worldLayer)) return;

            var blockPosition = hitInfo.point + hitInfo.normal * 0.5f;
            if (Vector3Int.FloorToInt(transform.position) != Vector3Int.FloorToInt(blockPosition) &&
                Vector3Int.FloorToInt(transform.position + Vector3.down) != Vector3Int.FloorToInt(blockPosition))
            {
                var selectedItem = _playerInventory.GetSelectedItem();
                if (selectedItem != null)
                    if (selectedItem.Item != null)
                        if (selectedItem.Item.IsPlaceable)
                        {
                            World.Instance.SpawnBlock(blockPosition, selectedItem.Item.BlockType.Id);
                            if (_isSurvival) _playerInventory.RemoveSelectedItem();

                            handAnimationController.PlayPlaceAnimation();
                        }

                _nextPlaceTime = Time.time + placeDelay;
            }
        }

        public bool TryOpenBlock()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f, worldLayer)) return false;

            var blockPosition = hitInfo.point - hitInfo.normal * 0.5f;

            var block = World.Instance.GetBlockAtPosition(blockPosition);
            if (block.HaveInventory)
            {
                if (UiManager.Instance.OpenCloseChest(World.Instance.GetInventory(blockPosition),
                        Vector3Int.FloorToInt(blockPosition)))
                    GetComponentInParent<InteractController>().DisableScripts();
                return true;
            }

            return false;
        }

        public void TryDestroyBlock()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f, worldLayer)) return;
            if (_isSurvival) _destroyBlock.SetActive(true);
            var blockPosition = Vector3Int.FloorToInt(hitInfo.point - hitInfo.normal * 0.5f);

            if (_currentBlock == null || _targetBlockPosition != blockPosition)
            {
                _currentDamage = 0f;
                _targetBlockPosition = blockPosition;
                _currentBlock = World.Instance.GetBlockAtPosition(blockPosition);
                if (_currentBlock == null)
                    return;
            }

            if (!_isSurvival)
            {
                // Задержка разрушения в режиме без выживания
                if (Time.time < _nextDestroyTime) return;
                _nextDestroyTime = Time.time + destroyDelayNonSurvival;

                World.Instance.DestroyBlock(blockPosition);
                _playerInventory.RemoveDurability();
                _currentBlock = null;
                if (_isSurvival)
                    if (_isSurvival)
                        _destroyBlock.SetActive(false);
                handAnimationController.StopBreakingAnimation();
                _isAnimating = false;
                return;
            }

            if (_currentBlock.Durability > 0) _currentDamage += breakSpeed * Time.deltaTime * miningMultiplier;
            _material.SetFloat(DamageAmount, _currentDamage / _currentBlock.Durability);

            if (_currentDamage >= _currentBlock.Durability && _currentBlock.Durability > 0)
            {
                World.Instance.DestroyBlock(blockPosition);
                _playerInventory.RemoveDurability();
                _currentDamage = 0f;
                _currentBlock = null;
                if (_isSurvival) _destroyBlock.SetActive(false);
                handAnimationController.StopBreakingAnimation();
                _isAnimating = false;
            }
        }
    }
}