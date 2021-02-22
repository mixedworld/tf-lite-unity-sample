using MixedWorld.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Sharing
{

    public class ObjectClientList
    {
        public HashSet<string> ClientIds;
    }
    public class ClientRegistry : Singleton<ClientRegistry>
    {
        private bool isInit = false;

        public SharedPropertyManager<ObjectClientList> objectClientList;

        private ObjectClientList localBufferObjectClientList;



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
                ClientFactory.Instance.UpdateAllClients();
            }
        }

        void Start()
        {
            Init();
        }

        void Init()
        {
            if (isInit) return;

            MainTopicBuilder.Instance.Maintopic = "Main";
            MainTopicBuilder.Instance.Subtopic = "SharedProperty";

            objectClientList = new SharedPropertyManager<ObjectClientList>("","GLOBAL", this.GetType().Name, nameof(objectClientList));
            objectClientList.Value = new ObjectClientList();

            objectClientList.Value.ClientIds = new HashSet<string>();

            objectClientList.OnSharedPropertyUpdate += OnObjectRegistryChanged;

            localBufferObjectClientList = new ObjectClientList();
            localBufferObjectClientList.ClientIds = new HashSet<string>();

            isInit = true;
        }

        public void AddClientId(string objectId)
        {
            if (!isInit)
            {
                Init();
            }

            if (objectClientList.ConnectionEstablished)
            {
                int tmpCount = objectClientList.Value.ClientIds.Count;
                ResyncBuffer();
                objectClientList.Value.ClientIds.Add(objectId);
                objectClientList.StatusFlag = Variable_Status.Dirty;
            }
            else
            {
                localBufferObjectClientList.ClientIds.Add(objectId);
                objectClientList.StatusFlag = Variable_Status.Dirty;
            }

        }

        public void RemoveClientId(string objectId)
        {
            if (!isInit)
            {
                Init();
            }

            if (objectClientList.ConnectionEstablished)
            {
                int tmpCount = objectClientList.Value.ClientIds.Count;
                ResyncBuffer();
                bool success = objectClientList.Value.ClientIds.Remove(objectId);
                //Debug.Log($"Client {objectId} removed from the list {success}");
                objectClientList.StatusFlag = Variable_Status.Dirty;
            }
            else
            {
                localBufferObjectClientList.ClientIds.Remove(objectId);
                objectClientList.StatusFlag = Variable_Status.Dirty;
            }
        }

        private void ResyncBuffer()
        {
            if (localBufferObjectClientList.ClientIds.Count > 0)
            {
                objectClientList.Value.ClientIds = MergeLists(localBufferObjectClientList.ClientIds, objectClientList.Value.ClientIds);
                localBufferObjectClientList.ClientIds.Clear();
                objectClientList.StatusFlag = Variable_Status.Dirty;
            }
        }

        private void Update()
        {
            if (objectClientList.ConnectionEstablished)
            {
                ResyncBuffer();
            }
            if (objectClientList.IsConnected)
            {
                objectClientList.Update();
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
