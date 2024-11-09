using System;
using MultiCraft.Scripts.Game.Inventory;
using MultiCraft.Scripts.Game.Items;
using UnityEngine;

namespace MultiCraft.Scripts.Game.Entities
{
    public class ItemEntity : MonoBehaviour
    {
        public Item item;
        public GameObject ItemModel;

        private void Start()
        {
            ItemsDataBase.Initialize();
            item = ItemsDataBase.GetItem("Dirt");
        }
        
        private void Update()
        {
            if (ItemModel != null)
            {
                ItemModel.transform.Rotate(Vector3.up * 50 * Time.deltaTime); // 50 - скорость вращения
            }
        }


        public void Spawn(Vector3 position)
        {
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))return;
            var inventory = other.gameObject.GetComponent<Inventory.Inventory>();
            var pick= inventory.AddItem(item, 1);
            if (pick) PickUp();
        }

        public void PickUp()
        {
            Destroy(ItemModel);
            Destroy(gameObject);
        }
    }
}