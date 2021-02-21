
namespace MixedWorld.Mqtt
{
    public struct MqttLastWillTestament
    {
        public string Topic;
        public byte[] Payload;
        public MqttQualityOfService QualityOfService;
        public bool Retain;

        public MqttLastWillTestament(string topic, byte[] payload, MqttQualityOfService qualityOfService, bool retain)
        {
            this.Topic = topic;
            this.Payload = payload;
            this.QualityOfService = qualityOfService;
            this.Retain = retain;
        }
    }
}
