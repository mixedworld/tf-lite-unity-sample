using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace MixedWorld.Mqtt
{
    public class MqttSubscription
    {
        private Regex regexTopic = null;
        private MqttQualityOfService qualityOfService = MqttQualityOfService.AtMostOnce;
        private HashSet<MqttMessageCallback> callbacks = new HashSet<MqttMessageCallback>();

        public MqttQualityOfService QualityOfService {  get { return this.qualityOfService;  } }
        
        public MqttSubscription(string topic, MqttQualityOfService qualityOfService)
        {
            this.regexTopic = this.ConvertTopic(topic);
            this.qualityOfService = qualityOfService;
        }

        public void AddCallback(MqttMessageCallback callback, MqttQualityOfService qualityOfService, out bool sendSubscribe)
        {
            this.callbacks.Add(callback);

            // Check whether the subscribiton was just created and need to be send out.
            // Or if the QoS needs to be upgraded. Per MQTT protocol, a subscription to the very same topic,
            // will replace the orinial subscription and change its QoS. Therefore no need to unsubscibe first.
            sendSubscribe = ((this.qualityOfService < qualityOfService) || (this.callbacks.Count == 1));

            if (sendSubscribe)
                this.qualityOfService = qualityOfService;
        }

        public void RemoveCallback(MqttMessageCallback callback, out bool unsubscribe)
        {
            this.callbacks.Remove(callback);
            unsubscribe = (this.callbacks.Count == 0);
        }

        // ------------------------------------------------------------------ //
        // Following lines are copied from Müqqen project (https://github.com/jloehr/Mueqqen).
        // https://github.com/jloehr/Mueqqen/blob/master/Assets/Mueqqen/Scripts/MQTTSubscription.cs
        //
        // Copyright (c) 2018 Julian Löhr
        // Licensed under the MIT license.
        //
        // ------------------------------------------------------------------ //
        private Regex ConvertTopic(string Topic)
        {
            string ConvertedTopic = "";
            bool EndWildcard = Topic.EndsWith("/#");

            // Strip '#' otherwise it will be RegexEscaped
            if (EndWildcard)
            {
                Topic = Topic.Substring(0, Topic.Length - 1);
            }

            /* Split Topic by '+' Wildcard:
             *
             * Two Capture Groups
             * First one captures empty in case Topic starts with '+/' or ungreedy at least one abitrary character.
             * I.e. that is the Topic substring.
             * Second one captures the '+' wildcard, or end of line to get the last topic substring.
             * '+' character must be preceded by start of string or '/' and succeeded by '/' to not capture 
             *  any '+' that are between abitrary characters, e.g. "Foo/Garten+Roof/Bar.
             */
            foreach (Match Match in Regex.Matches(Topic, @"((?=\+\/)|.+?)(?:$|((?<=^)|(?<=\/))\+(?=\/))"))
            {
                // Replace '+' wildcard with regex that matches arbitrary characters until '/'
                if (Match.Index != 0)
                {
                    ConvertedTopic += @"[^\/]+";
                }

                ConvertedTopic += Regex.Escape(Match.Groups[1].Value);
            }

            // In case of '#' wildcard, add generic "match all" regex
            if (EndWildcard)
            {
                ConvertedTopic += @".+";
            }

            // Wrap Topic into start and end of string, so no substring are going to be matched
            ConvertedTopic = "^" + ConvertedTopic + "$";
            return new Regex(ConvertedTopic);
        }

        // ------------------------------------------------------------------ //
        //
        // End of MIT license
        //
        // ------------------------------------------------------------------ //

        public void OnMessageReceived(MqttMessage mqttMessage)
        {
            if (this.regexTopic.IsMatch(mqttMessage.Topic))
            {
                foreach (MqttMessageCallback callback in this.callbacks)
                {
                    try
                    {
                        callback(mqttMessage);
                    }
                    catch (Exception e)
                    {
                        //Debug.LogFormat("Error in MQTT subscription callback '{0}': {1}", this, e);
                        System.Console.WriteLine("Error in MQTT subscription callback '{0}': {1}", this, e);
                    }
                }
            }
        }

        public override string ToString()
        {
            return string.Format("'{0}' ({1}), {2} subscribers", this.regexTopic, this.qualityOfService, this.callbacks.Count);
        }
    }
}
