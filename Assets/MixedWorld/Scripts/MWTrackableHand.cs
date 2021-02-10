using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Util
{

public class MWTrackableHand : MonoBehaviour
{
        public List<Transform> Joints;
        public List<MWHandBone> MWBones;
        public float smoothSpeed = 10f;

        public void UpdateJoints(Vector3[] jointPos)
        {
            Transform p = transform.parent;

            transform.SetParent(null);
            Quaternion q = transform.rotation;
            Vector3 pos = transform.position;
            transform.rotation = Quaternion.identity;
            transform.position = Vector3.zero;
            // sphere joints
            float smoothDelta = Time.deltaTime * smoothSpeed;
            for (int i = 0; i < jointPos.Length; i++)
            {
                Joints[i].localPosition = Vector3.Lerp(Joints[i].localPosition, jointPos[i], smoothDelta);
            }
            // Connection Bones
            for (int i = 0; i < MWBones.Count; i++)
            {
                MWBones[i].UpdateBone(true);
            }
            // reparent and reset handposition and rotation
            transform.rotation = q;
            transform.position = pos;
            transform.SetParent(p, true);
        }
    }
}

