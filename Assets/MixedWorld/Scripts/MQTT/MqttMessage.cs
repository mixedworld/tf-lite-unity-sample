
namespace MixedWorld.Mqtt
{
    public struct MqttMessage
    {
        public string Topic;
        public byte[] Payload;

        public MqttMessage(string topic, byte[] payload)
        {
            this.Topic = topic;
            this.Payload = payload;
        }
    }
}
