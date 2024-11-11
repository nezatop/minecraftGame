using System;
using MultiCraft.Scripts.Game.Blocks;
using MultiCraft.Scripts.Game.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MultiCraft.Scripts.Game.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.0f; // Скорость движения персонажа
        public float jumpHeight = 2.0f; // Высота прыжка
        public float gravity = -9.81f; // Сила гравитации

        public float maxDistance = 5f;      // Максимальная дальность взаимодействия
        public float breakSpeed = 10f;       // Скорость разрушения (урон в секунду)
        private float currentDamage = 0f;   // Текущий накопленный урон
        private Block currentBlock;         // Текущий целевой блок для разрушения
        private Vector3 targetBlockPosition;
        
        private CharacterController controller;
        private Vector3 velocity;
        private bool isGrounded;
        
        private GameWorld _gameWorld;
        [SerializeField]private Camera Camera;

        [Obsolete("Obsolete")]
        private void Start()
        {
            controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            _gameWorld = FindObjectOfType<GameWorld>();
        }

        private void Update()
        {
            if (Input.GetMouseButton(0))
            {
                Debug.Log(currentDamage);
                TryDestroyBlock();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                // Сброс урона, если игрок отпустил кнопку
                currentDamage = 0f;
                currentBlock = null;
            }
            
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    Vector3 blockPosition = hitInfo.point + hitInfo.normal * 0.5f;
                        if(Vector3Int.FloorToInt(transform.position) != Vector3Int.FloorToInt(blockPosition))
                            _gameWorld.SpawnBlock(blockPosition, BlockType.Stone); // Укажите тип блока
                }
            }

            // Проверяем, находится ли персонаж на земле
            isGrounded = controller.isGrounded;

            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f; // Небольшое отрицательное значение для стабильного приземления
            }

            // Получаем ввод от игрока для движения
            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // Вычисляем направление движения
            Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

            // Двигаем персонажа
            controller.Move(moveDirection * (moveSpeed * Time.deltaTime));

            // Проверяем, нажата ли кнопка прыжка и находится ли персонаж на земле
            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            // Применяем гравитацию к вертикальной скорости
            velocity.y += gravity * Time.deltaTime;

            // Двигаем персонажа по вертикали
            controller.Move(velocity * Time.deltaTime);
        }
        
        void TryDestroyBlock()
        {
            Ray ray = Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));

            // Проверяем попадание луча
            if (Physics.Raycast(ray, out var hitInfo, maxDistance))
            {
                Vector3 blockPosition = hitInfo.point - hitInfo.normal * 0.5f;

                // Проверяем, не изменился ли целевой блок
                if (currentBlock == null || targetBlockPosition != blockPosition)
                {
                    // Сбрасываем урон, если начали разрушать новый блок
                    currentDamage = 0f;
                    targetBlockPosition = blockPosition;
                    currentBlock = _gameWorld.GetBlockAtPosition(blockPosition); // Получаем текущий блок
                }

                // Увеличиваем накопленный урон
                currentDamage += breakSpeed * Time.deltaTime;

                // Разрушаем блок, если урон превышает прочность
                if (currentDamage >= currentBlock.Durability)
                {
                    _gameWorld.DestroyBlock(blockPosition);
                    currentDamage = 0f;
                    currentBlock = null;
                }
            }
        }
    }
}