using System;
using System.Collections;
using UnityEngine;

namespace Multicraft.Scripts.Engine.Core.Hunger
{
    public class HungerSystem : MonoBehaviour
    {
        public float maxHunger = 100; // Максимальный уровень голода
        public float hunger = 100; // Текущий уровень голода
        public float hungerDecreaseRate = 1f; // Скорость уменьшения голода (ед/сек)
        public float hungerDecreaseTime = 1f; // интервал уменьшения голода сек

        public Action<int> onHungerChanged;
        public Action onHungerZero;
        
        private void Update()
        {
            StartCoroutine(HungerDecrease());
        }

        public IEnumerator HungerDecrease()
        {
            // Уменьшение голода со временем
            hunger -= hungerDecreaseRate * Time.deltaTime;
            hunger = Mathf.Clamp(hunger, 0, maxHunger);

            // Проверка состояния голода
            if (hunger <= 0)
            {
                onHungerZero?.Invoke();
            }
            
            onHungerChanged?.Invoke((int)hunger);
            yield return new WaitForSeconds(hungerDecreaseTime);
        }

        public void EatFood(int foodValue)
        {
            // Восстановление голода
            hunger += foodValue;
            hunger = Mathf.Clamp(hunger, 0, maxHunger);
            onHungerChanged?.Invoke((int)hunger);
            Debug.Log($"Ate food, hunger restored by {foodValue}. Current hunger: {hunger}");
        }
    }
}