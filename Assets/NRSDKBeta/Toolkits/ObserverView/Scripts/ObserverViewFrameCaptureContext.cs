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
    using NRKernal.Record;
    using System;
    using UnityEngine;

    /// <summary> An observer view frame capture context. </summary>
    public class ObserverViewFrameCaptureContext
    {
        /// <summary> The blender. </summary>
        private ObserverViewBlender m_Blender;
        /// <summary> The frame provider. </summary>
        private AbstractFrameProvider m_FrameProvider;
        /// <summary> The capture behaviour. </summary>
        private NRObserverViewBehaviour m_CaptureBehaviour;
        /// <summary> The net worker. </summary>
        private ObserverViewNetWorker m_NetWorker;
        /// <summary> The encoder. </summary>
        private IEncoder m_Encoder;

        /// <summary> Options for controlling the camera. </summary>
        private CameraParameters m_CameraParameters;
        /// <summary> True if is initialize, false if not. </summary>
        private bool m_IsInit = false;
        /// <summary> True if is released, false if not. </summary>
        private bool m_IsReleased = false;

        /// <summary> Gets the preview texture. </summary>
        /// <value> The preview texture. </value>
        public Texture PreviewTexture
        {
            get
            {
                return m_Blender?.BlendTexture;
            }
        }

        /// <summary> Gets the behaviour. </summary>
        /// <returns> The behaviour. </returns>
        public NRObserverViewBehaviour GetBehaviour()
        {
            return m_CaptureBehaviour;
        }

        /// <summary> Gets frame provider. </summary>
        /// <returns> The frame provider. </returns>
        public AbstractFrameProvider GetFrameProvider()
        {
            return m_FrameProvider;
        }

        /// <summary> Request camera parameter. </summary>
        /// <returns> The CameraParameters. </returns>
        public CameraParameters RequestCameraParam()
        {
            return m_CameraParameters;
        }

        /// <summary> Gets the encoder. </summary>
        /// <returns> The encoder. </returns>
        public IEncoder GetEncoder()
        {
            return m_Encoder;
        }

        /// <summary> Default constructor. </summary>
        public ObserverViewFrameCaptureContext()
        {
        }

        /// <summary> Starts capture mode. </summary>
        /// <param name="param"> The parameter.</param>
        public void StartCaptureMode(CameraParameters param)
        {
            this.m_CameraParameters = param;
            this.m_CaptureBehaviour = this.GetObserverViewCaptureBehaviour();
            this.m_Encoder = new VideoEncoder();
            this.m_Encoder.Config(param);
            this.m_Blender = new ObserverViewBlender();
            this.m_Blender.Config(m_CaptureBehaviour.CaptureCamera, m_Encoder, param);
            this.m_NetWorker = new ObserverViewNetWorker(this);

            this.m_FrameProvider = new ObserverViewFrameProvider(m_CaptureBehaviour.CaptureCamera, this.m_CameraParameters.frameRate);
            this.m_FrameProvider.OnUpdate += this.m_Blender.OnFrame;
            this.m_IsInit = true;
        }

        /// <summary> Gets observer view capture behaviour. </summary>
        /// <returns> The observer view capture behaviour. </returns>
        private NRObserverViewBehaviour GetObserverViewCaptureBehaviour()
        {
            NRObserverViewBehaviour capture = GameObject.FindObjectOfType<NRObserverViewBehaviour>();
            Transform headParent = null;
            if (NRSessionManager.Instance.NRSessionBehaviour != null)
            {
                headParent = NRSessionManager.Instance.NRSessionBehaviour.transform.parent;
            }
            if (capture == null)
            {
                capture = GameObject.Instantiate(Resources.Load<NRObserverViewBehaviour>("Record/Prefabs/NRObserverViewBehaviour"), headParent);
            }
            GameObject.DontDestroyOnLoad(capture.gameObject);
            return capture;
        }

        /// <summary> Stops capture mode. </summary>
        public void StopCaptureMode()
        {
            this.Release();
        }

        /// <summary> Starts a capture. </summary>
        /// <param name="ip">       The IP.</param>
        /// <param name="callback"> The callback.</param>
        public void StartCapture(string ip, Action<bool> callback)
        {
            if (!m_IsInit)
            {
                callback?.Invoke(false);
                return;
            }
            ((VideoEncoder)m_Encoder).EncodeConfig.SetOutPutPath(string.Format("rtp://{0}:5555", ip));
            m_NetWorker.CheckServerAvailable(ip, (result) =>
            {
                NRDebugger.Info("[ObserverView] CheckServerAvailable : " + result);
                if (result)
                {
                    m_Encoder?.Start();
                    m_FrameProvider?.Play();
                }
                else
                {
                    this.Release();
                }
                callback?.Invoke(result);
            });
        }

        /// <summary> Stops a capture. </summary>
        public void StopCapture()
        {
            if (!m_IsInit)
            {
                return;
            }
            m_FrameProvider?.Stop();
            m_Encoder?.Stop();
        }

        /// <summary> Releases this object. </summary>
        public void Release()
        {
            if (!m_IsInit || m_IsReleased)
            {
                return;
            }
            m_FrameProvider?.Release();
            m_Blender?.Dispose();
            m_Encoder?.Release();
            m_NetWorker?.Close();

            m_FrameProvider = null;
            m_Blender = null;
            m_Encoder = null;
            m_NetWorker = null;

            GameObject.Destroy(m_CaptureBehaviour.gameObject);
            m_CaptureBehaviour = null;

            m_IsInit = false;
            m_IsReleased = true;
        }
    }
}
