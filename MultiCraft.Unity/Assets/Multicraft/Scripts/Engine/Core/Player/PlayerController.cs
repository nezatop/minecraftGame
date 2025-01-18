using System;
using MultiCraft.Scripts.Engine.Core.Entities;
using MultiCraft.Scripts.Engine.Core.HealthSystem;
using Multicraft.Scripts.Engine.Core.Hunger;
using MultiCraft.Scripts.Engine.Core.Inventories;
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

        private Inventory _inventory;
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
        
        public float horizontalInput;
        public float verticalInput;
        private void Start()
        {
            
            _inventory = GetComponent<Inventory>();
            health = GetComponent<Health>();
            if (health == null)
            {
                Debug.LogError("Health component not found on the player!");
            }

            controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            if(!isMobile)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = true;
            }
        }

        private void Update()
        {
            HandleHeal();
            HandleMovement();
            HandleItemInteraction();
            HandleAnimation();
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
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll < 0f && _currentValue < MaxValue)
                _currentValue++;
            else if (scroll > 0f && _currentValue > MinValue)
                _currentValue--;

            var handItem = _inventory.UpdateHotBarSelectedSlot(_currentValue);
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
                var item = _inventory.RemoveSelectedItem().Item;
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