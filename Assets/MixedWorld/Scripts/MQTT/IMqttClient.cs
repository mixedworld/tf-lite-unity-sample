using System.Collections;

namespace MixedWorld.Mqtt
{
    public interface IMqttClient
    {
        bool IsConnected { get; }
        int Port { get; }
        string LastError { get; }

        IEnumerator Connect(MqttLastWillTestament lastWill, string username = null, string password = null);
        void Disconnect();
        void Subscribe(string topic, MqttQualityOfService qualityOfService);
        void Unsubscribe(string topic);
        void Publish(string topic, byte[] payloadBuffer, MqttQualityOfService qualityOfService, bool retain);
        bool TryReceiveMessage(out MqttMessage message);
    }
}
