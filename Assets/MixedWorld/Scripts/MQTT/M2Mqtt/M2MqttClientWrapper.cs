using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Security;

using UnityEngine;

using MixedWorld.Utility;

#if UNITY_EDITOR || !UNITY_WEBGL

using M2MqttClient = uPLibrary.Networking.M2Mqtt.MqttClient;
using M2MqttMsgPublishEventArgs = uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishEventArgs;
using M2MqttSslProtocols = uPLibrary.Networking.M2Mqtt.MqttSslProtocols;

namespace MixedWorld.Mqtt.M2Mqtt
{
    public class M2MqttClientWrapper : IMqttClient
    {
        private static readonly int DefaultBrokerPort = 1883;
        private static readonly int DefaultSslBrokerPort = 8883;
        private static readonly bool DefaultUseSsl = false;

        private string clientId = string.Empty;
        private string hostName = string.Empty;
        private int port = 0;
        private bool ssl = false;

        private M2MqttClient m2MqttClient = null;
        private string lastError = null;
        private Queue<MqttMessage> receivedQueue = new Queue<MqttMessage>();
        private object receivedQueueLock = new object();
        private HashSet<ushort> pendingPublishes = new HashSet<ushort>();
        private ManualResetEventSlim pendingPublishesEvent = new ManualResetEventSlim();
        private object pendingPublishesLock = new object();
        private AutoResetEvent connectMutexEvent = new AutoResetEvent(true);
        private ManualResetEventSlim disconnectFlag = new ManualResetEventSlim();

        public bool IsConnected
        {
            get { return this.m2MqttClient?.IsConnected ?? false; }
        }

        public int Port
        {
            get { return this.port; }
        }

        public string LastError
        {
            get { return this.lastError; }
        }

        public M2MqttClientWrapper(string clientId, string hostName) : this(clientId, hostName, (DefaultUseSsl ? DefaultSslBrokerPort : DefaultBrokerPort), DefaultUseSsl) { }
        public M2MqttClientWrapper(string clientId, string hostName, int port) : this(clientId, hostName, port, DefaultUseSsl) { }
        public M2MqttClientWrapper(string clientId, string hostName, bool ssl) : this(clientId, hostName, (ssl ? DefaultSslBrokerPort : DefaultBrokerPort), ssl) { }

        public M2MqttClientWrapper(string clientId, string hostName, int port, bool ssl)
        {
            this.clientId = clientId;
            this.hostName = hostName;
            this.port = port;
            this.ssl = ssl; 
        }

        public IEnumerator Connect(MqttLastWillTestament lastWill, string username = null, string password = null)
        {
            // If there is already an open connection or an connection attempt, that has no disconnect pending, early out.     
            if ((this.m2MqttClient != null) && (!this.disconnectFlag.IsSet))
                yield break;

            // Make sure any pending disconnect is finished.
            // Check for an ongoing connect worker thread, this may happen 
            // when disconnect and connect are called in close succession
            if (!this.connectMutexEvent.WaitOne(0))
            {
                Debug.LogFormat($"Ongoing MQTT Connect procedure detected, delaying until it is resolved.");
                while(!this.connectMutexEvent.WaitOne(0))
                    yield return null;
            }

            Exception connectExpection = null;
            ManualResetEventSlim connectDoneEvent = new ManualResetEventSlim();
            BackgroundWorker ConnectTask = null;

            this.disconnectFlag.Reset();

            try
            {
                // Instantiate a new client instance for each connection attempt.
                // Due to certain errors in the M2MQTT lib, it is not recommended to reuse the instance.
                this.CreateAndBindNewInstance();

                // Use a local reference copy, so when disconnect is called the worker thread does 
                // not run into a NullRef exception.
                M2MqttClient connectionInstance = this.m2MqttClient;

                // Do the actual Connect call in a worker thread, as it is a blocking call
                ConnectTask = new BackgroundWorker(
                    "M2MQTT Connect Worker",
                    (o) =>
                    {
                        try
                        {
                            connectionInstance.Connect(
                                this.clientId,
                                username,
                                password,
                                lastWill.Retain,
                                (byte)lastWill.QualityOfService,
                                !string.IsNullOrEmpty(lastWill.Topic),
                                lastWill.Topic,
                                lastWill.Payload,
                                true,
                                60);

                            // There is a race condition, where an open m2mqtt connection will be leaked.
                            // If there is a disconnect call during the connect call, disconnect won't do any work.
                            // The main thread will dispose all references, though won't terminate this thread.
                            // This connect thread will continue and open the connection, despite the main thread thinking
                            // it has disconnected. Therefore use this flag to signal, that disconnect has been called
                            // and the connection shall be closed immediately.
                            if (this.disconnectFlag.IsSet && connectionInstance.IsConnected)
                                connectionInstance.Disconnect();
                        }
                        catch (Exception e)
                        {
                            connectExpection = e;
                        }
                        finally
                        {
                            connectDoneEvent.Set();
                            this.connectMutexEvent.Set();
                        }

                        return Enumerable.Empty<float>();
                    });

                ConnectTask.Run(null);
            }
            // If instantiating the new client fails, i.e. host could not be resolved,
            // or anything else fails, at least reset, so a new connect attempt can be made.
            catch (Exception e)
            {
                connectExpection = e;
                connectDoneEvent.Set();
                this.connectMutexEvent.Set();
            }

            // Wait till the worker thread is finished
            while (!connectDoneEvent.IsSet)
                yield return null;

            ConnectTask?.Dispose(); 
            connectDoneEvent.Dispose();

            // Connect has failed, reset and rethrow
            if (connectExpection != null)
            {
                this.Disconnect();
                throw connectExpection;
            }

            yield break;
        }

