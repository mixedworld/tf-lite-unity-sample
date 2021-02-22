using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Sharing
{
    public class MWLaserpointer : MonoBehaviour
    {
        public Transform StartPoint;
        public Transform EndPoint = null;
        public string EndPointSpawnerName = "cursor";

        public string EndPointName = "";
        private Vector3 currentLaserScale = Vector3.zero;
        void Start()
        {
            currentLaserScale = StartPoint.localScale;
        }

        void Update()
        {
            // For now this wont work..
            return;
            if (EndPoint == null)
            {
                if (EndPointName == "")
                {
                    //EndPointName = spar.avatarId.Replace(spar.OwnSpawnerType, EndPointSpawnerName).Replace("/", null);
                    //Debug.Log("New Endpoint name to find is: " + EndPointName);
                }
                GameObject go = GameObject.Find(EndPointName);
                if (go!= null)
                {
                    EndPoint = go.transform;
                }
                else
                {
                    currentLaserScale.z = 0.01f;
                }
            }
            else
            {
                currentLaserScale.z = Mathf.Abs(Vector3.Distance(StartPoint.position, EndPoint.position));
            }
            if (currentLaserScale != StartPoint.localScale)
            {
                StartPoint.localScale = currentLaserScale;
            }
        }
    }
}
