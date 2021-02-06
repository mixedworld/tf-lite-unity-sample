/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal.Beta
{
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary> A nr grabber. </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class NRGrabber : MonoBehaviour
    {
        /// <summary> The grab button. </summary>
        public ControllerButton grabButton = ControllerButton.GRIP;
        /// <summary> The hand enum. </summary>
        public ControllerHandEnum handEnum;
        /// <summary> True to enable, false to disable the grab multi. </summary>
        public bool grabMultiEnabled = false;
        /// <summary> True to update pose by rigidbody. </summary>
        public bool updatePoseByRigidbody = true;

        /// <summary> True to previous grab press. </summary>
        private bool m_PreviousGrabPress;
        /// <summary> Dictionary of grab readies. </summary>
        private Dictionary<NRGrabbableObject, int> m_GrabReadyDict = new Dictionary<NRGrabbableObject, int>();
        /// <summary> List of grabbings. </summary>
        private List<NRGrabbableObject> m_GrabbingList = new List<NRGrabbableObject>();
        /// <summary> The children colliders. </summary>
        private Collider[] m_ChildrenColliders;

        /// <summary> Awakes this object. </summary>
        void Awake()
        {
            Rigidbody rigid = GetComponent<Rigidbody>();
            rigid.useGravity = false;
            rigid.isKinematic = true;
            m_ChildrenColliders = GetComponentsInChildren<Collider>();
        }

        /// <summary> Executes the 'enable' action. </summary>
        private void OnEnable()
        {
            NRInput.OnControllerStatesUpdated += OnControllerPoseUpdated;
        }

        /// <summary> Executes the 'disable' action. </summary>
        private void OnDisable()
        {
            NRInput.OnControllerStatesUpdated -= OnControllerPoseUpdated;
        }

        /// <summary> Fixed update. </summary>
        private void FixedUpdate()
        {
            if (!updatePoseByRigidbody)
                return;
            UpdateGrabbles();
        }

        /// <summary> Executes the 'trigger enter' action. </summary>
        /// <param name="other"> The other.</param>
        private void OnTriggerEnter(Collider other)
        {
            NRGrabbableObject grabble = other.GetComponent<NRGrabbableObject>() ?? other.GetComponentInParent<NRGrabbableObject>();
            if (grabble == null)
                return;
            if (m_GrabReadyDict.ContainsKey(grabble))
                m_GrabReadyDict[grabble] += 1;
            else
                m_GrabReadyDict.Add(grabble, 1);
        }

        /// <summary> Executes the 'trigger exit' action. </summary>
        /// <param name="other"> The other.</param>
        private void OnTriggerExit(Collider other)
        {
            NRGrabbableObject grabble = other.GetComponent<NRGrabbableObject>() ?? other.GetComponentInParent<NRGrabbableObject>();
            if (grabble == null)
                return;
            int count = 0;
            if (m_GrabReadyDict.TryGetValue(grabble, out count))
            {
                if (count > 1)
                    m_GrabReadyDict[grabble] = count - 1;
                else
                    m_GrabReadyDict.Remove(grabble);
            }
        }

        /// <summary> Executes the 'controller pose updated' action. </summary>
        private void OnControllerPoseUpdated()
        {
            if (updatePoseByRigidbody)
                return;
            UpdateGrabbles();
        }

        /// <summary> Updates the grabbles. </summary>
        private void UpdateGrabbles()
        {
            bool pressGrab = NRInput.GetButton(handEnum, grabButton);
            bool grabAction = !m_PreviousGrabPress && pressGrab;
            bool releaseAction = m_PreviousGrabPress && !pressGrab;
            m_PreviousGrabPress = pressGrab;
            if (grabAction && m_GrabbingList.Count == 0 && m_GrabReadyDict.Keys.Count != 0)
            {
                if (!grabMultiEnabled)
                {
                    NRGrabbableObject nearestGrabble = GetNearestGrabbleObject();
                    if (nearestGrabble)
                        GrabTarget(nearestGrabble);
                }
                else
                {
                    foreach (NRGrabbableObject grabble in m_GrabReadyDict.Keys)
                    {
                        GrabTarget(grabble);
                    }
                }
                SetChildrenCollidersEnabled(false);
            }

            if (releaseAction)
            {
                for (int i = 0; i < m_GrabbingList.Count; i++)
                {
                    m_GrabbingList[0].GrabEnd();
                }
                m_GrabbingList.Clear();
                SetChildrenCollidersEnabled(true);
            }

            if (m_GrabbingList.Count > 0 && !grabAction)
                MoveGrabbingObjects();
        }

        /// <summary> Gets nearest grabble object. </summary>
        /// <returns> The nearest grabble object. </returns>
        private NRGrabbableObject GetNearestGrabbleObject()
        {
            NRGrabbableObject nearestGrabble = null;
            float nearestSqrMagnitude = float.MaxValue;
            foreach (NRGrabbableObject grabbleObj in m_GrabReadyDict.Keys)
            {
                if (grabbleObj.AttachedColliders == null)
                    continue;
                for (int i = 0; i < grabbleObj.AttachedColliders.Length; i++)
                {
                    Vector3 closestPoint = grabbleObj.AttachedColliders[i].ClosestPointOnBounds(transform.position);
                    float grabbableSqrMagnitude = (transform.position - closestPoint).sqrMagnitude;
                    if (grabbableSqrMagnitude < nearestSqrMagnitude)
                    {
                        nearestSqrMagnitude = grabbableSqrMagnitude;
                        nearestGrabble = grabbleObj;
                    }
                }
            }
            return nearestGrabble;
        }

        /// <summary> Grab target. </summary>
        /// <param name="target"> Target for the.</param>
        private void GrabTarget(NRGrabbableObject target)
        {
            if (!target.CanGrab)
                return;
            target.GrabBegin(this);
            if (!m_GrabbingList.Contains(target))
                m_GrabbingList.Add(target);
            if (m_GrabReadyDict.ContainsKey(target))
                m_GrabReadyDict.Remove(target);
        }

        /// <summary> Move grabbing objects. </summary>
        private void MoveGrabbingObjects()
        {
            for (int i = 0; i < m_GrabbingList.Count; i++)
            {
                if (updatePoseByRigidbody)
                    m_GrabbingList[i].MoveRigidbody(transform.position, transform.rotation);
                else
                    m_GrabbingList[i].MoveTransform(transform.position, transform.rotation);
            }
        }

        /// <summary> Sets children colliders enabled. </summary>
        /// <param name="isEnabled"> True if is enabled, false if not.</param>
        private void SetChildrenCollidersEnabled(bool isEnabled)
        {
            if (m_ChildrenColliders == null)
                return;
            for (int i = 0; i < m_ChildrenColliders.Length; i++)
            {
                m_ChildrenColliders[i].enabled = isEnabled;
            }
        }
    }
}
