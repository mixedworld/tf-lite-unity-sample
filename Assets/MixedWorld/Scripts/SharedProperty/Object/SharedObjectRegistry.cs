using MixedWorld.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Sharing
{
    public class ObjectList
    {
        public HashSet<string> objectIds;
    }
    public class SharedObjectRegistry : MonoBehaviour
    {
        private bool isInit = false;

        public SharedPropertyManager<ObjectList> objectList;
        public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;
        public string rootTopic = "GLOBAL";
        private ObjectList localBufferObjectList;

        void OnObjectRegistryChanged(object sender, SampleEventArgs e)
        {
            StartCoroutine(ObjectRegistryChanged(e.senderName.ToString()));
        }

        private IEnumerator ObjectRegistryChanged(string name)
        {
            yield return new WaitForEndOfFrame();
            Debug.LogFormat("Property changed by {0}", name);

            if (isInit)
            {
                ResyncBuffer();
                ClientFactory.Instance.UpdatePrefabContainers(this);
            }
            
            // Factory Mode:
            
            // Go through the list and create all new sensors.
        }

        public void Init()
        {
            if (isInit) return;

            objectList = new SharedPropertyManager<ObjectList>("", this.rootTopic, this.GetType().Name, nameof(objectList));
            objectList.Value = new ObjectList();
            objectList.sharedMode = sharedMode;
            objectList.Value.objectIds = new HashSet<string>();

            objectList.OnSharedPropertyUpdate += OnObjectRegistryChanged;

            localBufferObjectList = new ObjectList();
            localBufferObjectList.objectIds = new HashSet<string>();

            isInit = true;
        }

        public void AddSharedObjectId(string objectId)
        {
            if (!isInit)
            {
                Init();
            }

            if (objectList.ConnectionEstablished)
            {
                int tmpCount = objectList.Value.objectIds.Count;
                ResyncBuffer();
                objectList.Value.objectIds.Add(objectId);
                objectList.StatusFlag = Variable_Status.Dirty;
            }
            else
            {
                localBufferObjectList.objectIds.Add(objectId);
                objectList.StatusFlag = Variable_Status.Dirty;
            }

        }

        public void RemoveObjectId(string objectId)
        {
            if (!isInit)
            {
                Init();
            }

            if (objectList.ConnectionEstablished)
            {
                int tmpCount = objectList.Value.objectIds.Count;
                ResyncBuffer();
                bool success = objectList.Value.objectIds.Remove(objectId);
                objectList.StatusFlag = Variable_Status.Dirty;
            }
            else
            {
                localBufferObjectList.objectIds.Remove(objectId);
                objectList.StatusFlag = Variable_Status.Dirty;
            }
        }

        private void ResyncBuffer()
        {
            if (localBufferObjectList.objectIds.Count > 0)
            {
                objectList.Value.objectIds = MergeLists(localBufferObjectList.objectIds, objectList.Value.objectIds);
                localBufferObjectList.objectIds.Clear();
                objectList.StatusFlag = Variable_Status.Dirty;
            }
        }

        private void Update()
        {
            if (objectList.ConnectionEstablished)
            {
                ResyncBuffer();
            }
            if (objectList.IsConnected)
            {
                objectList.Update();
            }
        }

        private HashSet<string> MergeLists(HashSet<string> A, HashSet<string> B)
        {
            HashSet<string> C = new HashSet<string>(A);
            C.UnionWith(B);
            return C;
        }

    }
}
