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
        public bool isLeft = false;
        public bool isTracked = true;
        public bool isDirty = false;

        private float lastTracked = 0f;
        private bool hidden = false;
        private bool locked = false;

        private Vector3[] m_joints = new Vector3[21];
        private Vector3[] m_clonedJoints = new Vector3[21];

        public Vector3[] SharedJoints
        {
            get
            {
                locked = true;
                m_clonedJoints = (Vector3[])m_joints.Clone();
                locked = false;
                return m_clonedJoints;
            }
        }

        private Vector3 lastKnownPos = Vector3.zero;

        private void Start()
        {
            if (isLeft)
            {
                lastKnownPos.x = -1f;
            }
            else
            {
                lastKnownPos.x = +1f;
            }
        }
        private void Update()
        {
            if (isTracked)
            {
                lastTracked = 0f;
                HideHand(false);
                isTracked = false;
            }
            else
            {
                if (lastTracked > 1f)
                {
                    HideHand(true);
                    lastTracked = 1.0f;
                }
                else if (!hidden)
                {
                    lastTracked += Time.deltaTime;
                }
            }

        }

        /// <summary>
        /// Hides or Shows a hand depending on the value of hide.
        /// </summary>
        /// <param name="hide"></param>
        public void HideHand(bool hide)
        {
            if (hide == hidden) return;
            hidden = hide;
            foreach(Transform j in Joints)
            {
                j.gameObject.SetActive(!hide);
            }
        }
        public void UpdateJoints(Vector3[] jointPos, bool fromRemote = false)
        {
            
            lastKnownPos = jointPos[0];
            isTracked = true;

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
                if (!locked)
                {
                    m_joints[i] = jointPos[i];
                }
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
            this.isDirty = !fromRemote;
        }

        public float DistToNewPos(Vector3 newPos)
        {
            return Vector3.Distance(newPos, lastKnownPos);
        }
    }
}

