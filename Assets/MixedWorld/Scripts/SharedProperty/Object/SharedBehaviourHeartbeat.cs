using Microsoft.MixedReality.Toolkit;
using MixedWorld.Mqtt;
using MixedWorld.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnbiasedTimeManager;
using UnityEngine;



namespace MixedWorld.Sharing
{

    public class HeartbeatTimer
    {
        private DateTime _LastBeatDataTime;
        private ulong _UnbiasedTimeULong;



        public DateTime LastBeatDataTime
        {
            get { return _LastBeatDataTime; }
            set { _LastBeatDataTime = value; }
        }
        public ulong UnbiasedTimeULong
        {
            get { return _UnbiasedTimeULong; }
            set { _UnbiasedTimeULong = value; }
        }

    }
    [RequireComponent(typeof(ObjectIdentifier))]
    public class SharedBehaviourHeartbeat : MonoBehaviour
    {
        private float idleLastUpdateTime = 0;
        public float heartbeatInterval = 2f;
        public float heartbeatDelay = 2f;
        public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

        private SharedPropertyManager<HeartbeatTimer> heartbeatTimer;

        private ObjectIdentifier objectIdentifier;

        private string heartbeatTopic;





        private void OnEnable()
        {
            objectIdentifier = EnsureObjectIdentifier();
       
            UnbiasedTime.Instance.onTimeReceive += OnTimeReceive;

            MainTopicBuilder.Instance.Maintopic = "Main";
            MainTopicBuilder.Instance.Subtopic = "SharedProperty";

            heartbeatTopic = MainTopicBuilder.Instance.GetMainTopic() + "/" + objectIdentifier.ObjectId + "/#";


            MqttManager.Instance?.Subscribe(
                heartbeatTopic,
                this.OnMqttMessageReceived,
            MqttQualityOfService.AtMostOnce);
        }


        private void OnDisable()
        {

            UnbiasedTime.Instance.onTimeReceive -= OnTimeReceive;

            MqttManager.Instance?.Unsubscribe(
                heartbeatTopic,
                this.OnMqttMessageReceived);
        }
        // Start is called before the first frame update
        void Start()
        {
            MainTopicBuilder.Instance.Maintopic = "Main";
            MainTopicBuilder.Instance.Subtopic = "SharedProperty";

            heartbeatTimer = new SharedPropertyManager<HeartbeatTimer>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(heartbeatTimer));
            heartbeatTimer.sharedMode = sharedMode;
            heartbeatTimer.Value = new HeartbeatTimer();
            heartbeatTimer.Value.LastBeatDataTime = DateTime.Now;

            idleLastUpdateTime = Time.realtimeSinceStartup;
        }

        public  void OnMqttMessageReceived(MqttMessage mqttMessage)
        {
            
            StartCoroutine(SomeClientObjectChanged(objectIdentifier.ObjectId));
        }

        private IEnumerator SomeClientObjectChanged(string name)
        {
            yield return new WaitForEndOfFrame();
            Debug.Log("<3 " +name + " gameobject: " + transform.name);
            idleLastUpdateTime = Time.realtimeSinceStartup;
        }

        private void OnTimeReceive(bool isSucceed, ulong time)
        {
            if (isSucceed)
            {
                Debug.Log(time);
                Debug.Log(UnbiasedTime.GetDateTime(time));
                heartbeatTimer.Value.UnbiasedTimeULong = time;
            }
            else
            {
                //No reliable time 
            }
        }

        void Update()
        {

            
            if ((sharedMode & SenderRecieverSharedMode.Sender) != 0)
            {

                if ((Time.realtimeSinceStartup > idleLastUpdateTime + heartbeatInterval + heartbeatDelay) && (heartbeatTimer.StatusFlag != Variable_Status.Sending))
                {
                    //Debug.Log("<3 over " + (Time.realtimeSinceStartup - idleLastUpdateTime));
                    GlobalEventManager.Instance.SendRefreshToAllSharedProperties();
                }
                if ((Time.realtimeSinceStartup > idleLastUpdateTime +heartbeatInterval) && (heartbeatTimer.StatusFlag != Variable_Status.Sending))
                {
                    heartbeatTimer.Value.LastBeatDataTime = DateTime.Now;
                    heartbeatTimer.StatusFlag = Variable_Status.Dirty;

                    //Debug.Log("<3 idle " + (Time.realtimeSinceStartup - idleLastUpdateTime));
                    heartbeatTimer.Update();
                }

            }
            else if ((sharedMode & SenderRecieverSharedMode.Receiver) !=0)
            {
                {
                    if (Time.realtimeSinceStartup > idleLastUpdateTime + heartbeatInterval + heartbeatDelay)
                    {
                        //Debug.Log("<3 kill " + idleLastUpdateTime);
                        idleLastUpdateTime = Time.realtimeSinceStartup;
                        ClientFactory.Instance.KillClient(gameObject);
                    }
                }
            }
        }

        private ObjectIdentifier EnsureObjectIdentifier()
        {
            var objectIdentifier = this.gameObject;
            return objectIdentifier.EnsureComponent<ObjectIdentifier>();
        }
    }
}

