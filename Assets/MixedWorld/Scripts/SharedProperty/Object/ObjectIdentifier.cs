using UnityEngine;
using MixedWorld.Core;
using MixedWorld.Utility;
using System.Collections;

namespace MixedWorld.Sharing
{
    public class ObjectIdentifier : MonoBehaviour
    {
        [SerializeField]
        private string seed = "";
        [SerializeField] private bool useNameAsSeed = false;
        public bool noSeedHashing = false;
        public bool globalSelfRegister = true;

        public bool isInitialized = false;
        public string rootId = "";

        public string Seed
        {
            get
            {
                return seed;
            }
            set
            {
                if (value == "")
                {
                    if (useNameAsSeed)
                    {
                        seed = gameObject.name;
                    }
                    else
                    {
                        seed = $"{Utilities.Timestamp()}";
                    }
                }
                else
                {
                    seed = value;
                }
                // Regenerate the mwID with new seed.
                if (noSeedHashing)
                {
                    ObjectId = seed;
                }
                else
                {
                    ObjectId = Utilities.SeededUID(seed.GetHashCode());
                }
            }
        }

        [SerializeField]
        private string objectId = "";

        [SerializeField]
        [HideInInspector]
        private string mwID = ""; // Editor display only.
        public string ObjectId
        {
            private set
            {
                if (objectId != value)
                {
                    objectId = value;
                    mwID = objectId;
                }
            }
            get
            {
                return objectId;
            }
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (useNameAsSeed)
            {
                seed = "";
            }
            Seed = seed;
            useNameAsSeed = false;
            if (objectId != mwID)
            {
                mwID = objectId;
            }
        }
    #endif

        private void OnDestroy()
        {
            ObjectRegistry.Instance?.RemoveObjectId(objectId);
        }

        IEnumerator Start()
        {
            // This is necessary so that we can modify the variable after adding this component to a gameobject.
            yield return new WaitForEndOfFrame();
            if (globalSelfRegister)
            {
                ObjectRegistry.Instance?.AddObjectId(objectId);
                isInitialized = true;
            }
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}