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
    using UnityEngine;

    /// <summary> HMDPoseTracker oprate the pose tracking. </summary>
    public class NRHMDPosTrackerExperiment : MonoBehaviour
    {
        /// <summary> Values that represent pose sources. </summary>
        public enum PoseSource
        {
            /// <summary> An enum constant representing the left eye option. </summary>
            LeftEye = 0,
            /// <summary> An enum constant representing the right eye option. </summary>
            RightEye = 1,
            /// <summary> An enum constant representing the center eye option. </summary>
            CenterEye = 2,
        }

        /// <summary> Values that represent tracking types. </summary>
        public enum TrackingType
        {
            /// <summary> An enum constant representing the rotation and position option. </summary>
            RotationAndPosition = 0,
            /// <summary> An enum constant representing the rotation only option. </summary>
            RotationOnly = 1,
            /// <summary> An enum constant representing the position only option. </summary>
            PositionOnly = 2
        }

        /// <summary> The pose source. </summary>
        [SerializeField]
        private PoseSource m_PoseSource;
        /// <summary> Type of the tracking. </summary>
        [SerializeField]
        private TrackingType m_TrackingType;

        /// <summary> True to use relative transform. </summary>
        public bool UseRelativeTransform = false;

        /// <summary> True if is initialized, false if not. </summary>
        private bool isInitialized = false;

        /// <summary> Initializes this object. </summary>
        private void Init()
        {
            bool result;
            var leftCamera = transform.Find("LeftCamera").GetComponent<Camera>();
            var rightCamera = transform.Find("RightCamera").GetComponent<Camera>();
            var matrix_data = NRFrame.GetEyeProjectMatrix(out result, leftCamera.nearClipPlane, leftCamera.farClipPlane);
            if (result)
            {
                if (m_PoseSource == PoseSource.CenterEye)
                {
                    leftCamera.projectionMatrix = matrix_data.LEyeMatrix;
                    rightCamera.projectionMatrix = matrix_data.REyeMatrix;

                    var eyeposFromHead = NRFrame.EyePoseFromHead;
                    leftCamera.transform.localPosition = eyeposFromHead.LEyePose.position;
                    leftCamera.transform.localRotation = eyeposFromHead.LEyePose.rotation;

                    rightCamera.transform.localPosition = eyeposFromHead.REyePose.position;
                    rightCamera.transform.localRotation = eyeposFromHead.REyePose.rotation;
                }
                else
                {
                    var matrix = m_PoseSource == PoseSource.LeftEye ? matrix_data.LEyeMatrix : matrix_data.REyeMatrix;
                    gameObject.GetComponent<Camera>().projectionMatrix = matrix;
                    NRDebugger.Info("[HMDPoseTracker Init] apply matrix:" + matrix.ToString());
                }

                isInitialized = true;
            }
        }

        /// <summary> Updates this object. </summary>
        public void Update()
        {
            if (!isInitialized)
            {
                this.Init();
            }

            UpdatePos();
        }

        /// <summary> Updates the position. </summary>
        private void UpdatePos()
        {
            switch (m_PoseSource)
            {
                case PoseSource.LeftEye:
                    UpdatePoseByeTrackingType(NRFrameExtension.EyePose.LEyePose);
                    break;
                case PoseSource.RightEye:
                    UpdatePoseByeTrackingType(NRFrameExtension.EyePose.REyePose);
                    break;
                case PoseSource.CenterEye:
                    UpdatePoseByeTrackingType(NRFrame.HeadPose);
                    break;
                default:
                    break;
            }
        }

        /// <summary> Updates the pose bye tracking type described by pose. </summary>
        /// <param name="pose"> The pose.</param>
        private void UpdatePoseByeTrackingType(Pose pose)
        {
            switch (m_TrackingType)
            {
                case TrackingType.RotationAndPosition:
                    if (UseRelativeTransform)
                    {
                        transform.localRotation = pose.rotation;
                        transform.localPosition = pose.position;
                    }
                    else
                    {
                        transform.rotation = pose.rotation;
                        transform.position = pose.position;
                    }
                    NRDebugger.Error(pose.ToString());
                    break;
                case TrackingType.RotationOnly:
                    if (UseRelativeTransform)
                    {
                        transform.localRotation = pose.rotation;
                    }
                    else
                    {
                        transform.rotation = pose.rotation;
                    }
                    break;
                case TrackingType.PositionOnly:
                    if (UseRelativeTransform)
                    {
                        transform.localPosition = pose.position;
                    }
                    else
                    {
                        transform.position = pose.position;
                    }
                    break;
                default:
                    break;
            }
        }
    }
}
