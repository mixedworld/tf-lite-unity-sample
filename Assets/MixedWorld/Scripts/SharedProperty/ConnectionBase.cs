
using MixedWorld.Mqtt;
using MixedWorld.Utility;
using System;
using System.Text;


public class SampleEventArgs : EventArgs
{
    public string senderName { get; set; }
}

public class ConnectionBase
{

    private bool autoConnect = true;
    private bool isMqttConnectionEstablished = false;
    public bool acceptOwnRetainedMsg = false;
    private bool isConnected = false;
    private bool connectionEstablished = false;
    private float idleCounterConnectionEstablished = 0f;


    public string fullTopic = "default";


    public bool ConnectionEstablished
    {
        get { return this.connectionEstablished; }
        private set { this.connectionEstablished = value; }
    }


    public bool AutoConnect
    {
        get { return this.autoConnect; }
        set { this.autoConnect = value; }
    }
    public NameHash32 ClientId
    {
        get
        {
            return MqttManager.Instance.HashedClientId;
        }
    }

    public bool IsConnected
    {
        get { return this.isConnected; }
    }


    private string TopicPrefix
    {
        get { return MqttManager.Instance.GlobalTopicPrefix; }
    }

    private void OnEnable()
    {
        if (this.autoConnect)
            this.Connect();
    }
    private void OnDisable()
    {
        this.Disconnect();
    }

    public void Connect()
    {
        this.Disconnect();

        this.OnConnecting();

        MqttManager.Instance.Connected.AddListener(this.OnMqttConnected);
        MqttManager.Instance.Disonnected.AddListener(this.OnMqttDisconnected);

        MqttManager.Instance.Subscribe(
                    this.fullTopic,
                    this.OnMqttMessageReceived,
                    MqttQualityOfService.AtMostOnce);

        this.isConnected = true;
        if (MqttManager.Instance.IsConnected)
            this.OnMqttConnected();
    }

    public virtual void OnMqttMessageReceived(MqttMessage mqttMessage) {  }

    public void Disconnect()
    {
        if (!this.isConnected) return;

        this.OnDisconnecting();

        if (MqttManager.Instance != null)
        {
            MqttManager.Instance.Connected.RemoveListener(this.OnMqttConnected);
            MqttManager.Instance.Disonnected.RemoveListener(this.OnMqttDisconnected);

            MqttManager.Instance.Unsubscribe(
                this.fullTopic,
                this.OnMqttMessageReceived);
        }


        this.isConnected = false;
        this.isMqttConnectionEstablished = false;

        this.OnDisconnected();
    }

    private void OnMqttConnected()
    {
        this.isMqttConnectionEstablished = true;
        this.OnConnected();
    }
    private void OnMqttDisconnected()
    {
        this.isMqttConnectionEstablished = false;
        this.OnDisconnected();
    }


    public bool SendString(string json)
    {
        if (!this.isConnected)
        {
            Connect();
        }
        if (!this.isConnected)
        {
            return false;
        }
        try
        {
            byte[] AsBytes = Encoding.UTF8.GetBytes(json);

            // Send message data to the other clients
            MqttManager.Instance.Publish(
                this.fullTopic,
                AsBytes,
                MqttQualityOfService.AtMostOnce,
                true);
        }
        catch (Exception e)
        {
        }
        return true;
    }

    private void OnConnecting()
    { }
    private void OnConnected()
    {
        acceptOwnRetainedMsg = true;
    }
    private void OnDisconnecting()
    {
        connectionEstablished = false;
    }
    private void OnDisconnected()
    {
        connectionEstablished = false;
    }
    private void OnSynchronized()
    {
    }

    private void OnConnectionMsgReceived(MqttMessage mqttMessage)
    {
        if(Encoding.UTF8.GetString(mqttMessage.Payload).Equals(ClientId.ToString()))
        {
            connectionEstablished = true;
        }

    }
    public void RecievedAllRetainMsg()
    {
        if (!connectionEstablished)
        {
                
            if (idleCounterConnectionEstablished < UnityEngine.Time.time)
            {

                MqttManager.Instance.Subscribe(
                            "Echo/Test",
                            this.OnConnectionMsgReceived,
                            MqttQualityOfService.AtMostOnce);

                // Send message data to the other clients
                MqttManager.Instance.Publish(
                    "Echo/Test",
                    Encoding.UTF8.GetBytes(ClientId.ToString()),
                    MqttQualityOfService.AtMostOnce,
                    false);
                
                idleCounterConnectionEstablished += UnityEngine.Time.time + 3f;
            }
        }
    }
}