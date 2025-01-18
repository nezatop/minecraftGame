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

        public Action OnDeath;
        public Action<int> OnDamage;

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
            StartCoroutine(ColorRes());
            health = Mathf.Clamp(health - damage, 0, maxHealth);
            if (health <= 0) OnDeath?.Invoke();
            OnDamage?.Invoke((int)health);
        }

        private IEnumerator ColorRes()
        {
            
            var material = meshRenderer.material;
            material.color = Color.red;
            yield return new WaitForSeconds(0.1f);
            material.color = Color.white;
        }

        private void TakeDamage()
        {
            var damage = 0.001f * Time.deltaTime;
            health = Mathf.Clamp((health - damage), 1f, maxHealth);
            if (health <= 0) OnDeath?.Invoke();
            OnDamage?.Invoke((int)health);
        }

        public void Heal(int heal)
        {
            health = Mathf.Clamp(health + heal, 0, maxHealth);
        }
    }
}