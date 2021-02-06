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
    using System;
    using System.Runtime.InteropServices;
    using UnityEngine;

    /// <summary> Session Native API. </summary>
    internal partial class NativeGesture
    {
        /// <summary> The on gesture update. </summary>
        private static Action<NRGestureInfo, NRGestureEventType> OnGestureUpdate;
        /// <summary> Information describing the current gesture. </summary>
        private static NRGestureInfo m_CurrentGestureInfo;

        /// <summary> True to valid. </summary>
        private bool m_Valid;
        /// <summary> Handle of the gesture. </summary>
        private UInt64 m_GestureHandle;
        /// <summary> Type of the current event. </summary>
        private NRGestureEventType m_CurrentEventType = NRGestureEventType.UNDEFINED;

        /// <summary> Callback, called when the nr gesture data. </summary>
        /// <param name="gesture_handle">      Handle of the gesture.</param>
        /// <param name="gesture_data_handle"> Handle of the gesture data.</param>
        /// <param name="user_data">           Information describing the user.</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate void NRGestureDataCallback(UInt64 gesture_handle, UInt64 gesture_data_handle, UInt64 user_data);

        /// <summary> Default constructor. </summary>
        public NativeGesture()
        {
            var result = NativeApi.NRGestureCreate(ref m_GestureHandle);
            NRDebugger.Info("[NativeGesture] NRGestureCreate: " + result.ToString());
            if (result == NativeResult.Success)
            {
                m_Valid = true;
                SetGestureDataCallback();
            }
        }

        /// <summary> Callback, called when the set. </summary>
        /// <param name="callback"> The callback.</param>
        internal void SetCallback(Action<NRGestureInfo, NRGestureEventType> callback)
        {
            if (m_CurrentGestureInfo == null)
                m_CurrentGestureInfo = new NRGestureInfo();
            OnGestureUpdate = callback;
        }

        /// <summary> Starts a recognize. </summary>
        internal void StartRecognize()
        {
            if (!m_Valid)
                return;
            var result = NativeApi.NRGestureStart(m_GestureHandle);
            NRDebugger.Info("[NativeGesture] NRGestureStart: " + result.ToString());
        }

        /// <summary> Stops a recognize. </summary>
        internal void StopRecognize()
        {
            if (!m_Valid)
                return;
            var result = NativeApi.NRGestureStop(m_GestureHandle);
            NRDebugger.Info("[NativeGesture] NRGestureStop: " + result.ToString());
        }

        /// <summary> Destroys this object. </summary>
        internal void Destroy()
        {
            if (!m_Valid)
                return;
            OnGestureUpdate = null;
            var result = NativeApi.NRGestureDestroy(m_GestureHandle);
            NRDebugger.Info("[NativeGesture] NRGestureDestroy: " + result.ToString());
        }

        /// <summary> Executes the 'gesture data callback' action. </summary>
        /// <param name="gesture_handle">      Handle of the gesture.</param>
        /// <param name="gesture_data_handle"> Handle of the gesture data.</param>
        /// <param name="userdata">            The userdata.</param>
        private static void OnGestureDataCallback(UInt64 gesture_handle, UInt64 gesture_data_handle, UInt64 userdata)
        {
            int out_gesture_type = 0;
            NativeApi.NRGestureGetGestureType(gesture_handle, gesture_data_handle, ref out_gesture_type);
            int out_event_type = 0;
            NativeApi.NRGestureGetEventType(gesture_handle, gesture_data_handle, ref out_event_type);
            NativeMat4f out_hand_pose = new NativeMat4f(Matrix4x4.identity);
            NativeApi.NRGestureGetHandPose(gesture_handle, gesture_data_handle, ref out_hand_pose);
            Pose unitypose;
            ConversionUtility.ApiPoseToUnityPose(out_hand_pose, out unitypose);

            //NRDebugger.Error(string.Format("[NativeGesture] gesture info: gesture_type:{0}, gesture_event:{1}", out_gesture_type, out_event_type));

            if(m_CurrentGestureInfo != null)
            {
                m_CurrentGestureInfo.gestureType = (NRGestureBasicType)out_gesture_type;
                m_CurrentGestureInfo.gesturePosition = unitypose.position;
                m_CurrentGestureInfo.gestureRotation = unitypose.rotation;
                OnGestureUpdate?.Invoke(m_CurrentGestureInfo, (NRGestureEventType)out_event_type);
            }
        }

        /// <summary> Callback, called when the set gesture data. </summary>
        /// <returns> True if it succeeds, false if it fails. </returns>
        private bool SetGestureDataCallback()
        {
            var result = NativeApi.NRGestureSetCaptureCallback(m_GestureHandle, OnGestureDataCallback, 0);
            NRDebugger.Info("[NativeGesture] NRGestureSetCaptureCallback: " + result.ToString());
            return result == NativeResult.Success;
        }

        /// <summary> A native api. </summary>
        private struct NativeApi
        {
            /// <summary> Nr gesture create. </summary>
            /// <param name="out_gesture_handle"> [in,out] Handle of the out gesture.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureCreate(ref UInt64 out_gesture_handle);

            /// <summary> Nr gesture start. </summary>
            /// <param name="gesture_handle"> Handle of the gesture.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureStart(UInt64 gesture_handle);

            /// <summary> Nr gesture stop. </summary>
            /// <param name="gesture_handle"> Handle of the gesture.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureStop(UInt64 gesture_handle);

            /// <summary> Nr gesture destroy. </summary>
            /// <param name="gesture_handle"> Handle of the gesture.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureDestroy(UInt64 gesture_handle);

            /// <summary> Callback, called when the nr gesture set capture. </summary>
            /// <param name="gesture_handle">   Handle of the gesture.</param>
            /// <param name="gesture_callback"> The gesture callback.</param>
            /// <param name="user_data">        Information describing the user.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGestureSetCaptureCallback(UInt64 gesture_handle, NRGestureDataCallback gesture_callback, UInt64 user_data);

            /// <summary> Nr gesture get gesture type. </summary>
            /// <param name="gesture_handle">      Handle of the gesture.</param>
            /// <param name="gesture_data_handle"> Handle of the gesture data.</param>
            /// <param name="out_gesture_type">    [in,out] Type of the out gesture.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureGetGestureType(UInt64 gesture_handle, UInt64 gesture_data_handle, ref int out_gesture_type);

            /// <summary> Nr gesture get event type. </summary>
            /// <param name="gesture_handle">      Handle of the gesture.</param>
            /// <param name="gesture_data_handle"> Handle of the gesture data.</param>
            /// <param name="out_event_type">      [in,out] Type of the out event.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureGetEventType(UInt64 gesture_handle, UInt64 gesture_data_handle, ref int out_event_type);

            /// <summary> Nr gesture get hand pose. </summary>
            /// <param name="gesture_handle">      Handle of the gesture.</param>
            /// <param name="gesture_data_handle"> Handle of the gesture data.</param>
            /// <param name="out_hand_pose">       [in,out] The out hand pose.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGestureGetHandPose(UInt64 gesture_handle, UInt64 gesture_data_handle, ref NativeMat4f out_hand_pose);
        }
    }
}
