﻿using System;
using Multicraft.Scripts.Engine.Core.Hunger;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Engine.Core.HealthSystem
{
    public class Health : MonoBehaviour
    {
        public float health = 10;

        public float maxHealth = 10;

        public event Action OnDeath;
        public event Action<int> OnDamage;

        public bool haveHunger = true;
        public SkinnedMeshRenderer meshRenderer;

        private void Awake()
        {
            if (haveHunger && health > 0) gameObject.GetComponent<HungerSystem>().onHungerZero += TakeDamage;
        }

        private void OnDisable()
        {
            if (haveHunger && health > 0) gameObject.GetComponent<HungerSystem>().onHungerZero -= TakeDamage;
        }

        public void TakeDamage(int damage)
        {
            var material = meshRenderer.material;
            material.color = Color.red;
            health = Mathf.Clamp(health - damage, 0, maxHealth);
            if (health <= 0) OnDeath?.Invoke();
            OnDamage?.Invoke((int)health);
            material.color = Color.white;
        }

        private void TakeDamage()
        {
            var damage = 0.001f * Time.deltaTime;
            health = Mathf.Clamp((health - damage), 1f, maxHealth);
            if (health <= 0) OnDeath?.Invoke();
            OnDamage?.Invoke((int)health);
        }
    }
}