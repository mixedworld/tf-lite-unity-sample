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
    using System.Linq;
    using UnityEngine;
    using System;

    /// <summary> A first person streamming cast. </summary>
    public class FirstPersonStreammingCast : MonoBehaviour
    {
        /// <summary> The FPS configuration view. </summary>
        [SerializeField]
        private FPSConfigView m_FPSConfigView;
        /// <summary> The net worker. </summary>
        private ObserverViewNetWorker m_NetWorker;
        /// <summary> The video capture. </summary>
        private NRVideoCapture m_VideoCapture = null;

        /// <summary> The server IP. </summary>
        //private string serverIP = "192.168.31.6";
        private string serverIP = "192.168.178.77";


        /// <summary> Gets the full pathname of the rtp file. </summary>
        /// <value> The full pathname of the rtp file. </value>
        public string RTPPath
        {
            get
            {
                return string.Format(@"rtp://{0}:5555", serverIP);
            }
        }

        /// <summary> True if is initialized, false if not. </summary>
        private bool m_IsInitialized = false;

        /// <summary> Starts this object. </summary>
        void Start()
        {
            this.Init();
        }

        /// <summary> Initializes this object. </summary>
        private void Init()
        {
            if (m_IsInitialized)
            {
                return;
            }
            m_NetWorker = new ObserverViewNetWorker();

            m_FPSConfigView.OnClickStart += (ip) =>
            {
                m_NetWorker.CheckServerAvailable(ip, (result) =>
                {
                    NRDebugger.Info("[FirstPersonStreammingCast] Is the server {0} ok? {1}", ip, result);
                    if (result)
                    {
                        serverIP = ip;
                        CastToServer();
                    }
                });
            };

            m_FPSConfigView.OnClickStop += () =>
            {
                StopVideoCapture();
            };

            m_IsInitialized = true;
        }

        /// <summary> Converts this object to a server. </summary>
        private void CastToServer()
        {
            CreateVideoCapture(delegate ()
            {
                NRDebugger.Info("[FirstPersonStreammingCast] Start video capture.");
                StartVideoCapture();
            });
        }

        #region video capture
        /// <summary> Creates video capture. </summary>
        /// <param name="callback"> The callback.</param>
        private void CreateVideoCapture(Action callback)
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Created VideoCapture Instance!");

            if (m_VideoCapture != null)
            {
                callback?.Invoke();
                return;
            }

            NRVideoCapture.CreateAsync(false, delegate (NRVideoCapture videoCapture)
            {
                if (videoCapture != null)
                {
                    m_VideoCapture = videoCapture;

                    callback?.Invoke();
                }
                else
                {
                    NRDebugger.Error("[FirstPersonStreammingCast] Failed to create VideoCapture Instance!");
                }
            });
        }

        /// <summary> Starts video capture. </summary>
        public void StartVideoCapture()
        {
            Resolution cameraResolution = NRVideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            NRDebugger.Info("[FirstPersonStreammingCast]  cameraResolution:" + cameraResolution);

            int cameraFramerate = NRVideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
            NRDebugger.Info("[FirstPersonStreammingCast]  cameraFramerate:" + cameraFramerate);

            if (m_VideoCapture != null)
            {
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 1f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                cameraParameters.blendMode = BlendMode.Blend;

                m_VideoCapture.StartVideoModeAsync(cameraParameters, OnStartedVideoCaptureMode);
            }
        }

        /// <summary> Stops video capture. </summary>
        public void StopVideoCapture()
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Stop Video Capture!");
            m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
        }

        /// <summary> Executes the 'started video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Started Video Capture Mode!");
            m_VideoCapture.StartRecordingAsync(RTPPath, OnStartedRecordingVideo);
        }

        /// <summary> Executes the 'stopped video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Stopped Video Capture Mode!");
            m_VideoCapture = null;
        }

        /// <summary> Executes the 'started recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Started Recording Video!");
        }

        /// <summary> Executes the 'stopped recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("[FirstPersonStreammingCast] Stopped Recording Video!");
            m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
        }
        #endregion
    }
}
