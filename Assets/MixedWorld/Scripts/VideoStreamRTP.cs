/****************************************************************************
* Copyright 2021 mixed.world. All rights reserved.
*                                                                                                                                                                                                                                                                    
*                                                                                                                                                           
* https://mixed.world        
* 
*****************************************************************************/

using NRKernal;
using NRKernal.Record;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace MixedWorld.VideoStream
{
    public class VideoStreamRTP : MonoBehaviour
    {
        
        /// <summary> The Streaming URL - where to stream to. </summary>
        /// <value> Streaming URL starting with protocol rtp </value>
        public string ReceiverURL
        {
            get
            {
                return receiverURL;
            }
            set
            {
                receiverURL = value;
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

        [SerializeField] RawImage camTexture = null;
        [SerializeField] RawImage blendTexture = null;
        [SerializeField] GameObject handtracking = null;


        [SerializeField] string receiverURL = @"rtp://192.168.178.77:42023";
        /// <summary> The video capture. </summary>
        NRVideoCapture m_VideoCapture = null;

        /// <summary> Starts this object. </summary>
        void Start()
        {
            CreateVideoCaptureTest();
        }

        /// <summary> Tests create video capture. </summary>
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

        
        /// <summary> Starts video capture. </summary>
        public void StartVideoCapture()
        {
            Resolution cameraResolution = NRVideoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
            NRDebugger.Info(cameraResolution);

            int cameraFramerate = NRVideoCapture.GetSupportedFrameRatesForResolution(cameraResolution).OrderByDescending((fps) => fps).First();
            NRDebugger.Info(cameraFramerate);

            if (m_VideoCapture != null)
            {
                NRDebugger.Info("Created VideoCapture Instance!");
                CameraParameters cameraParameters = new CameraParameters();
                cameraParameters.hologramOpacity = 0.0f;
                cameraParameters.frameRate = cameraFramerate;
                cameraParameters.cameraResolutionWidth = cameraResolution.width;
                cameraParameters.cameraResolutionHeight = cameraResolution.height;
                cameraParameters.pixelFormat = CapturePixelFormat.BGRA32;
                cameraParameters.blendMode = BlendMode.Blend;

                m_VideoCapture.StartVideoModeAsync(cameraParameters, OnStartedVideoCaptureMode);

                camTexture.texture = CameraTexture;
                blendTexture.texture = BlendTexture;
                handtracking.SetActive(true);
                //Previewer.SetData(m_VideoCapture.PreviewTexture, true);
            }
        }

        /// <summary> Stops video capture. </summary>
        public void StopVideoCapture()
        {
            if (m_VideoCapture == null)
            {
                return;
            }
            NRDebugger.Info("Stop Video Capture!");
            m_VideoCapture.StopRecordingAsync(OnStoppedRecordingVideo);
            //Previewer.SetData(m_VideoCapture.PreviewTexture, false);
        }

        /// <summary> Executes the 'started video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("Started Video Capture Mode!");
            m_VideoCapture.StartRecordingAsync(ReceiverURL, OnStartedRecordingVideo);
        }

        /// <summary> Executes the 'started recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStartedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("Started Recording Video!");
        }

        /// <summary> Executes the 'stopped recording video' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedRecordingVideo(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("Stopped Recording Video!");
            m_VideoCapture.StopVideoModeAsync(OnStoppedVideoCaptureMode);
        }

        /// <summary> Executes the 'stopped video capture mode' action. </summary>
        /// <param name="result"> The result.</param>
        void OnStoppedVideoCaptureMode(NRVideoCapture.VideoCaptureResult result)
        {
            NRDebugger.Info("Stopped Video Capture Mode!");
        }
    }
}
