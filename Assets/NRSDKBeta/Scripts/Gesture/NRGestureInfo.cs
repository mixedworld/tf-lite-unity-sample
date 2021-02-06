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

    /// <summary> Values that represent nr gesture basic types. </summary>
    public enum NRGestureBasicType
    {
        /// <summary> An enum constant representing the undefined option. </summary>
        UNDEFINED = -1,
        /// <summary> An enum constant representing the fist option. </summary>
        FIST = 0,
        /// <summary> An enum constant representing the one option. </summary>
        ONE = 1,
        /// <summary> position error. </summary>
        ERROR = 2,
        /// <summary> An enum constant representing the palm option. </summary>
        PALM = 3,
    }

    /// <summary> Values that represent nr gesture event types. </summary>
    public enum NRGestureEventType
    {
        /// <summary> An enum constant representing the undefined option. </summary>
        UNDEFINED = -1
    }

    /// <summary> Information about the nr gesture. </summary>
    public class NRGestureInfo
    {
        /// <summary> Type of the gesture. </summary>
        public NRGestureBasicType gestureType;
        /// <summary> The gesture position. </summary>
        public Vector3 gesturePosition;
        /// <summary> The gesture rotation. </summary>
        public Quaternion gestureRotation;

        /// <summary> Default constructor. </summary>
        public NRGestureInfo()
        {
            Clear();
        }

        /// <summary> Clears this object to its blank/initial state. </summary>
        public void Clear()
        {
            gestureType = NRGestureBasicType.UNDEFINED;
            gesturePosition = Vector3.zero;
            gestureRotation = Quaternion.identity;
        }

        /// <summary> Convert this object into a string representation. </summary>
        /// <returns> A string that represents this object. </returns>
        public override string ToString()
        {
            return string.Format("[NRGestureInfo] gestureType: {0}, position: {1}, rotation: {2}", gestureType, gesturePosition.ToString("f3"), gestureRotation.ToString("f3"));
        }
    }
}