        public void Disconnect()
        {
            // Check if there is an connection or connection attempt
            if ((this.m2MqttClient == null) || (this.disconnectFlag.IsSet))
                return;

            // Canel the connection attempt.
            // Set the flag, so the Connect Thread will disconnect immediately after connecting.
            this.disconnectFlag.Set();

            // If there has been a connection, wait for remaining messages to be published.
            if(this.m2MqttClient.IsConnected)
                this.WaitUntilMessagesArePublished();

            // Disconnect the client and reset this instance.
            // Disconnect internally simply tries to send a message first. There are certain race conditions
            // and no double call checks that may result in an exception, so simply catch 'em all and ignore those.
            try
            {
                if (this.m2MqttClient.IsConnected)
                    this.m2MqttClient.Disconnect();
            }
            catch { }
            finally
            {
                this.UnbindFromInstance();
            }
        }

        private void CreateAndBindNewInstance()
        {
            // The last two SSL/TLS arguments are only considered when the ssl argument is actually true. Therefore we can pass those even when no SSL/TLS is used.
            //this.m2MqttClient = new M2MqttClient(this.hostName, this.port, this.ssl, null, null, M2MqttSslProtocols.TLSv1_2, new RemoteCertificateValidationCallback(CertificationValidator.Instance.ValidateServerCertificate));
            this.m2MqttClient = new M2MqttClient(this.hostName, this.port, this.ssl, null, null, M2MqttSslProtocols.TLSv1_2, null);



            this.m2MqttClient.MqttMsgPublishReceived += this.OnMessageReceived;
            this.m2MqttClient.MqttMsgPublished += this.OnMessagePublished;
            this.m2MqttClient.ConnectionClosed += this.OnConnectionClosed;
        }

        private void UnbindFromInstance()
        {
            this.m2MqttClient.MqttMsgPublishReceived -= this.OnMessageReceived;
            this.m2MqttClient.MqttMsgPublished -= this.OnMessagePublished;
            this.m2MqttClient.ConnectionClosed -= this.OnConnectionClosed;
            this.m2MqttClient = null;
        }

        public void Subscribe(string topic, MqttQualityOfService qualityOfService)
        {
            this.m2MqttClient.Subscribe(new string[] { topic }, new byte[] { (byte)qualityOfService });
        }

        public void Unsubscribe(string topic)
        {
            this.m2MqttClient.Unsubscribe(new string[] { topic });
        }

        public void Publish(string topic, byte[] payloadBuffer, MqttQualityOfService qualityOfService, bool retain)
        {
            ushort messageId = this.m2MqttClient.Publish(topic, payloadBuffer, (byte)qualityOfService, retain);

            if(qualityOfService != MqttQualityOfService.AtMostOnce)
            {
                lock (this.pendingPublishesLock)
                    this.pendingPublishes.Add(messageId);
            }
        }

        private void OnMessagePublished(object sender, uPLibrary.Networking.M2Mqtt.Messages.MqttMsgPublishedEventArgs e)
        {
            if (e.IsPublished)
            {
                lock (this.pendingPublishesLock)
                {
                    this.pendingPublishes.Remove(e.MessageId);
                    this.pendingPublishesEvent.Set();
                }
            }
        }

        private void OnConnectionClosed(object sender, EventArgs e)
        {
            lock (this.pendingPublishesLock)
            {
                if(this.pendingPublishes.Count > 0)
                {
                    Debug.LogFormat($"MQTT Connection closed. Discarding {this.pendingPublishes} pending messages.");
                    this.pendingPublishes.Clear();
                    this.pendingPublishesEvent.Set();
                }
            }

            // Clean up after ungracefull disconnect.
            this.Disconnect();
        }

        private void WaitUntilMessagesArePublished()
        {
            while(true)
            {
                lock (this.pendingPublishesLock)
                {
                    if (this.pendingPublishes.Count == 0)
                        return;
                    else
                    {
                        Debug.LogFormat("{0} pending messages, waiting until all are published.", this.pendingPublishes.Count);
                        this.pendingPublishesEvent.Reset();
                    }
                }

                if(!this.pendingPublishesEvent.Wait(500))
                {
                    Debug.LogFormat($"MQTT pending messages have timed out, discarding remaining {this.pendingPublishes.Count}");
                    this.pendingPublishes.Clear();
                    this.pendingPublishesEvent.Set();
                    break;
                }
            }
        }

        public bool TryReceiveMessage(out MqttMessage message)
        {
            lock (this.receivedQueueLock)
            {
                if (this.receivedQueue.Count == 0)
                {
                    message = default(MqttMessage);
                    return false;
                }
                else
                {
                    message = this.receivedQueue.Dequeue();
                    return true;
                }
            }
        }

        private void OnMessageReceived(object sender, M2MqttMsgPublishEventArgs e)
        {
            lock (this.receivedQueueLock)
            {
                this.receivedQueue.Enqueue(new MqttMessage(e.Topic, e.Message));
            }
        }
    }
}
#endif
