using UnityEngine;

namespace MultiCraft.Scripts.Engine.Core.Player
{
    public class CameraController : MonoBehaviour
    {
        public float sensitivity = 2.0f;
        public float maxYAngle = 80.0f;

        public Transform head; // Ссылка на голову (объект с камерой)
        public Transform body; // Ссылка на тело игрока

        private float _rotationY;
        
        public VariableJoystick variableJoystick;
        public bool isMobile = false;

        private void Update()
        {
            float mouseX,mouseY;
            if (isMobile)
            {
                mouseX = variableJoystick.Horizontal;
                mouseY = variableJoystick.Vertical;
            }
            else
            {
                mouseX = Input.GetAxis("Mouse X");
                mouseY = Input.GetAxis("Mouse Y");
            }
            
            body.Rotate(Vector3.up * (mouseX * sensitivity));

            _rotationY -= mouseY * sensitivity;
            _rotationY = Mathf.Clamp(_rotationY, -maxYAngle, maxYAngle);

            head.localRotation = Quaternion.Euler(_rotationY, 0.0f, 0.0f);
        }
    }
}