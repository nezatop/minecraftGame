using System;
using MultiCraft.Scripts.Engine.Core.Entities;
using MultiCraft.Scripts.Engine.Core.HealthSystem;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
using MultiCraft.Scripts.Engine.Core.Items;
using MultiCraft.Scripts.Engine.Core.MeshBuilders;
using MultiCraft.Scripts.Engine.Utils;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Engine.Core.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.0f;
        public float jumpHeight = 1.24f;
        public float gravity = -18f;

        public float fallThreshold = 3.0f;
        public float fallDamageMultiplier = 0.5f;

        public AudioSource footstepAudioSource;
        public AudioSource jumpAudioSource;
        public AudioClip[] footstepSounds;
        public AudioClip jumpSound;
        public float stepInterval = 0.5f;

        public CharacterController controller;
        private Vector3 _velocity;
        private bool _isGrounded;

        public HandRenderer handRenderer;
        public DroppedItem droppedItemPrefab;
        public float dropForce = 5f;

        public Inventory inventory;
        private float _fallStartY;
        private bool _isFalling;
        public Health health;
        public HungerSystem hunger;

        private float _stepTimer;

        public Animator animator;

        private int _currentValue = 0;
        private bool _crouching;
        private const int MinValue = 0;
        private const int MaxValue = 8;
        
        public VariableJoystick variableJoystick;
        public bool isMobile = false;

        public CameraController cameraController;
        
        private DestroyAndPlaceBlockController _destroyAndPlaceBlockController;
        private Network.Player.DestroyAndPlaceBlockController _destroyAndPlaceBlockControllerNetwork;
        
        private InteractController _interactController;
        private Network.Player.InteractController _interactControllerNetwork;
        
        public float horizontalInput;
        public float verticalInput;
        private void Start()
        {
            _destroyAndPlaceBlockController = cameraController.gameObject.GetComponent<DestroyAndPlaceBlockController>();
            _destroyAndPlaceBlockControllerNetwork = cameraController.gameObject.GetComponent<Network.Player.DestroyAndPlaceBlockController>();
           
            _interactController = gameObject.GetComponent<InteractController>();
            _interactControllerNetwork = gameObject.GetComponent<Network.Player.InteractController>();
            
            inventory = GetComponent<Inventory>();
            health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("Health component not found on the player!");
            }

            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            HandleHeal();
            HandleMovement();
            HandleItemInteraction();
            HandleAnimation();
        }

        public void RightClickHandle()
        {
            if (_destroyAndPlaceBlockController != null)
            {
                _destroyAndPlaceBlockController.TryEatItem();
                if(_destroyAndPlaceBlockController.TryOpenBlock())return;
                _destroyAndPlaceBlockController.TryPlaceBlock();
            }
            else
            {
                _destroyAndPlaceBlockControllerNetwork.TryEatItem();
                if(_destroyAndPlaceBlockControllerNetwork.TryOpenBlock())return;
                _destroyAndPlaceBlockControllerNetwork.TryPlaceBlock();
            }
        }

        public void StopBreaking()
        {
            if (_destroyAndPlaceBlockController != null)
            {
                _destroyAndPlaceBlockController.StopBreaking();
            }
            else
            {
                _destroyAndPlaceBlockControllerNetwork.StopBreaking();
            }
        }
        
        public void LeftClickHandle()
        {
            if (_destroyAndPlaceBlockController != null)
            {
                if(_destroyAndPlaceBlockController.TryAttact())return;
                _destroyAndPlaceBlockController.TryDestroyBlock();
            }
            else
            {
                if(_destroyAndPlaceBlockControllerNetwork.TryAttact())return;
                _destroyAndPlaceBlockControllerNetwork.TryDestroyBlock();
            }
        }

        public void Enable()
        {
            if (_interactController != null)
            {
                _interactController.EnableScripts();
            }
            else
            {
                _interactControllerNetwork.EnableScripts();
            }
        }

        public void Disable()
        {
            if (_interactController != null)
            {
                _interactController.DisableScripts();
            }
            else
            {
                _interactControllerNetwork.DisableScripts();
            }
        }
        
        private void HandleHeal()
        {
            if (hunger.hunger > 0.5 * hunger.maxHunger)
            {
                health.Heal(1);
            }
        }

        private void HandleMovement()
        {
            CheckGrounded();
            Vector3 moveDirection = GetInput();
            MoveCharacter(moveDirection);
            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                HandleJump();
            }

            UpdateVerticalVelocity();
        }

        private void CheckGrounded()
        {
            _isGrounded = controller.isGrounded;
            animator.SetBool("Grounded", _isGrounded);

            if (_isGrounded)
            {
                if (_velocity.y < 0)
                    _velocity.y = -2f;

                // Проверяем, был ли игрок в состоянии падения
                if (_isFalling)
                {
                    float fallDistance = _fallStartY - transform.position.y;
                    if (fallDistance > fallThreshold)
                    {
                        ApplyFallDamage(fallDistance);
                    }
                    _isFalling = false; // Завершаем состояние падения
                }
            }
            else
            {
                if (!_isFalling) // Если игрок только начал падать
                {
                    _isFalling = true;
                    _fallStartY = transform.position.y; // Сохраняем начальную высоту падения
                }
            }
        }


        private Vector3 GetInput()
        {
            if (isMobile)
            {
                horizontalInput = variableJoystick.Horizontal;
                verticalInput = variableJoystick.Vertical;
            }
            else
            {
                horizontalInput = Input.GetAxis("Horizontal");
                verticalInput = Input.GetAxis("Vertical");
            }

            return transform.forward * verticalInput + transform.right * horizontalInput;
        }

        private void MoveCharacter(Vector3 moveDirection)
        {
            controller.Move(moveDirection * (moveSpeed * Time.deltaTime));

            if (_isGrounded && moveDirection.magnitude > 0)
            {
                _stepTimer += Time.deltaTime;
                if (_stepTimer >= stepInterval)
                {
                    PlayFootstepSound();
                    _stepTimer = 0f;
                }
            }
            else
            {
                _stepTimer = 0f;
            }
        }

        public void HandleJump()
        {
            
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                PlayJumpSound();
            
        }

        private void UpdateVerticalVelocity()
        {
            _velocity.y += gravity * Time.deltaTime;
            controller.Move(_velocity * Time.deltaTime);
        }


        public void Teleport(Vector3 position)
        {
            _velocity = Vector3
                .zero; // Отключаем текущую скорость, чтобы избежать странного поведения после телепортации
            controller.enabled = false; // Отключаем CharacterController, чтобы избежать конфликтов
            transform.position = position;
            controller.enabled = true; // Включаем обратно
        }


        private void HandleItemInteraction()
        {
            Item handItem;
            if (!isMobile)
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");

                if (scroll < 0f && _currentValue < MaxValue)
                    _currentValue++;
                else if (scroll > 0f && _currentValue > MinValue)
                    _currentValue--;
                
                if (Input.GetKeyDown(KeyCode.Alpha1))
                    _currentValue = 0;
                if (Input.GetKeyDown(KeyCode.Alpha2))
                    _currentValue = 1;
                if (Input.GetKeyDown(KeyCode.Alpha3))
                    _currentValue = 2;
                if (Input.GetKeyDown(KeyCode.Alpha4))
                    _currentValue = 3;
                if (Input.GetKeyDown(KeyCode.Alpha5))
                    _currentValue = 4;
                if (Input.GetKeyDown(KeyCode.Alpha6))
                    _currentValue = 5;
                if (Input.GetKeyDown(KeyCode.Alpha7))
                    _currentValue = 6;
                if (Input.GetKeyDown(KeyCode.Alpha8))
                    _currentValue = 7;
                if (Input.GetKeyDown(KeyCode.Alpha9))
                    _currentValue = 8;
                
                handItem = inventory.UpdateHotBarSelectedSlot(_currentValue);
            }
            else
            {
                var itemInSlot = inventory.GetSelectedItem();
                if(itemInSlot == null) return;
                handItem = itemInSlot.Item;
            }

            

            if (handItem != null)
            {
                if (handItem.BlockType != null)
                {
                    var block = ResourceLoader.Instance.GetBlock(handItem.BlockType.Id);
                    if (block != null)
                    {
                        var blockMesh = DropItemMeshBuilder.GeneratedMesh(block);
                        handRenderer.SetMesh(blockMesh);
                    }
                    else
                    {
                        var itemMesh = DropItemMeshBuilder.GeneratedMesh(handItem);
                        handRenderer.RemoveMesh();
                    }
                }
            }
            else
            {
                handRenderer.RemoveMesh();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                var item = inventory.RemoveSelectedItem().Item;
                var camera = transform.GetChild(0);
                var droppedItem = Instantiate(droppedItemPrefab, camera.position + Vector3.down * 0.2f,
                    Quaternion.identity);
                Rigidbody rb = droppedItem.GetComponent<Rigidbody>();
                Vector3 force = camera.transform.forward.normalized * dropForce;
                rb.AddForce(force, ForceMode.Impulse);
                droppedItem.Item = new ItemInSlot(item, 1);
                droppedItem.Init();
            }
        }

        private void HandleAnimation()
        {
            float horizontalInput;
            float verticalInput;
            
            if (isMobile)
            {
                horizontalInput = variableJoystick.Horizontal;
                verticalInput = variableJoystick.Vertical;
            }
            else
            {
                horizontalInput = Input.GetAxis("Horizontal");
                verticalInput = Input.GetAxis("Vertical");
            }
            
            bool isMoving = horizontalInput != 0 || verticalInput != 0;
            animator.SetFloat("VelocityX", horizontalInput*4);
            animator.SetFloat("VelocityZ", verticalInput*4);

            if (Input.GetKey(KeyCode.LeftControl))
            {
                _crouching = true;
                moveSpeed = 2.5f;
            }
            else
            {
                _crouching = false;
                moveSpeed = 5.0f;
            }

            var center = controller.center;
            center.y = _crouching ? 0.6f : 0.21f;
            controller.center = center;
            animator.SetFloat("Upright", !_crouching ? 1.0f : 0.0f);
        }

        private void ApplyFallDamage(float fallDistance)
        {
            if (health != null)
            {
                float damage = (fallDistance - fallThreshold) * fallDamageMultiplier;
                health.TakeDamage(Mathf.RoundToInt(damage));
            }
        }

        private void PlayFootstepSound()
        {
            if (footstepSounds.Length > 0 && footstepAudioSource != null)
            {
                int randomIndex = UnityEngine.Random.Range(0, footstepSounds.Length);
                footstepAudioSource.PlayOneShot(footstepSounds[randomIndex]);
            }
        }

        private void PlayJumpSound()
        {
            if (jumpAudioSource != null && jumpSound != null)
            {
                jumpAudioSource.PlayOneShot(jumpSound);
            }
        }
    }
}