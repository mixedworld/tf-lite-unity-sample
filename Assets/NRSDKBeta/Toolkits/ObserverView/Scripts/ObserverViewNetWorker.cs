/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal.Beta.StreammingCast
{
    using NRKernal.Beta.NetWork;
    using System;
    using System.Collections;
    using UnityEngine;

    /// <summary> An observer view net worker. </summary>
    public class ObserverViewNetWorker
    {
        /// <summary> The context. </summary>
        ObserverViewFrameCaptureContext m_Context;
        /// <summary> The net work client. </summary>
        NetWorkClient m_NetWorkClient;

        /// <summary> The limit waitting time. </summary>
        private const float limitWaittingTime = 5f;
        /// <summary> True if is connected, false if not. </summary>
        private bool m_IsConnected = false;
        /// <summary> True if is jonin success, false if not. </summary>
        private bool m_IsJoninSuccess = false;
        /// <summary> True if is closed, false if not. </summary>
        private bool m_IsClosed = false;

        /// <summary> Constructor. </summary>
        /// <param name="contex"> (Optional) The contex.</param>
        public ObserverViewNetWorker(ObserverViewFrameCaptureContext contex = null)
        {
            this.m_Context = contex;

            m_NetWorkClient = new NetWorkClient();
            m_NetWorkClient.OnDisconnect += OnDisconnect;
            m_NetWorkClient.OnConnect += OnConnected;
            m_NetWorkClient.OnJoinRoomResult += OnJoinRoomResult;
            m_NetWorkClient.OnCameraParamUpdate += OnCameraParamUpdate;
        }

        /// <summary> Check server available. </summary>
        /// <param name="ip">       The IP.</param>
        /// <param name="callback"> The callback.</param>
        public void CheckServerAvailable(string ip, Action<bool> callback)
        {
            if (string.IsNullOrEmpty(ip))
            {
                callback?.Invoke(false);
            }
            else
            {
                NRKernalUpdater.Instance.StartCoroutine(CheckServerAvailableCoroutine(ip, callback));
            }
        }

        /// <summary> Check server available coroutine. </summary>
        /// <param name="ip">       The IP.</param>
        /// <param name="callback"> The callback.</param>
        /// <returns> An IEnumerator. </returns>
        private IEnumerator CheckServerAvailableCoroutine(string ip, Action<bool> callback)
        {
            // Start to connect the server.
            m_NetWorkClient.Connect(ip, 6000);
            float timeLast = 0;
            while (!m_IsConnected)
            {
                if (timeLast > limitWaittingTime || m_IsClosed)
                {
                    NRDebugger.Info("[ObserverView] Connect the server TimeOut!");
                    callback?.Invoke(false);
                    yield break;
                }
                timeLast += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            // Start to enter the room.
            m_NetWorkClient.EnterRoomRequest();

            timeLast = 0;
            while (!m_IsJoninSuccess)
            {
                if (timeLast > limitWaittingTime || m_IsClosed)
                {
                    NRDebugger.Info("[ObserverView] Join the server TimeOut!");
                    callback?.Invoke(false);
                    yield break;
                }
                timeLast += Time.deltaTime;
                yield return new WaitForEndOfFrame();
            }
            callback?.Invoke(true);
        }

        #region Net msg
        /// <summary> Executes the 'connected' action. </summary>
        private void OnConnected()
        {
            NRDebugger.Info("OnConnected...");
            m_IsConnected = true;
        }

        /// <summary> Executes the 'disconnect' action. </summary>
        private void OnDisconnect()
        {
            NRDebugger.Info("OnDisconnect...");
            this.Close();
        }

        /// <summary> Executes the 'camera parameter update' action. </summary>
        /// <param name="param"> The parameter.</param>
        private void OnCameraParamUpdate(CameraParam param)
        {
            if (this.m_Context == null)
            {
                return;
            }
            this.m_Context.GetBehaviour().UpdateCameraParam(param.fov);
        }

        /// <summary> Executes the 'join room result' action. </summary>
        /// <param name="result"> True to result.</param>
        private void OnJoinRoomResult(bool result)
        {
            NRDebugger.Info("OnJoinRoomResult :" + result);
            m_IsJoninSuccess = result;
            if (!result)
            {
                this.Close();
            }
        }
        #endregion

        /// <summary> Closes this object. </summary>
        public void Close()
        {
            m_NetWorkClient?.Dispose();
            m_NetWorkClient = null;
        }
    }
}
