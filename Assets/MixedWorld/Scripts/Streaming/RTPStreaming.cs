/****************************************************************************
* Copyright 2021 mixed.world. All rights reserved.
*                                                                                                                                                                                                                                                                 
* https://mixed.world      
* 
*****************************************************************************/

namespace MixedWorld.Streaming
{
    using NRKernal.Record;
    using System.Linq;
    using UnityEngine;
    using System;
    using System.IO;
    using UnityEngine.UI;
    using NRKernal;

    /// <summary> Streaming Controller to Stream Nreal View including MR Scene over RTP </summary>
    public class RTPStreaming : MonoBehaviour
    {
        /// <summary> The FPS configuration view. </summary>
        [SerializeField]
        private RTPConfigView m_RTPConfigView;
        /// <summary> The Handtracking GameObject </summary>
        [SerializeField]
        private GameObject m_handtracking = null;
        [SerializeField]
        private bool camOnly = false;
        /// <summary>
        /// The optional preview Image.
        /// </summary>
        [SerializeField]
        private RawImage m_previewImage = null;
        /// <summary> The Optional Camera View </summary>
        [SerializeField]
        private RawImage m_cameraImage = null;

        /// <summary> The Nreal video capture module. </summary>
        private NRVideoCapture m_VideoCapture = null;

        /// <summary> The RTP server IP. </summary>
        private string rtpServerIP = "highfive"; //"192.168.178.77";

        /// <summary> The RTP default Port. /// </summary>
        private string rtpPort = "1042";

        /// <summary>
        /// Determines if only the camera should be used for hand tracking without encoding it for streaming.
        /// </summary>
        public bool CamOnly
        {
            get
            {
                return camOnly;
            }
            set
            {
                camOnly = value;
            }
        }
        /// <summary>
        /// Returns the Camera Texture without the virtual screen.
        /// </summary>
        public RenderTexture CameraTexture
        {
            get
            {
                return m_VideoCapture?.GetContext()?.GetBlender().RGBTexture;
            }
        }
        /// <summary>
        /// Returns the Virtual Texture with Scene content but without Camera.
        /// </summary>
        public RenderTexture VirtualTexture
        {
            get
            {
                return m_VideoCapture?.GetContext()?.GetBlender().VirtualTexture;
            }
        }
        /// <summary>
        /// Returns the Blended Texture containing Camera and Scene content.
        /// </summary>
        public RenderTexture BlendTexture
        {
            get
            {
                return m_VideoCapture?.GetContext()?.GetBlender().BlendTexture;
            }
        }
        /// <summary> Returns the RTP Path including Protocoll, IP and Port. OR if it is a filename will save to disk. </summary>
        /// <value> The RTP IP Address. </value>
        public string RTPPath
        {
            get
            {
                if (rtpServerIP.Split('.').Length == 4)
                {
                    if (rtpServerIP.Split(':').Length == 2)
                    {
                        return string.Format(@"rtp://{0}", rtpServerIP);
                    }
                    else
                    {
                        return string.Format(@"rtp://{0}:{1}", rtpServerIP, rtpPort);
                    }
                }
                else
                {
                    // Saving to local filepath
                    string timeStamp = Time.time.ToString().Replace(".", "").Replace(":", "");
                    string filename = string.Format("{0}_{1}.mp4", rtpServerIP, timeStamp);
                    string filepath = Path.Combine(Application.persistentDataPath, filename);
                    return filepath;
                }
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
            m_RTPConfigView.OnClickStreaming += (ip) =>
            {
                rtpServerIP = ip;
                Debug.Log("Trying to stream to " + RTPPath);
                CastToServer();
            };

            m_RTPConfigView.OnClickCancel += () =>
            {
                StopVideoCapture();
            };

            CreateVideoCaptureTest();
            m_IsInitialized = true;
        }

        void CreateVideoCaptureTest()
        {
            NRVideoCapture.CreateAsync(false, delegate (NRVideoCapture videoCapture)
            {
                if (videoCapture != null)
                {
                    m_VideoCapture = videoCapture;
                }
                else
                {
                    NRDebugger.Error("Failed to create VideoCapture Instance!");
                }
            });
        }

        /// <summary> Starts the video capture and rtp streaming. </summary>
        private void CastToServer()
        {
            CreateVideoCapture(delegate ()
            {
                Debug.Log("Starting Video Streaming.");
                StartVideoCapture();
            });
        }

        #region video capture
        /// <summary> Creates video capture. </summary>
        /// <param name="callback"> The callback.</param>
        private void CreateVideoCapture(Action callback)
        {
            Debug.Log("VideoCapture Instance created.");

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
                    Debug.LogError("Failed to create VideoCapture Instance.");
                }
            });
        }

        /// <summary> Starts video capture. </summary>
        public void StartVideoCapture()
        {
            Resolution cameraResolution = NRVideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            Debug.Log("Camera Resolution:" + cameraResolution);

            int cameraFramerate = NRVideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
            Debug.Log("Camera Framerate:" + cameraFramerate);

            if (m_VideoCapture != null)
            {
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 1f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                cameraParameters.blendMode = BlendMode.Blend;

                m_VideoCapture.StartVideoModeAsync(cameraParameters, OnStartedVideoCaptureMode, camOnly);
                //if (m_cameraImage = null)
                m_cameraImage.texture = CameraTexture;
                //if (m_previewImage != null)
                m_previewImage.texture = BlendTexture;
                if (m_handtracking != null)
                {
                    m_handtracking.SetActive(true);
                }
            }
        }

        /// <summary> Stops video capture. </summary>
        public void StopVideoCapture()
        {
            m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
        }

        /// <summary> Executes the 'started video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            m_VideoCapture.StartRecordingAsync(RTPPath, OnStartedRecordingVideo);
        }

        /// <summary> Executes the 'stopped video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            m_VideoCapture = null;
        }

        /// <summary> Executes the 'started recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            Debug.Log("Video recording started.");
        }

        /// <summary> Executes the 'stopped recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            Debug.Log("Video recording stopped.");
            m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
        }
        #endregion
    }
}
