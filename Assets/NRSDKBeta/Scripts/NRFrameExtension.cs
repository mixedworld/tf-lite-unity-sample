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
    /// <summary> NRFrameExtension is a extention class of NRFrame. </summary>
    public class NRFrameExtension
    {
        /// <summary> The eye pose. </summary>
        private static EyePoseData m_EyePose;

        /// <summary> Get the pose of device in unity world coordinate. </summary>
        /// <value> The eye pose. </value>
        public static EyePoseData EyePose
        {
            get
            {
                if (NRFrame.SessionStatus == SessionState.Running)
                {
                    NRSessionManager.Instance.NativeAPI.NativeHeadTracking.GetEyePose(ref m_EyePose.LEyePose, ref m_EyePose.REyePose);
                }
                return m_EyePose;
            }
        }
    }
}
