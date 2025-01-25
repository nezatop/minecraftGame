using System;
using UnityEngine;
using YG;

namespace MultiCraft.Scripts.Utils
{
    public class LoadUser : MonoBehaviour
    {
        public Action Login;

        public void Initialize()
        {
            var playerId = YG2.player.id;
            var playerName = YG2.player.name;

            PlayerPrefs.SetString("PlayerID", playerId);
            PlayerPrefs.SetString("PlayerName", playerName);
            PlayerPrefs.Save();
            
            Login?.Invoke();
        }
    }
}