using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Utility
{
    public class ThingsChanger : MonoBehaviour
    {
        public List<GameObject> things = new List<GameObject>();
        public int currentIndex = 0;

        void Start()
        {
            currentIndex = Random.Range(0, things.Count);
            things[currentIndex].SetActive(true);
        }

        public void SwitchThing(int idx = -1)
        {
            int newIdx = idx;
            if (idx == -1 && things.Count > 0)
            {
                newIdx = Random.Range(0, things.Count);
            }
            if (newIdx >= 0 && newIdx < things.Count)
            {
                foreach (var hat in things)
                {
                    hat.SetActive(false);
                }
                things[idx].SetActive(true);
                currentIndex = idx;
            }
        }
    }
}

