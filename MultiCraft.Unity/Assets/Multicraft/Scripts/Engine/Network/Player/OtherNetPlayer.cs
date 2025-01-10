using MultiCraft.Scripts.Engine.Core.HealthSystem;
using TMPro;
using UnityEngine;

namespace MultiCraft.Scripts.Engine.Network.Player
{
    public class OtherNetPlayer : MonoBehaviour
    {
        public string playerName;
        public TMP_Text nickNameTable;

        public Health health;
        
        public Animator animator;
        
        public Transform cameraTransform;

        public void Init()
        {
            nickNameTable.text = playerName;
        }

        private void Update()
        {
            if(!cameraTransform)return;
            var direction = cameraTransform.position - nickNameTable.transform.position;
            direction.y = 0f;
            nickNameTable.transform.rotation = Quaternion.LookRotation(direction);
            nickNameTable.transform.Rotate(0f, 180f, 0f);

        }
    }
}