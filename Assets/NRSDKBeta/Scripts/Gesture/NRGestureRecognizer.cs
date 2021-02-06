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

    /// <summary> A nr gesture recognizer. </summary>
    public class NRGestureRecognizer
    {
        /// <summary> The on gesture update. </summary>
        public Action<NRGestureInfo> OnGestureUpdate;
        /// <summary> The on gesture event triggered. </summary>
        public Action<NRGestureEventType, NRGestureInfo> OnGestureEventTriggered;

        /// <summary> Default constructor. </summary>
        public NRGestureRecognizer()
        {
            if (!NRGestureManager.Inited)
                NRGestureManager.Init();
            NRGestureManager.RigisterGestureRecognizer(this);
        }

        /// <summary> Starts a recognize. </summary>
        public void StartRecognize()
        {
            NRGestureManager.StartRecognize();
        }

        /// <summary> Stops a recognize. </summary>
        public void StopRecognize()
        {
            NRGestureManager.StopRecognize();
        }

        /// <summary> Destroys the given stopReco. </summary>
        /// <param name="stopReco"> (Optional) The stop reco to destroy.</param>
        public void Destroy(bool stopReco = true)
        {
            if (stopReco)
                StopRecognize();
            OnGestureUpdate = null;
            OnGestureEventTriggered = null;
            NRGestureManager.UnRigisterGestureRecognizer(this);
        }
    }
}
