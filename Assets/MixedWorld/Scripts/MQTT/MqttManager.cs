using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

using MixedWorld.Utility;


namespace MixedWorld.Mqtt
{
    public class MqttManager : Singleton<MqttManager>
    {
        private static readonly float ReconnectDelay = 3.0f;

        [SerializeField] private bool autoConnect = true;
        [SerializeField] private string globalTopicPrefix = null;
        [SerializeField] private string brokerHostName = "mqtt-broker";
        [SerializeField] private int brokerPort = 0;
        [SerializeField] private string username = null;
        [SerializeField] private string password = null;
        [SerializeField] private bool useSsl = false;

        [Tooltip("Raised once when the Connect method is called before the connection attempt is made, therefore safe to set the LWT.")]
        [SerializeField] private UnityEvent connecting = new UnityEvent();
        [Tooltip("Raised after the a connection has been successfully established, including reconnects.")]
        [SerializeField] private UnityEvent connected = new UnityEvent();
        [Tooltip("Raised when the connection will be gracefully closed. Listeners are able to send their last messages. QoS 0 and 1 are garanteed to be sent out and recommended to be used as last messages. QoS 2 can only be guaranteed on certain backends or broker implementations, though may delay the actual discconect call.")]
        [SerializeField] private UnityEvent disconnecting = new UnityEvent();
        [Tooltip("Raised after the connection has been closed, either graceful or ungraceful.")]
        [SerializeField] private UnityEvent disconnected = new UnityEvent();

        private string clientId = null;
        private NameHash32 hashedClientId = NameHash32.Empty;
        private MqttLastWillTestament lastWill = default(MqttLastWillTestament);
        private IMqttClient mqttClient = null;
        private Coroutine connectRoutine = null;
        private Coroutine messagePumpRoutine = null;
        private Dictionary<string, MqttSubscription> subscriptions = new Dictionary<string, MqttSubscription>();

        #region Properties

        /// <summary>
        /// Global prefix that is applied internally for all subscriptions and sent messages.
        /// </summary>
        public string GlobalTopicPrefix
        {
            get { return this.globalTopicPrefix; }
            set
            {
                if (this.globalTopicPrefix != value)
                {
                    if (this.IsConnectedOrWaitingForConnection)
                    {
                        throw new InvalidOperationException(
                            $"Cannot change {nameof(this.GlobalTopicPrefix)} while already connected. " +
                            $"You need to disconnect first, then reconnect after the change.");
                    }

                    this.globalTopicPrefix = value;
                    //if (!this.globalTopicPrefix.EndsWith("/"))
                    //    this.globalTopicPrefix += "/";
                }
            }
        }

        /// <summary>
        /// Use SSL/TLS encryption for the connection. May not be supported on all backends.
        /// </summary>
        public bool UseSsl
        {
            get { return this.useSsl; }
            set { this.useSsl = value; }
        }

        /// <summary>
        /// Open the connection on Start and reconnect when the connection breaks. 
        /// </summary>
        public bool AutoConnect
        {
            get { return this.autoConnect; }
            set { this.autoConnect = value; }
        }

        /// <summary>
        /// Client's MQTT Id.
        /// </summary>
        public string ClientId
        {
            get { return this.clientId; }
        }

        /// <summary>
        /// Hashed version of the ClientID
        /// </summary>
        public NameHash32 HashedClientId
        {
            get
            {
                if (this.hashedClientId == NameHash32.Empty)
                    this.hashedClientId = new NameHash32(this.clientId);
                return this.hashedClientId;
            }
        }
        /// <summary>
        /// Is there an active and open connection to the broker.
        /// </summary>
        public bool IsConnected
        {
            get { return this.mqttClient != null && this.mqttClient.IsConnected && !this.IsWaitingForConnection; }
        }

        /// <summary>
        /// Is the client currently trying to connect to the broker, including wait time between attempts.
        /// </summary>
        public bool IsWaitingForConnection
        {
            get { return this.connectRoutine != null; }
        }

        /// <summary>
        /// Is there an open and active connection or is the client in the processs of establishing one.
        /// </summary>
        public bool IsConnectedOrWaitingForConnection
        {
            get { return this.IsConnected || this.IsWaitingForConnection; }
        }

        /// <summary>
        /// Set the last will message. Can only be changed as long as the connection has not been initiated or established.
        /// </summary>
        public MqttLastWillTestament LastWill
        {
            get { return this.lastWill; }
            set
            {
                if (this.IsConnectedOrWaitingForConnection) throw new InvalidOperationException("Cannot change last will after the connection is initiated or established.");
                this.lastWill = value;
            }
        }
        
