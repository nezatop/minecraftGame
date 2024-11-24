using MultiCraft.Scripts.Core.Inventories;
using UnityEngine;

namespace MultiCraft.Scripts.Core.Players
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float moveSpeed = 5.0f;
        public float jumpHeight = 2.0f;
        public float gravity = -18f;

        private CharacterController _controller;
        private Vector3 _velocity;
        public bool _isGrounded;
        
        private int _currentValue = 0;
        private Inventory _inventory;
        private const int MinValue = 0;
        private const int MaxValue = 8; 

        
        private void Start()
        {
            _inventory = GetComponent<Inventory>();
            _controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll < 0f)
            {
                if (_currentValue < MaxValue)
                    _currentValue++;
            }
            else if (scroll > 0f)
            {
                if (_currentValue > MinValue)
                    _currentValue--;
            }

            _inventory.UpdateHotBarSelectedSlot(_currentValue);
            
            _isGrounded = _controller.isGrounded;

            if (_isGrounded && _velocity.y < 0)
                _velocity.y = -2f;

            var horizontalInput = Input.GetAxis("Horizontal");
            var verticalInput = Input.GetAxis("Vertical");

            var moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
            _controller.Move(moveDirection * (moveSpeed * Time.deltaTime));

            if (Input.GetButtonDown("Jump") && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }
    }
}