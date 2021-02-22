using System;
using System.IO;
using UnityEngine;
using System.Runtime.InteropServices;

namespace MixedWorld.Mqtt.WebMqtt
{
    public static class WebMqttApi
    {
        private static InitState initState = InitState.Unavailable;

        private enum InitState
        {
            Unavailable,
            Loading,
            Available
        }

        public enum ConnectState
        {
            Error = -1,
            Disconnected = 0,
            Connecting = 1,
            Connected = 2
        }

        public static bool IsLoaded
        {
            get
            {
                if (initState == InitState.Available) return true;
                if (MqttIsLibraryInitialized())
                {
                    initState = InitState.Available;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }


        public static void Load()
        {
            if (initState == InitState.Loading) return;
            if (initState == InitState.Available) return;

            string libUrl = Path.Combine(Application.streamingAssetsPath, "Platform", "Web", "paho-mqtt-min.js");
            MqttInitLibrary(libUrl);

            initState = InitState.Loading;
        }
        public static int CreateClient(string host, int port, string path, string clientId, bool useSSL = false)
        {
            if (!IsLoaded) throw new InvalidOperationException("WebMqttApi is not loaded yet. Call Load() first and wait for IsLoaded to return true.");
            return MqttClientCreate(host, port, path, clientId, useSSL);
        }

        public static void Connect(int handle, string username = null, string password = null)
        {
            MqttClientConnect(handle, username, password);
        }
        public static void Disconnect(int handle)
        {
            MqttClientDisconnect(handle);
        }

        public static ConnectState GetConnectState(int handle)
        {
            return (ConnectState)MqttClientGetConnectState(handle);
        }
        public static string GetLastError(int handle)
        {
            return MqttClientGetLastError(handle);
        }
        public static void ClearLastError(int handle)
        {
            MqttClientClearLastError(handle);
        }

        public static void Subscribe(int handle, string topic, int qualityOfService)
        {
            MqttClientSubscribe(handle, topic, qualityOfService);
        }
        public static void Unsubscribe(int handle, string topic)
        {
            MqttClientUnsubscribe(handle, topic);
        }

        public static void Publish(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain)
        {
            MqttClientPublish(handle, topic, payload, payloadLength, qualityOfService, retain);
        }

        public static void SetLastWillMessage(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain)
        {
            MqttClientSetLastWillMessage(handle, topic, payload, payloadLength, qualityOfService, retain);
        }
        public static void RemoveLastWillMessage(int handle)
        {
            MqttClientSetLastWillMessage(handle, string.Empty, null, 0, 0, false);
        }

        public static int GetQueuedMessageCount(int handle)
        {
            return MqttClientGetQueuedMessageCount(handle);
        }
        public static int DequeueMessage(int handle)
        {
            return MqttClientDequeueMessage(handle);
        }

        public static int GetMessagePayloadLength(int handle, int messageHandle)
        {
            return MqttClientGetMessagePayloadLength(handle, messageHandle);
        }
        public static int GetMessagePayload(int handle, int messageHandle, byte[] buffer, int bufferLength)
        {
            return MqttClientGetMessagePayload(handle, messageHandle, buffer, bufferLength);
        }
        public static string GetMessageChannel(int handle, int messageHandle)
        {
            return MqttClientGetMessageChannel(handle, messageHandle);
        }
        public static bool GetMessageRetained(int handle, int messageHandle)
        {
            return MqttClientGetMessageRetained(handle, messageHandle);
        }
        public static int GetMessageQualityOfService(int handle, int messageHandle)
        {
            return MqttClientGetMessageQualityOfService(handle, messageHandle);
        }
        public static bool GetMessageDuplicate(int handle, int messageHandle)
        {
            return MqttClientGetMessageDuplicate(handle, messageHandle);
        }

        public static void DeleteMessage(int handle, int messageHandle)
        {
            MqttClientDeleteMessage(handle, messageHandle);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int MqttInitLibrary(string libUrl);
        [DllImport("__Internal")]
        private static extern bool MqttIsLibraryInitialized();
        [DllImport("__Internal")]
        private static extern int MqttClientCreate(string host, int port, string path, string clientId, bool useSsl);
        [DllImport("__Internal")]
        private static extern void MqttClientConnect(int handle, string username, string password);
        [DllImport("__Internal")]
        private static extern void MqttClientDisconnect(int handle);
        [DllImport("__Internal")]
        private static extern int MqttClientGetConnectState(int handle);
        [DllImport("__Internal")]
        private static extern string MqttClientGetLastError(int handle);
        [DllImport("__Internal")]
        private static extern void MqttClientClearLastError(int handle);
        [DllImport("__Internal")]
        private static extern void MqttClientSubscribe(int handle, string topic, int qualityOfService);
        [DllImport("__Internal")]
        private static extern void MqttClientUnsubscribe(int handle, string topic);
        [DllImport("__Internal")]
        private static extern void MqttClientPublish(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain);
        [DllImport("__Internal")]
        private static extern void MqttClientSetLastWillMessage(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain);
        [DllImport("__Internal")]
        private static extern int MqttClientGetQueuedMessageCount(int handle);
        [DllImport("__Internal")]
        private static extern int MqttClientDequeueMessage(int handle);
        [DllImport("__Internal")]
        private static extern int MqttClientGetMessagePayloadLength(int handle, int messageHandle);
        [DllImport("__Internal")]
        private static extern int MqttClientGetMessagePayload(int handle, int messageHandle, byte[] buffer, int bufferLength);
        [DllImport("__Internal")]
        private static extern string MqttClientGetMessageChannel(int handle, int messageHandle);
        [DllImport("__Internal")]
        private static extern bool MqttClientGetMessageRetained(int handle, int messageHandle);
        [DllImport("__Internal")]
        private static extern int MqttClientGetMessageQualityOfService(int handle, int messageHandle);
        [DllImport("__Internal")]
        private static extern bool MqttClientGetMessageDuplicate(int handle, int messageHandle);
        [DllImport("__Internal")]
        private static extern void MqttClientDeleteMessage(int handle, int messageHandle);
#else
        private static int MqttInitLibrary(string libUrl) { throw new NotImplementedException(); }
        private static bool MqttIsLibraryInitialized() { throw new NotImplementedException(); }
        private static int MqttClientCreate(string host, int port, string path, string clientId, bool useSsl) { throw new NotImplementedException(); }
        private static void MqttClientConnect(int handle, string username, string password) { throw new NotImplementedException(); }
        private static void MqttClientDisconnect(int handle) { throw new NotImplementedException(); }
        private static int MqttClientGetConnectState(int handle) { throw new NotImplementedException(); }
        private static string MqttClientGetLastError(int handle) { throw new NotImplementedException(); }
        private static void MqttClientClearLastError(int handle) { throw new NotImplementedException(); }
        private static void MqttClientSubscribe(int handle, string topic, int qualityOfService) { throw new NotImplementedException(); }
        private static void MqttClientUnsubscribe(int handle, string topic) { throw new NotImplementedException(); }
        private static void MqttClientPublish(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain) { throw new NotImplementedException(); }
        private static void MqttClientSetLastWillMessage(int handle, string topic, byte[] payload, int payloadLength, int qualityOfService, bool retain) { throw new NotImplementedException(); }
        private static int MqttClientGetQueuedMessageCount(int handle) { throw new NotImplementedException(); }
        private static int MqttClientDequeueMessage(int handle) { throw new NotImplementedException(); }
        private static int MqttClientGetMessagePayloadLength(int handle, int messageHandle) { throw new NotImplementedException(); }
        private static int MqttClientGetMessagePayload(int handle, int messageHandle, byte[] buffer, int bufferLength) { throw new NotImplementedException(); }
        private static string MqttClientGetMessageChannel(int handle, int messageHandle) { throw new NotImplementedException(); }
        private static bool MqttClientGetMessageRetained(int handle, int messageHandle) { throw new NotImplementedException(); }
        private static int MqttClientGetMessageQualityOfService(int handle, int messageHandle) { throw new NotImplementedException(); }
        private static bool MqttClientGetMessageDuplicate(int handle, int messageHandle) { throw new NotImplementedException(); }
        private static void MqttClientDeleteMessage(int handle, int messageHandle) { throw new NotImplementedException(); }
#endif
    }
}
