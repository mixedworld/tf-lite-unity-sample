using System;
using System.Collections;

using UnityEngine;

namespace MixedWorld.Mqtt.WebMqtt
{
    public class WebMqttClient : IMqttClient
    {
        private static readonly int WebSocketBrokerPort = 1884;
        private static readonly int WebSocketSslBrokerPort = 1885;
        private static readonly bool DefaultUseSSL = false;
        
        private int handle = -1;
        private string hostName = null;
        private int port = WebSocketBrokerPort;
        private bool useSsl = false;
        private string clientId = null;


        public bool IsConnected
        {
            get { return (this.handle != -1) && (WebMqttApi.GetConnectState(this.handle) == WebMqttApi.ConnectState.Connected); }
        }
        public int Port
        {
            get { return this.port; }
        }
        public string LastError
        {
            get
            {
                if (this.handle == -1) return null;
                return WebMqttApi.GetLastError(this.handle);
            }
        }


        public WebMqttClient(string clientId, string hostName) : this(clientId, hostName, (DefaultUseSSL ? WebSocketSslBrokerPort : WebSocketBrokerPort), DefaultUseSSL) { }
        public WebMqttClient(string clientId, string hostName, int port) : this(clientId, hostName, port, DefaultUseSSL) { }
        public WebMqttClient(string clientId, string hostName, bool ssl) : this(clientId, hostName, (ssl ? WebSocketSslBrokerPort : WebSocketBrokerPort), ssl) { }
        public WebMqttClient(string clientId, string hostName, int port, bool ssl)
        {
            this.clientId = clientId;
            this.hostName = hostName;
            this.port = port;
            this.useSsl = ssl;
        }

        public IEnumerator Connect(MqttLastWillTestament lastWill, string username = null, string password = null)
        {
            if (this.handle == -1)
            {
                // Since WebMqttApi is loaded on-demand and asynchronously, we'll have to wait
                // for it to become available, in case it isn't there yet. Connect is desgined 
                // as a Coroutine, so that we're just waiting until it's there.
                if (!WebMqttApi.IsLoaded)
                {
                    WebMqttApi.Load();

                    while (!WebMqttApi.IsLoaded)
                        yield return null;
                }
                
                this.handle = WebMqttApi.CreateClient(this.hostName, this.port, "/", this.clientId, this.useSsl);
            }

            // Early-out if we're establishing a connection
            WebMqttApi.ConnectState state = WebMqttApi.GetConnectState(this.handle);
            if (state == WebMqttApi.ConnectState.Connecting) yield break;

            // Reset the last error state before starting a new operation
            WebMqttApi.ClearLastError(this.handle);

            if (!string.IsNullOrEmpty(lastWill.Topic))
                WebMqttApi.SetLastWillMessage(this.handle, lastWill.Topic, lastWill.Payload, (lastWill.Payload == null) ? 0 : lastWill.Payload.Length, (byte)lastWill.QualityOfService, lastWill.Retain);
            else
                WebMqttApi.RemoveLastWillMessage(this.handle);

            // Start establishing the connection
            WebMqttApi.Connect(this.handle, username, password);

            // Wait until either a connection is established or an error occurred (and we're no longer trying)
            while (true)
            {
                yield return null;

                state = WebMqttApi.GetConnectState(this.handle);
                if (state != WebMqttApi.ConnectState.Connecting)
                    break;
            }
        }
        public void Disconnect()
        {
            if (this.handle != -1)
            {
                // Reset the last error state before starting a new operation
                WebMqttApi.ClearLastError(this.handle);

                // Start the disconnect procedure
                WebMqttApi.Disconnect(this.handle);
            }
        }

        public void Subscribe(string topic, MqttQualityOfService qualityOfService)
        {
            WebMqttApi.Subscribe(this.handle, topic, (byte)qualityOfService);
        }
        public void Unsubscribe(string topic)
        {
            WebMqttApi.Unsubscribe(this.handle, topic);
        }

        public void Publish(string topic, byte[] payloadBuffer, MqttQualityOfService qualityOfService, bool retain)
        {
            WebMqttApi.Publish(
                this.handle,
                topic,
                payloadBuffer,
                payloadBuffer.Length,
                (byte)qualityOfService,
                retain);
        }
        public bool TryReceiveMessage(out MqttMessage message)
        {
            message = default(MqttMessage);

            WebMqttApi.ConnectState connectState = WebMqttApi.GetConnectState(this.handle);
            if (connectState != WebMqttApi.ConnectState.Connected) return false;

            int messageHandle = WebMqttApi.DequeueMessage(this.handle);
            if (messageHandle < 0) return false;

            int receivedBytes = WebMqttApi.GetMessagePayloadLength(this.handle, messageHandle);

            message.Topic = WebMqttApi.GetMessageChannel(this.handle, messageHandle);
            message.Payload = new byte[receivedBytes];
            WebMqttApi.GetMessagePayload(this.handle, messageHandle, message.Payload, message.Payload.Length);

            WebMqttApi.DeleteMessage(this.handle, messageHandle);
            return true;
        }
    }
}
