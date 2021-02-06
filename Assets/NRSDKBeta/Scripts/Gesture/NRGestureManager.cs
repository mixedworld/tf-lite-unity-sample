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

    /// <summary> Manager for nr gestures. </summary>
    public class NRGestureManager : MonoBehaviour
    {
        /// <summary> List of recognizers. </summary>
        private static List<NRGestureRecognizer> m_RecognizerList = new List<NRGestureRecognizer>();
        /// <summary> The native gesture. </summary>
        private static NativeGesture m_NativeGesture;

        /// <summary> True to recongnizing. </summary>
        private static bool m_Recongnizing;
        /// <summary> Gets or sets a value indicating whether the inited. </summary>
        /// <value> True if inited, false if not. </value>
        internal static bool Inited { get; private set; }

        /// <summary> Initializes this object. </summary>
        internal static void Init()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (m_NativeGesture == null)
            {
                m_NativeGesture = new NativeGesture();
                m_NativeGesture.SetCallback(OnGestureUpdate);
            }
            Inited = true;
#endif
        }

        /// <summary> Starts a recognize. </summary>
        internal static void StartRecognize()
        {
            if (m_NativeGesture != null)
                m_NativeGesture.StartRecognize();
        }

        /// <summary> Stops a recognize. </summary>
        internal static void StopRecognize()
        {
            if (m_NativeGesture != null)
                m_NativeGesture.StopRecognize();
        }

        /// <summary> Rigister gesture recognizer. </summary>
        /// <param name="gestureRecognizer"> The gesture recognizer.</param>
        internal static void RigisterGestureRecognizer(NRGestureRecognizer gestureRecognizer)
        {
            if (Inited)
            {
                if (gestureRecognizer == null || m_RecognizerList.Contains(gestureRecognizer))
                    return;
                m_RecognizerList.Add(gestureRecognizer);
            }
        }

        /// <summary> Un rigister gesture recognizer. </summary>
        /// <param name="gestureRecognizer"> The gesture recognizer.</param>
        internal static void UnRigisterGestureRecognizer(NRGestureRecognizer gestureRecognizer)
        {
            if (!Inited)
                Init();
            if (Inited)
            {
                if (gestureRecognizer == null)
                    return;
                if (m_RecognizerList.Contains(gestureRecognizer))
                    m_RecognizerList.Remove(gestureRecognizer);
            }
        }

        /// <summary> Destroys this object. </summary>
        internal static void Destroy()
        {
            if (m_NativeGesture != null)
            {
                m_NativeGesture.Destroy();
                m_NativeGesture = null;
            }
        }

        /// <summary> Executes the 'gesture update' action. </summary>
        /// <param name="gestureInfo">      Information describing the gesture.</param>
        /// <param name="gestureEventType"> Type of the gesture event.</param>
        private static void OnGestureUpdate(NRGestureInfo gestureInfo, NRGestureEventType gestureEventType)
        {
            for (int i = 0; i < m_RecognizerList.Count; i++)
            {
                NRGestureRecognizer gestureRecognizer = m_RecognizerList[i];
                if (gestureRecognizer == null)
                {
                    m_RecognizerList.Remove(gestureRecognizer);
                    continue;
                }
                gestureRecognizer.OnGestureUpdate?.Invoke(gestureInfo);
                if (gestureEventType != NRGestureEventType.UNDEFINED)
                    gestureRecognizer.OnGestureEventTriggered?.Invoke(gestureEventType, gestureInfo);
            }
        }
    }
}