        /// <summary>
        /// Raised once when the Connect method is called before the connection attempt is made, therefore safe to set the LWT.
        /// </summary>
        public UnityEvent Connecting
        {
            get { return this.connecting; }
        }

        /// <summary>
        /// Raised after the a connection has been successfully established, including reconnects.
        /// </summary>
        public UnityEvent Connected
        {
            get { return this.connected; }
        }

        /// <summary>
        /// Raised when the connection will be gracefully closed. Listeners are able to send their last messages. 
        /// QoS 0 and 1 are garanteed to be sent out and recommended to be used as last messages. 
        /// QoS 2 can only be guaranteed on certain backends or broker implementations, though may delay the actual discconect call.
        /// </summary>
        public UnityEvent Disconnecting
        {
            get { return this.disconnecting; }
        }

        /// <summary>
        /// Raised after the connection has been closed, either graceful or ungraceful.
        /// </summary>
        public UnityEvent Disonnected
        {
            get { return this.disconnected; }
        }

        #endregion

        #region Create and Connect

        protected override void Awake()
        {
            base.Awake();
            this.clientId = Guid.NewGuid().ToString();
        }

        private void Start()
        {
            if (this.autoConnect && !this.IsConnectedOrWaitingForConnection)
                this.Connect();
        }

        /// <summary>
        /// Opens a connection to the set or given broker.
        /// If there is currently an active and open connection or connecting attempt,
        /// those will be closed and aborted first by calling <see cref="Disconnect"/> internally.
        /// Will raise a <see cref="Connecting"/> event once, so listeners can prepare or set the LWT.
        /// After a connection is successfully established (including each reconnect), it will raise a <see cref="Connected"/> event.
        /// </summary>
        /// <param name="brokerHostName">Hostname or IP address</param>
        /// <param name="brokerPort">Port to be used. 0 means the backend implementation uses its default value.</param>
        /// <param name="username">Optional username for authorization</param>
        /// <param name="password">Optional password for authorization</param>
        public void Connect(string brokerHostName = null, int brokerPort = 0, string username = null, string password = null)
        {
            // Close any existing connection
            this.Disconnect();
            // Raise the event, that a connection is going to be established
            this.connecting.Invoke();

            // Change broker connection settings when specified
            if (brokerHostName != null) this.brokerHostName = brokerHostName;
            if (brokerPort != 0) this.brokerPort = brokerPort;
            if (username != null) this.username = username;
            if (password != null) this.password = password;

            try
            {
                if (this.brokerPort != 0)
                    this.mqttClient = MqttClientFactory.CreateClient(this.clientId, this.brokerHostName, this.brokerPort, this.useSsl);
                else
                    this.mqttClient = MqttClientFactory.CreateClient(this.clientId, this.brokerHostName, this.useSsl);
            }
            catch (Exception e)
            {
                Debug.LogFormat("Error setting up MQTT client: {1}", this, e);
                this.Disconnect();
                return;
            }

            this.connectRoutine = this.StartCoroutine(this.EstablishConnection());
        }

        private IEnumerator ReconnectTimer()
        {
            yield return new WaitForSeconds(ReconnectDelay);

            if (this.autoConnect)
                this.connectRoutine = this.StartCoroutine(this.EstablishConnection());
            else
                this.connectRoutine = null;
        }

        private IEnumerator EstablishConnection()
        {
            Debug.LogFormat("Connecting to MQTT broker '{0}' with port {1}.\nGlobal topic prefix: '{2}'\nLast will topic: '{3}'.", 
                this.brokerHostName, 
                this.mqttClient.Port,
                this.globalTopicPrefix,
                (string.IsNullOrEmpty(this.LastWill.Topic)) ? "not set" : string.Format("'{0}'", this.lastWill.Topic));

            if (!string.IsNullOrEmpty(this.username) || !string.IsNullOrEmpty(this.password))
                Debug.LogFormat("Authorizing MQTT connection via user '{0}'", this.username);
            else
                Debug.LogFormat("Using an MQTT connection without authorization.");

            // Utilize an  IEnumerator, like Unity Coroutines
            IEnumerator ConnectingRoutine = this.mqttClient.Connect(this.lastWill, this.username, this.password);

            while(true)
            {
                try
                {
                    bool hasNext = ConnectingRoutine.MoveNext();
                    if (!hasNext && this.mqttClient.IsConnected)
                        break;

                    if (!hasNext && !this.mqttClient.IsConnected)
                        throw new Exception("Connection failed for unknown reason.");

                    // Check for non-exception errors
                    string lastError = this.mqttClient.LastError;
                    if (!string.IsNullOrEmpty(lastError))
                        throw new Exception(lastError);
                }
                catch (Exception e)
                {
                    Debug.LogFormat("Error connecting to MQTT broker: {0}", e);
                    if (this.AutoConnect)
                        this.connectRoutine = this.StartCoroutine(this.ReconnectTimer());
                    else
                        this.connectRoutine = null;
                    yield break;
                }

                yield return null;
            }


            Debug.LogFormat("Connection to MQTT broker established.");
            this.InitMqttSubscriptions();
            this.connectRoutine = null;
            this.messagePumpRoutine = this.StartCoroutine(this.MessagePump());
            this.connected.Invoke();
        }

