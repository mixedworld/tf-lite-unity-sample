using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Sharing
{

    public class SharedPrefabBase : MonoBehaviour
    {
        public bool isSender = false;

        public List<GameObject> hideList = new List<GameObject>();
        public void UpdateCompenents()
        {
            foreach (var go in hideList)
            {
                go.SetActive(!isSender);
            }
        }

        private void Start()
        {
            if (isSender)
            {
                StartCoroutine(ClientFactory.Instance.RegisterPrefab(GetComponent<ObjectIdentifier>()));
            }
            UpdateCompenents();
        }
    }
}
