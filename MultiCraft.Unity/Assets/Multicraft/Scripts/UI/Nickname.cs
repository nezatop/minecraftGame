using System;
using MultiCraft.Scripts.Utils;
using TMPro;
using UnityEngine;

namespace MultiCraft.Scripts.UI
{
    public class Nickname : MonoBehaviour
    {
        public TMP_Text nickname;
        public LoadUser loadUser;

        private void OnEnable()
        {
            loadUser.Login += OnLogin;
            loadUser.Initialize();
        }

        private void OnDisable()
        {
            loadUser.Login -= OnLogin;
        }

        private void OnLogin()
        {
            nickname.text = PlayerPrefs.GetString("PlayerName");
        }
    }
}