using UnityEngine;

namespace MultiCraft.Scripts.Core.Players
{
    public class HighLightController : MonoBehaviour
    {
        public GameObject highlightPrefab;
        private GameObject _highlight;

        private void Start()
        {
            if (_highlight == null)
                _highlight = Instantiate(highlightPrefab, Vector3.zero, Quaternion.Euler(0f, 0f, 0f));
        }

        private void OnDisable()
        {
            if(_highlight != null)
                _highlight.SetActive(false);
        }

        private void Update()
        {
            if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, 5f))
            {
                var inCube = hitInfo.point - (hitInfo.normal * 0.5f);
                var removeBlock = Vector3Int.FloorToInt(inCube);
                
                _highlight.transform.position = removeBlock + new Vector3(.5f, .5f, .5f);
                _highlight.SetActive(true);
            }
            else
            {
                _highlight.SetActive(false);
            }
        }
    }
}