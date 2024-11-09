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
            if (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0))
            {
                bool isDestroying = Input.GetMouseButtonDown(0);
                Ray ray = Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
                if (Physics.Raycast(ray, out var hitInfo))
                {
                    Vector3 blockPosition = isDestroying 
                        ? hitInfo.point - hitInfo.normal * 0.5f 
                        : hitInfo.point + hitInfo.normal * 0.5f;

                    if (isDestroying)
                    {
                        _gameWorld.DestroyBlock(blockPosition);
                    }
                    else
                    {
                        _gameWorld.SpawnBlock(blockPosition, BlockType.Stone); // Укажите тип блока
                    }
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
    }
}