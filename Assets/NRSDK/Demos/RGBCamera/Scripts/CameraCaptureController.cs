/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

using System;
using UnityEngine;
using UnityEngine.UI;

namespace NRKernal.NRExamples
{
    /// <summary> A controller for handling camera captures. </summary>
    [HelpURL("https://developer.nreal.ai/develop/unity/rgb-camera")]
    public class CameraCaptureController : MonoBehaviour
    {
        /// <summary> The capture image. </summary>
        public RawImage CaptureImage;
        /// <summary> Number of frames. </summary>
        public Text FrameCount;
        /// <summary> Gets or sets the RGB camera texture. </summary>
        /// <value> The RGB camera texture. </value>
        private NRRGBCamTexture RGBCamTexture { get; set; }

        /// <summary> Starts this object. </summary>
        private void Start()
        {
            RGBCamTexture = new NRRGBCamTexture();
            CaptureImage.texture = RGBCamTexture.GetTexture();
            RGBCamTexture.Play();
        }

        /// <summary> Updates this object. </summary>
        void Update()
        {
            //FrameCount.text = RGBCamTexture.FrameCount.ToString();
            FrameCount.text = $"Dimensions: {RGBCamTexture.Width}x{RGBCamTexture.Height}";
        }

        /// <summary> Plays this object. </summary>
        public void Play()
        {
            RGBCamTexture.Play();

            // The origin texture will be destroyed after call "Stop",
            // Rebind the texture.
            CaptureImage.texture = RGBCamTexture.GetTexture();
            // Nreal Cam Image is flipped vertically. So adjust for that.
            CaptureImage.uvRect = new Rect(1, 0, -1, 1);
            // Sometimes when a Raw Image if flipped it also needs to be rotated:
            CaptureImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
        }

        /// <summary> Pauses this object. </summary>
        public void Pause()
        {
            RGBCamTexture.Pause();
        }

        /// <summary> Stops this object. </summary>
        public void Stop()
        {
            RGBCamTexture.Stop();
        }

        /// <summary> Executes the 'destroy' action. </summary>
        void OnDestroy()
        {
            RGBCamTexture.Stop();
        }
    }
}
