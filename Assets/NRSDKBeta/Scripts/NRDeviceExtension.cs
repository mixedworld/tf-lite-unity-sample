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
    using AOT;
    using System;

    /// <summary> Values that represent nr brightness key events. </summary>
    public enum NRBrightnessKEYEvent
    {
        /// <summary> An enum constant representing the nr brightness key down option. </summary>
        NR_BRIGHTNESS_KEY_DOWN = 0,
        /// <summary> An enum constant representing the nr brightness key up option. </summary>
        NR_BRIGHTNESS_KEY_UP = 1,
    }

    /// <summary> Brightness key event. </summary>
    /// <param name="key"> The key.</param>
    public delegate void BrightnessKeyEvent(NRBrightnessKEYEvent key);
    /// <summary> Brightness value changed event. </summary>
    /// <param name="value"> The value.</param>
    public delegate void BrightnessValueChangedEvent(int value);

    /// <summary> A nr device extension. </summary>
    public static class NRDeviceExtension
    {
        /// <summary>
        /// Event queue for all listeners interested in OnBrightnessKeyCallback events. </summary>
        private static event BrightnessKeyEvent OnBrightnessKeyCallback;
        /// <summary>
        /// Event queue for all listeners interested in OnBrightnessValueCallback events. </summary>
        private static event BrightnessValueChangedEvent OnBrightnessValueCallback;
        /// <summary> The brightness minimum. </summary>
        public const int BRIGHTNESS_MIN = 0;
        /// <summary> The brightness maximum. </summary>
        public const int BRIGHTNESS_MAX = 7;

        /// <summary> A NRDevice extension method that adds an event listener to 'callback'. </summary>
        /// <param name="device">   The device to act on.</param>
        /// <param name="callback"> The callback.</param>
        public static void AddEventListener(this NRDevice device, BrightnessKeyEvent callback)
        {
            OnBrightnessKeyCallback += callback;
        }

        /// <summary>
        /// A NRDevice extension method that adds an event listener to 'callback'. </summary>
        /// <param name="device">   The device to act on.</param>
        /// <param name="callback"> The callback.</param>
        public static void AddEventListener(this NRDevice device, BrightnessValueChangedEvent callback)
        {
            OnBrightnessValueCallback += callback;
        }

        /// <summary> A NRDevice extension method that removes the event listener. </summary>
        /// <param name="device">   The device to act on.</param>
        /// <param name="callback"> The callback.</param>
        public static void RemoveEventListener(this NRDevice device, BrightnessKeyEvent callback)
        {
            OnBrightnessKeyCallback -= callback;
        }

        /// <summary> A NRDevice extension method that removes the event listener. </summary>
        /// <param name="device">   The device to act on.</param>
        /// <param name="callback"> The callback.</param>
        public static void RemoveEventListener(this NRDevice device, BrightnessValueChangedEvent callback)
        {
            OnBrightnessValueCallback -= callback;
        }

        /// <summary> A NRDevice extension method that gets the brightness. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <returns> The brightness. </returns>
        public static int GetBrightness(this NRDevice device)
        {
#if UNITY_EDITOR
            return 0;
#else
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return 0;
            }
            return device.NativeGlassesController.GetBrightness();
#endif
        }

        /// <summary> A NRDevice extension method that sets the brightness. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <param name="value">  The value.</param>
        public static void SetBrightness(this NRDevice device, int value)
        {
#if !UNITY_EDITOR
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return;
            }
            device.NativeGlassesController.SetBrightness(value);
#endif
        }

        /// <summary> A NRDevice extension method that gets a mode. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <returns> The mode. </returns>
        public static NativeGlassesMode GetMode(this NRDevice device)
        {
#if UNITY_EDITOR
            return NativeGlassesMode.ThreeD_1080;
#else
            if (device.IsGlassesPlugOut || device.NativeGlassesController == null)
            {
                return NativeGlassesMode.ThreeD_1080;
            }
            return device.NativeGlassesController.GetMode();
#endif
        }

        /// <summary> A NRDevice extension method that sets a mode. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <param name="mode">   The mode.</param>
        public static void SetMode(this NRDevice device, NativeGlassesMode mode)
        {
#if !UNITY_EDITOR
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return;
            }
            device.NativeGlassesController.SetMode(mode);
#endif
        }

        /// <summary> A NRDevice extension method that glasses version. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <returns> A string. </returns>
        public static string GlassesVersion(this NRDevice device)
        {
#if UNITY_EDITOR
            return "";
#else
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return "";
            }
            return device.NativeGlassesController.GetVersion();
#endif
        }

        /// <summary> A NRDevice extension method that glasses serial number. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <returns> A string. </returns>
        public static string GlassesSN(this NRDevice device)
        {
#if UNITY_EDITOR
            return "";
#else
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return "";
            }
            return device.NativeGlassesController.GetGlassesSN();
#endif
        }

        /// <summary> A NRDevice extension method that glasses identifier. </summary>
        /// <param name="device"> The device to act on.</param>
        /// <returns> A string. </returns>
        public static string GlassesID(this NRDevice device)
        {
#if UNITY_EDITOR
            return "";
#else
            if (device.IsGlassesPlugOut|| device.NativeGlassesController == null)
            {
                return "";
            }
            return device.NativeGlassesController.GetGlassesID();
#endif
        }

        /// <summary> Executes the 'brightness key callback internal' action. </summary>
        /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
        /// <param name="key_event">              The key event.</param>
        /// <param name="user_data">              Information describing the user.</param>
        [MonoPInvokeCallback(typeof(NRGlassesControlBrightnessKeyCallback))]
        private static void OnBrightnessKeyCallbackInternal(UInt64 glasses_control_handle, int key_event, UInt64 user_data)
        {
            OnBrightnessKeyCallback?.Invoke((NRBrightnessKEYEvent)key_event);
        }

        /// <summary> Executes the 'brightness value callback internal' action. </summary>
        /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
        /// <param name="brightness">             The brightness.</param>
        /// <param name="user_data">              Information describing the user.</param>
        [MonoPInvokeCallback(typeof(NRGlassesControlBrightnessValueCallback))]
        private static void OnBrightnessValueCallbackInternal(UInt64 glasses_control_handle, int brightness, UInt64 user_data)
        {
            OnBrightnessValueCallback?.Invoke(brightness);
        }

        /// <summary>
        /// A NRDevice extension method that regis glasses controller extra callbacks. </summary>
        /// <param name="device"> The device to act on.</param>
        public static void RegisGlassesControllerExtraCallbacks(this NRDevice device)
        {
            device.NativeGlassesController.RegisGlassesControlBrightnessKeyCallBack(OnBrightnessKeyCallbackInternal, 0);
            device.NativeGlassesController.RegisGlassesControlBrightnessValueCallBack(OnBrightnessValueCallbackInternal, 0);
        }
    }
}
