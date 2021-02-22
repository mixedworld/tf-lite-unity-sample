#if UNITY_WEBGL && !UNITY_EDITOR
#define USE_WEBMQTT
#else
#define USE_M2MQTT
#endif

namespace MixedWorld.Mqtt
{
    public static class MqttClientFactory
    {
        public static IMqttClient CreateClient(string clientId, string hostName, bool ssl)
        {
#if USE_WEBMQTT
            return new WebMqtt.WebMqttClient(clientId, hostName, ssl);
#elif USE_M2MQTT
            return new M2Mqtt.M2MqttClientWrapper(clientId, hostName, ssl);
#endif
        }

        public static IMqttClient CreateClient(string clientId, string hostName, int port, bool ssl)
        {
#if USE_WEBMQTT
            return new WebMqtt.WebMqttClient(clientId, hostName, port, ssl);
#elif USE_M2MQTT
            return new M2Mqtt.M2MqttClientWrapper(clientId, hostName, port, ssl);
#endif
        }
    }
}
