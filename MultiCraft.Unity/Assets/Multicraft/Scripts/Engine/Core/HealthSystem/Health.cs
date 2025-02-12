using System;
using System.Collections;
using Multicraft.Scripts.Engine.Core.Hunger;
using UnityEngine;
using UnityEngine.Serialization;

namespace MultiCraft.Scripts.Engine.Core.HealthSystem
{
    public class Health : MonoBehaviour
    {
        public float health = 10;

        public float maxHealth = 10;

        public Action<GameObject> OnDeath;
        public Action<int> OnDamage;

        public bool haveHunger = true;
        public SkinnedMeshRenderer meshRenderer;

        private void Awake()
        {
            if (haveHunger && health > 0) gameObject.GetComponent<HungerSystem>().onHungerZero += TakeHungerDamage;
        }

        private void OnDisable()
        {
            if (haveHunger && health > 0) gameObject.GetComponent<HungerSystem>().onHungerZero -= TakeHungerDamage;
        }
        public void TakeDamage(int damage)
        {
            StartCoroutine(ColorRes());
            health = Mathf.Clamp(health - damage, 0, maxHealth);
            if (health <= 0) OnDeath?.Invoke(gameObject);
            OnDamage?.Invoke((int)health);
        }

        public void ResetHealth()
        {
            health = maxHealth;
        }

        private IEnumerator ColorRes()
        {
            
            var material = meshRenderer.material;
            material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            material.color = Color.white;
        }

        private void TakeHungerDamage()
        {
            const int damage = 1;
            health = Mathf.Clamp((health - damage), 1f, maxHealth);
            if (health <= 0) OnDeath?.Invoke(gameObject);
            OnDamage?.Invoke((int)health);
        }

        public void Heal(int heal)
        {
            health = Mathf.Clamp(health + heal, 0, maxHealth);
        }
    }
}