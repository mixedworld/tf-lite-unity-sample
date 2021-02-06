/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

using UnityEngine;

namespace NRKernal.Beta.NRExamples
{
    /// <summary> A gesture recognize example. </summary>
    public class GestureRecognizeExample : MonoBehaviour
    {
        /// <summary> The gesture recognizer. </summary>
        private NRGestureRecognizer m_GestureRecognizer;

        /// <summary> Starts this object. </summary>
        void Start()
        {
            StartGesture();
        }

        /// <summary> Executes the 'destroy' action. </summary>
        private void OnDestroy()
        {
            DestroyGesture();
        }

        /// <summary> Starts a gesture. </summary>
        private void StartGesture()
        {
            m_GestureRecognizer = new NRGestureRecognizer();
            m_GestureRecognizer.OnGestureUpdate += OnGestureUpdate;
            m_GestureRecognizer.StartRecognize();
        }

        /// <summary> Destroys the gesture. </summary>
        private void DestroyGesture()
        {
            if (m_GestureRecognizer != null)
            {
                m_GestureRecognizer.Destroy();
                m_GestureRecognizer = null;
            }
        }

        /// <summary> Executes the 'gesture update' action. </summary>
        /// <param name="gestureInfo"> Information describing the gesture.</param>
        private void OnGestureUpdate(NRGestureInfo gestureInfo)
        {
            NRDebugger.Info(gameObject.name + " OnGestureUpdate:");
            NRDebugger.Info(gestureInfo.ToString());
        }
    }
}
