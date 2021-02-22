using System;

namespace MixedWorld.Mqtt.Utility
{
    public class Endpoint
    {
        private static readonly byte[] PayloadTrue = { 1 };
        private static readonly byte[] PayloadFalse = { 0 };

        public event Action<bool> OnValueReceived;

        private string endpointTopic;
        private bool retained;
        private MqttMessageCallback onMessageReceived;

        public bool Value { get; private set; }
        
        public Endpoint(string topic, bool retained = true)
        {
            this.endpointTopic = topic;
            this.retained = retained;
            this.Value = false;
            this.OnValueReceived += (value) => { this.Value = value; };
            this.onMessageReceived = (message) => { this.OnValueReceived?.Invoke((message.Payload[0] != 0)); };
        }

        public void Subscribe()
        {
            MqttManager.Instance.Subscribe(
                this.endpointTopic,
                this.onMessageReceived,
                MqttQualityOfService.AtMostOnce);
        }

        public void Unsubscribe()
        {
            MqttManager.Instance.Unsubscribe(
                this.endpointTopic,
                this.onMessageReceived);
        }

        public void SendValue(bool value)
        {
            MqttManager.Instance.Publish(this.endpointTopic, value ? PayloadTrue : PayloadFalse, MqttQualityOfService.AtMostOnce, this.retained);
        }
    }
}