        #endregion

        #region Destroy and Disconnect

        protected override void OnDestroy()
        {
            this.Disconnect();
            base.OnDestroy();
        }

        /// <summary>
        /// Pause (graceful soft disconnect) the connection, if the application is suspended,
        /// and reconnect if the app is resumed and there has been a connection or connection
        /// attempt before suspending. This is not neccessarily, as all services should be capable
        /// of handling ungraceful disconnects and network hicups, though it's cleaner.
        /// </summary>
        /// <param name="pauseStatus"></param>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                this.Disconnect(false);
            }
            else if(this.mqttClient != null)
            {
                this.connectRoutine = this.StartCoroutine(this.EstablishConnection());
            }
        }

        /// <summary>
        /// Like <see cref="OnApplicationPause(bool)"/>, the MQTT connection is suspended when this script or gameobject is disabled.
        /// If the connection was suspended, do a reconnect attempt.
        /// </summary>
        private void OnEnable()
        {
            if (this.mqttClient != null)
            {
                this.connectRoutine = this.StartCoroutine(this.EstablishConnection());
            }
        }

        /// <summary>
        /// Like <see cref="OnApplicationPause(bool)"/>, the MQTT connection is suspended when this script or gameobject is disabled.
        /// </summary>
        private void OnDisable()
        {
            this.Disconnect(false);
        }

        /// <summary>
        /// Disconnects the client gracefully and closes the connection or aborts any current connection attempts.
        /// If there is an active and open conenction, it will raise the <see cref="Disconnecting"/> event first,
        /// so listeners have the opportunity to send some last messages. Afterwards raises the <see cref="Disonnected"/>
        /// event, if there was an active and open connection.
        /// Regardless of <see cref="AutoConnect"/> it does not start any reconnect attempt.
        /// Pending QoS 0 and 1 messages are guaranteed to be send out before closing the connection and may delay the disconnect.
        /// QoS 2 are only guarenteed on certain backends, though the actual behaviour is backend specific.
        /// </summary>
        public void Disconnect()
        {
            this.Disconnect(true);
        }

        private void Disconnect(bool discardConnection)
        {
            bool connectedOrPending = this.IsConnectedOrWaitingForConnection;
            bool connectedWasRaised = (this.messagePumpRoutine != null);
            
            if (this.connectRoutine != null)
            {
                this.StopCoroutine(this.connectRoutine);
                this.connectRoutine = null;
            }

            if (this.messagePumpRoutine != null)
            {
                this.StopCoroutine(this.messagePumpRoutine);
                this.messagePumpRoutine = null;
            }

            if (this.mqttClient != null)
            {
                if (connectedOrPending)
                {
                    // Only raise disconnecting and disconnected if we're sure the connected event was raised
                    if (connectedWasRaised)
                        this.disconnecting.Invoke();

                    this.mqttClient.Disconnect();
                }

                if(discardConnection)
                    this.mqttClient = null;

                // Only raise disconnecting and disconnected if we're sure the connected event was raised
                if (connectedWasRaised)
                    this.disconnected.Invoke();
            }
        }

        #endregion

        #region Update

        private IEnumerator MessagePump()
        {
            while (this.IsConnected)
            { 
                this.ReceiveMessages();
                yield return null;
            }

            Debug.LogFormat("Lost connection to MQTT broker.");

            this.messagePumpRoutine = null;
            this.disconnected.Invoke();

            if (this.AutoConnect && !this.IsConnectedOrWaitingForConnection)
                this.connectRoutine = this.StartCoroutine(this.ReconnectTimer());

            yield break;
        }

        private void ReceiveMessages()
        {
            Profiler.BeginSample("MqttManager.ReceiveMessages", this);

            MqttMessage message;
            while (this.mqttClient.TryReceiveMessage(out message))
            {
                //NetworkTrafficTracker.Instance.AddReceivedMessage(message.Topic, message.Payload != null ? message.Payload.Length : 0);
                message.Topic = this.TrimTopicPrefix(message.Topic);
                foreach (MqttSubscription subscription in this.subscriptions.Values)
                {
                    subscription.OnMessageReceived(message);
                }
            }

            Profiler.EndSample();
        }

        #endregion

        #region Subscribe, Publish, Unsubscribe

        /// <summary>
        /// Adds a callback to the specified topic.
        /// Each topic-callback combination is only registered once, so subsequent calls have no effect.
        /// Though they may be used to raise the subscription QoS.
        /// For each topic the highest QoS is used for the acutal MQTT subscription.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="callback"></param>
        /// <param name="qualityOfService"></param>
        public void Subscribe(string topic, MqttMessageCallback callback, MqttQualityOfService qualityOfService)
        {
            MqttSubscription subscription = null;
            bool sendSubscribe = false;

            if (!this.subscriptions.TryGetValue(topic, out subscription))
            {
                subscription = new MqttSubscription(topic, qualityOfService);
                this.subscriptions.Add(topic, subscription);
            }

            subscription.AddCallback(callback, qualityOfService, out sendSubscribe);

            if (this.IsConnected && sendSubscribe)
            {
                string prefixedTopic = this.PrependTopicPrefix(topic);
                this.mqttClient.Subscribe(prefixedTopic, qualityOfService);
            }
        }

        /// <summary>
        /// Removes a callback from a topic.
        /// If there is no callback for the topic, the call has no effect.
        /// If the last callback is removed from a topic, the client will
        /// remove the underlying MQTT subscription. 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="callback"></param>
        public void Unsubscribe(string topic, MqttMessageCallback callback)
        {
            MqttSubscription subscription;

            if (this.subscriptions.TryGetValue(topic, out subscription))
            {
                bool unsubscribe;
                subscription.RemoveCallback(callback, out unsubscribe);

                if (unsubscribe)
                {
                    this.subscriptions.Remove(topic);
                    if (this.IsConnected)
                    {
                        string prefixedTopic = this.PrependTopicPrefix(topic);
                        this.mqttClient.Unsubscribe(prefixedTopic);
                    }
                }
            }
        }

        /// <summary>
        /// Send out a message.
        /// Pending QoS 0 and 1 are guaranteed to be sent out before gracefully closing the connections.
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="payloadBuffer"></param>
        /// <param name="qualityOfService"></param>
        /// <param name="retain"></param>
        public void Publish(string topic, byte[] payloadBuffer, MqttQualityOfService qualityOfService, bool retain)
        {
            Profiler.BeginSample("MqttManager.Send", this);

            string prefixedTopic = this.PrependTopicPrefix(topic);
            this.mqttClient.Publish(prefixedTopic, payloadBuffer, qualityOfService, retain);

            //if (NetworkTrafficTracker.Instance != null)
            //    NetworkTrafficTracker.Instance.AddSentMessage(prefixedTopic, payloadBuffer != null ? payloadBuffer.Length : 0);

            Profiler.EndSample();
        }

        private void InitMqttSubscriptions()
        {
            foreach (KeyValuePair<string, MqttSubscription> subscription in this.subscriptions)
            {
                string prefixedTopic = this.PrependTopicPrefix(subscription.Key);
                this.mqttClient.Subscribe(prefixedTopic, subscription.Value.QualityOfService);
            }
        }

        /// <summary>
        /// Prepends the <see cref="GlobalTopicPrefix"/> to the specified topic, or does nothing
        /// if no global prefix was set.
        /// </summary>
        /// <param name="topic"></param>
        /// <returns></returns>
        private string PrependTopicPrefix(string topic)
        {
            if (string.IsNullOrEmpty(this.globalTopicPrefix))
                return topic;
            else
                return this.globalTopicPrefix + topic;
        }

        /// <summary>
        /// Trims the <see cref="GlobalTopicPrefix"/> from the specified topic, or does nothing
        /// if no global prefix was set, or the topic has not been prefixed with it.
        /// </summary>
        /// <param name="prefixedTopic"></param>
        /// <returns></returns>
        private string TrimTopicPrefix(string prefixedTopic)
        {
            if (string.IsNullOrEmpty(this.globalTopicPrefix) || !prefixedTopic.StartsWith(this.globalTopicPrefix))
                return prefixedTopic;
            else
                return prefixedTopic.Remove(0, this.globalTopicPrefix.Length);
        }

        #endregion
    }
}
