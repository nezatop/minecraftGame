﻿using MultiCraft.Scripts.Engine.Core.HealthSystem;
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
           nickNameTable.transform.LookAt(cameraTransform);
           nickNameTable.transform.Rotate(0f, 180f, 0f);
        }
    }
}