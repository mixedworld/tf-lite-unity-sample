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
    using System.Runtime.InteropServices;

    /// <summary> Callback, called when the nr glasses control brightness key. </summary>
    /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
    /// <param name="key_event">              The key event.</param>
    /// <param name="user_data">              Information describing the user.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NRGlassesControlBrightnessKeyCallback(UInt64 glasses_control_handle, int key_event, UInt64 user_data);

    /// <summary> Callback, called when the nr glasses control brightness value. </summary>
    /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
    /// <param name="value">                  The value.</param>
    /// <param name="user_data">              Information describing the user.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NRGlassesControlBrightnessValueCallback(UInt64 glasses_control_handle, int value, UInt64 user_data);

    /// <summary> Callback, called when the nr glasses control temperature. </summary>
    /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
    /// <param name="temperature">            The temperature.</param>
    /// <param name="user_data">              Information describing the user.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void NRGlassesControlTemperatureCallback(UInt64 glasses_control_handle, int temperature, UInt64 user_data);

    /// <summary> A native glasses controller extension. </summary>
    public static class NativeGlassesControllerExtension
    {
        /// <summary> A NativeGlassesController extension method that gets a duty. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The duty. </returns>
        public static int GetDuty(this NativeGlassesController glassescontroller)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return -1;
            }
            int duty = -1;
            var result = NativeApi.NRGlassesControlGetDuty(glassescontroller.GlassesControllerHandle, ref duty);
            NativeErrorListener.Check(result, glassescontroller, "GetDuty");
            return result == NativeResult.Success ? duty : -1;
        }

        /// <summary> A NativeGlassesController extension method that sets a duty. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="duty">              The duty.</param>
        public static void SetDuty(this NativeGlassesController glassescontroller, int duty)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return;
            }
            var result = NativeApi.NRGlassesControlSetDuty(glassescontroller.GlassesControllerHandle, duty);
            NativeErrorListener.Check(result, glassescontroller, "SetDuty");
        }

        /// <summary> A NativeGlassesController extension method that gets the brightness. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The brightness. </returns>
        public static int GetBrightness(this NativeGlassesController glassescontroller)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return -1;
            }
            int brightness = -1;
            var result = NativeApi.NRGlassesControlGetBrightness(glassescontroller.GlassesControllerHandle, ref brightness);
            NativeErrorListener.Check(result, glassescontroller, "GetBrightness");
            return result == NativeResult.Success ? brightness : -1;
        }

        /// <summary> A NativeGlassesController extension method that sets the brightness. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="brightness">        The brightness.</param>
        public static void SetBrightness(this NativeGlassesController glassescontroller, int brightness)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return;
            }
            var result = NativeApi.NRGlassesControlSetBrightness(glassescontroller.GlassesControllerHandle, brightness);
            NativeErrorListener.Check(result, glassescontroller, "SetBrightness");
        }

        /// <summary>
        /// A NativeGlassesController extension method that back, called when the regis glasses control
        /// brightness key. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="callback">          The callback.</param>
        /// <param name="userdata">          The userdata.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public static bool RegisGlassesControlBrightnessKeyCallBack(this NativeGlassesController glassescontroller, NRGlassesControlBrightnessKeyCallback callback, ulong userdata)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return false;
            }
            var result = NativeApi.NRGlassesControlSetBrightnessKeyCallback(glassescontroller.GlassesControllerHandle, callback, userdata);
            NativeErrorListener.Check(result, glassescontroller, "SetBrightnessKeyCallback");
            return result == NativeResult.Success;
        }

        /// <summary>
        /// A NativeGlassesController extension method that back, called when the regis glasses control
        /// brightness value. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="callback">          The callback.</param>
        /// <param name="userdata">          The userdata.</param>
        /// <returns> True if it succeeds, false if it fails. </returns>
        public static bool RegisGlassesControlBrightnessValueCallBack(this NativeGlassesController glassescontroller, NRGlassesControlBrightnessValueCallback callback, ulong userdata)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return false;
            }
            var result = NativeApi.NRGlassesControlSetBrightnessValueCallback(glassescontroller.GlassesControllerHandle, callback, userdata);
            NativeErrorListener.Check(result, glassescontroller, "SetBrightnessValueCallback");
            return result == NativeResult.Success;
        }

        /// <summary> A NativeGlassesController extension method that gets a temprature. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="temperatureType">   Type of the temperature.</param>
        /// <returns> The temprature. </returns>
        public static int GetTemprature(this NativeGlassesController glassescontroller, NativeGlassesTemperaturePosition temperatureType)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return 0;
            }
            int temp = 0;
            var result = NativeApi.NRGlassesControlGetTemperatureData(glassescontroller.GlassesControllerHandle, temperatureType, ref temp);
            NativeErrorListener.Check(result, glassescontroller, "GetTemprature");
            return result == NativeResult.Success ? temp : -1;
        }

        /// <summary> A NativeGlassesController extension method that gets a version. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The version. </returns>
        public static string GetVersion(this NativeGlassesController glassescontroller)
        {
            byte[] bytes = new byte[128];
            var result = NativeApi.NRGlassesControlGetVersion(glassescontroller.GlassesControllerHandle, bytes, bytes.Length);
            NativeErrorListener.Check(result, glassescontroller, "GetVersion");
            return System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// A NativeGlassesController extension method that gets glasses identifier. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The glasses identifier. </returns>
        public static string GetGlassesID(this NativeGlassesController glassescontroller)
        {
            byte[] bytes = new byte[64];
            var result = NativeApi.NRGlassesControlGetGlassesID(glassescontroller.GlassesControllerHandle, bytes, bytes.Length);
            NativeErrorListener.Check(result, glassescontroller, "GetGlassesID");
            return System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// A NativeGlassesController extension method that gets glasses serial number. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The glasses serial number. </returns>
        public static string GetGlassesSN(this NativeGlassesController glassescontroller)
        {
            byte[] bytes = new byte[64];
            var result = NativeApi.NRGlassesControlGetGlassesSN(glassescontroller.GlassesControllerHandle, bytes, bytes.Length);
            NativeErrorListener.Check(result, glassescontroller, "GetGlassesSN");
            return System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
        }

        /// <summary> A NativeGlassesController extension method that gets a mode. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <returns> The mode. </returns>
        public static NativeGlassesMode GetMode(this NativeGlassesController glassescontroller)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return NativeGlassesMode.ThreeD_1080;
            }
            NativeGlassesMode mode = NativeGlassesMode.TwoD_1080;
            var result = NativeApi.NRGlassesControlGet2D3DMode(glassescontroller.GlassesControllerHandle, ref mode);
            NativeErrorListener.Check(result, glassescontroller, "GetMode");
            return mode;
        }

        /// <summary> A NativeGlassesController extension method that sets a mode. </summary>
        /// <param name="glassescontroller"> The glassescontroller to act on.</param>
        /// <param name="mode">              The mode.</param>
        public static void SetMode(this NativeGlassesController glassescontroller, NativeGlassesMode mode)
        {
            if (glassescontroller.GlassesControllerHandle == 0)
            {
                return;
            }
            var result = NativeApi.NRGlassesControlSet2D3DMode(glassescontroller.GlassesControllerHandle, mode);
            NativeErrorListener.Check(result, glassescontroller, "SetMode");
        }

        /// <summary> A native api. </summary>
        private struct NativeApi
        {
            /// <summary> Nr glasses control get duty. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_dute">               [in,out] The out dute.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetDuty(UInt64 glasses_control_handle, ref int out_dute);

            /// <summary> Nr glasses control set duty. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="dute">                   The dute.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlSetDuty(UInt64 glasses_control_handle, int dute);

            /// <summary> Nr glasses control get brightness. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_brightness">         [in,out] The out brightness.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetBrightness(UInt64 glasses_control_handle, ref int out_brightness);

            /// <summary> Nr glasses control set brightness. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="brightness">             The brightness.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlSetBrightness(UInt64 glasses_control_handle, int brightness);

            /// <summary> Nr glasses control get temperature data. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="position">               The position.</param>
            /// <param name="out_temperature">        [in,out] The out temperature.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetTemperatureData(UInt64 glasses_control_handle, NativeGlassesTemperaturePosition position, ref int out_temperature);

            /// <summary> Nr glasses control get version. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_version">            The out version.</param>
            /// <param name="len">                    The length.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetVersion(UInt64 glasses_control_handle, byte[] out_version, int len);

            /// <summary> Nr glasses control get glasses identifier. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_glasses_id">         Identifier for the out glasses.</param>
            /// <param name="len">                    The length.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetGlassesID(UInt64 glasses_control_handle,
               byte[] out_glasses_id, int len);

            /// <summary> Nr glasses control get glasses serial number. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_glasses_sn">         The out glasses serial number.</param>
            /// <param name="len">                    The length.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGetGlassesSN(UInt64 glasses_control_handle,
                byte[] out_glasses_sn, int len);

            /// <summary> Nr glasses control get 3D mode. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="out_mode">               [in,out] The out mode.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlGet2D3DMode(UInt64 glasses_control_handle, ref NativeGlassesMode out_mode);

            /// <summary> Nr glasses control set 3D mode. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="mode">                   The mode.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary)]
            public static extern NativeResult NRGlassesControlSet2D3DMode(UInt64 glasses_control_handle, NativeGlassesMode mode);

            /// <summary> Callback, called when the nr glasses control set brightness key. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="callback">               The callback.</param>
            /// <param name="user_data">              Information describing the user.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGlassesControlSetBrightnessKeyCallback(UInt64 glasses_control_handle, NRGlassesControlBrightnessKeyCallback callback, UInt64 user_data);

            /// <summary> Callback, called when the nr glasses control set brightness value. </summary>
            /// <param name="glasses_control_handle"> Handle of the glasses control.</param>
            /// <param name="callback">               The callback.</param>
            /// <param name="user_data">              Information describing the user.</param>
            /// <returns> A NativeResult. </returns>
            [DllImport(NativeConstants.NRNativeLibrary, CallingConvention = CallingConvention.Cdecl)]
            public static extern NativeResult NRGlassesControlSetBrightnessValueCallback(UInt64 glasses_control_handle, NRGlassesControlBrightnessValueCallback callback, UInt64 user_data);
        }
    }
}
