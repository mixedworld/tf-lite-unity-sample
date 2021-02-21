using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using MixedWorld.Mqtt;
using MixedWorld.Utility;

namespace MixedWorld.Sharing
{
    [DisallowMultipleComponent]
    public class ClientFactory : Singleton<ClientFactory>
    {
        [SerializeField]
        private Transform SceneRoot = null;
        private ObjectIdentifier mySelf = null;
        void Start()
        {
            var go = new GameObject();
            // If a Scene Root exists, parent this under it.
            if (SceneRoot != null)
            {
                go.transform.SetParent(SceneRoot, false);
            }
            mySelf = go.AddComponent<ObjectIdentifier>();
            mySelf.Seed = MqttManager.Instance.HashedClientId.ToString();
            var sbt = go.AddComponent<SharedBehaviourTransform>();
            //Explicitly set this behaviour to sender because its ours.
            sbt.SharedMode = SenderRecieverSharedMode.Sender;
            // Add SharedObjectRegistry
            var sor = go.AddComponent<SharedObjectRegistry>();
            sor.rootTopic = mySelf.ObjectId;
            sor.sharedMode = SenderRecieverSharedMode.Sender;
            sor.Init();
            // Add the Heartbeat
            var heartbeat = go.AddComponent<SharedBehaviourHeartbeat>();
            heartbeat.sharedMode = SenderRecieverSharedMode.Sender;
            // Add our cliend ID (hashed as ObjectId) to the phonebook.
            go.name = mySelf.ObjectId + "_Root";
            ClientRegistry.Instance.AddClientId(mySelf.ObjectId);
        }

        public void UpdateAllClients()
        {
            bool iAmOnTheGuestList = false;
            foreach (var objectId in ClientRegistry.Instance.objectClientList.Value.ClientIds)
            {
                if (mySelf.ObjectId == objectId)
                {
                    iAmOnTheGuestList = true;
                    continue;
                }
                GameObject clientGO = GameObject.Find(objectId + "_Root");
                if (clientGO == null)
                {
                    CreateClient(objectId);
                }
            }
            if (!iAmOnTheGuestList)
            {
                ClientRegistry.Instance.AddClientId(mySelf.ObjectId);
                GlobalEventManager.Instance.SendRefreshToAllSharedProperties();
            }
        }

        public void CreateClient(string name)
        {
            var go = new GameObject(name + "_Root");
            // If a Scene Root exists, parent this under it.
            if (SceneRoot != null)
            {
                go.transform.SetParent(SceneRoot, false);
            }

            var oi = go.AddComponent<ObjectIdentifier>();
            // This must be set sothat the seed and object ID are identical. Otherwise client removal will fail.
            oi.noSeedHashing = true;
            oi.Seed = name;
            // Add Heartbeat
            // Add other SharedBehaviours.
            var sbt = go.AddComponent<SharedBehaviourTransform>();
            //Explicitly set this behaviour to receiver because its theirs.
            sbt.SharedMode = SenderRecieverSharedMode.Receiver;
            // Add SharedObjectRegistry for remote client root
            var sor = go.AddComponent<SharedObjectRegistry>();
            sor.rootTopic = name;
            sor.sharedMode = SenderRecieverSharedMode.Receiver;
            sor.Init();
            // Add the Heartbeat
            var heartbeat = go.AddComponent<SharedBehaviourHeartbeat>();
            heartbeat.sharedMode = SenderRecieverSharedMode.Receiver;
            // Here we should add a prefab containing all the components that a client might need.
        }

        public void KillClient(GameObject client)
        {
            ObjectIdentifier oi = client.GetComponent<ObjectIdentifier>();
            ClientRegistry.Instance.RemoveClientId(oi.ObjectId);
            client.SetActive(false);
            Destroy(client);
        }

        /// <summary>
        /// Only the local Prefabs can register themselves.
        /// </summary>
        /// <param name="oi"></param>
        /// <returns></returns>
        public IEnumerator RegisterPrefab(ObjectIdentifier oi)
        {
            yield return new WaitUntil(()=> { return mySelf != null; });
            Debug.Log("RegisterPrefab was called with " + oi.ObjectId);
            mySelf.GetComponent<SharedObjectRegistry>().AddSharedObjectId(oi.ObjectId);
            oi.rootId = mySelf.ObjectId;
            oi.isInitialized = true;
        }

        /// <summary>
        /// This is for updating and creating remote prefab containers.
        /// </summary>
        public void UpdatePrefabContainers(SharedObjectRegistry clientSor)
        {
            foreach (var objectId in clientSor.objectList.Value.objectIds)
            {
                if (clientSor.transform.Find(objectId) == null)
                {
                    CreatePrefabContainer(objectId, clientSor.transform);
                }
            }
        }
        // The Factory Part for Remote Client Prefabs :
        public void CreatePrefabContainer(string objectId, Transform root)
        {
            var go = new GameObject(objectId);
            go.transform.SetParent(root, false);
            var oi = go.AddComponent<ObjectIdentifier>();
            oi.globalSelfRegister = false;
            // This must be set sothat the seed and object ID are identical. Otherwise client removal will fail.
            oi.noSeedHashing = true;
            oi.Seed = objectId;
            oi.rootId = root.GetComponent<ObjectIdentifier>().ObjectId;
            oi.isInitialized = true;
            // String Shared Behaviour
            var sbs = go.AddComponent<SharedBehaviourString>();
            sbs.sharedMode = SenderRecieverSharedMode.Receiver;

        }
    }
}
