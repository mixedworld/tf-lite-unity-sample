using Microsoft.MixedReality.Toolkit;
using MixedWorld.Mqtt;
using System;
using System.Collections;
using System.Collections.Generic;
using UnbiasedTimeManager;
using UnityEngine;



namespace MixedWorld.Sharing
{

    public class PrefabType : SharedPropertyBase
    {
        public string name;
    }
    [RequireComponent(typeof(ObjectIdentifier))]
    public class SharedBehaviourString : MonoBehaviour
    {
        
        public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;
        private ObjectIdentifier objectIdentifier;
        private SharedPropertyManager<PrefabType> prefabType;
        public string prefabName;


        // Start is called before the first frame update
        IEnumerator Start()
        {
            objectIdentifier = EnsureObjectIdentifier();
            yield return new WaitUntil(() => { return objectIdentifier.isInitialized; });

            prefabType = new SharedPropertyManager<PrefabType>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(prefabType));
            prefabType.sharedMode = sharedMode;
            prefabType.OnSharedPropertyUpdate += OnMqttMessageReceived;
            prefabType.Value = new PrefabType();
            prefabType.Value.name = prefabName;

        }

        private void Update()
        {
            if (prefabType == null)
            {
                return;
            }

            prefabType.Value.name = prefabName;
            prefabType.Update();
        }

        public void OnMqttMessageReceived(object sender, SampleEventArgs e)
        {
            StartCoroutine(SomeClientObjectChanged());
        }

        private IEnumerator SomeClientObjectChanged()
        {
            yield return new WaitForEndOfFrame();
            
            GameObject instance = Instantiate(Resources.Load(prefabType.Value.name, typeof(GameObject))) as GameObject;
            instance.transform.SetParent(transform.parent, false);
            var oi = instance.GetComponent<ObjectIdentifier>();
            var rootOi = GetComponent<ObjectIdentifier>();
            oi.rootId = rootOi.rootId;
            oi.noSeedHashing = true;
            oi.Seed = rootOi.ObjectId;
            oi.isInitialized = true;
            Destroy(this.gameObject);
        }


        private ObjectIdentifier EnsureObjectIdentifier()
        {
            var objectIdentifier = this.gameObject;
            return objectIdentifier.EnsureComponent<ObjectIdentifier>();
        }
    }
}

