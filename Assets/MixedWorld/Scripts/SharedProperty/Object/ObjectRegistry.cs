using MixedWorld.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ObjectRegistryList : SharedPropertyBase
{
    public HashSet<string> ObjectIds;
}
public class ObjectRegistry : Singleton<ObjectRegistry>
{
    private bool isInit = false;

    private SharedPropertyManager<ObjectRegistryList> objectRegistryList;

    private ObjectRegistryList localBufferObjectRegistryList;

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
            if(localBufferObjectRegistryList.ObjectIds.Count > 0)
            {
                objectRegistryList.Value.ObjectIds = MergeLists(localBufferObjectRegistryList.ObjectIds, objectRegistryList.Value.ObjectIds);
                objectRegistryList.Value.Status = Variable_Status.Dirty;
                localBufferObjectRegistryList.ObjectIds.Clear();
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Init();
    }

    void Init()
    {
        if (isInit) return;

        MainTopicBuilder.Instance.Maintopic = "Main";
        MainTopicBuilder.Instance.Subtopic = "SharedProperty";

        objectRegistryList = new SharedPropertyManager<ObjectRegistryList>("","GLOBAL", this.GetType().Name, nameof(objectRegistryList));
        objectRegistryList.Value = new ObjectRegistryList();

        objectRegistryList.Value.ObjectIds = new HashSet<string>();

        objectRegistryList.OnSharedPropertyUpdate += OnObjectRegistryChanged;

        localBufferObjectRegistryList = new ObjectRegistryList();
        localBufferObjectRegistryList.ObjectIds = new HashSet<string>();

        isInit = true;
    }

    public void AddObjectId(string objectId)
    {
        if (!isInit)
        {
            Init();
        }

        if (objectRegistryList.ConnectionEstablished)
        {
            int tmpCount = objectRegistryList.Value.ObjectIds.Count;

            if (localBufferObjectRegistryList.ObjectIds.Count > 0)
            {
                objectRegistryList.Value.ObjectIds = MergeLists(localBufferObjectRegistryList.ObjectIds, objectRegistryList.Value.ObjectIds);
                localBufferObjectRegistryList.ObjectIds.Clear();
            }

            objectRegistryList.Value.ObjectIds.Add(objectId);

            if (objectRegistryList.Value.ObjectIds.Count != tmpCount)
                objectRegistryList.Value.Status = Variable_Status.Dirty;
        }
        else
        {
            localBufferObjectRegistryList.ObjectIds.Add(objectId);
        }

    }

    public void RemoveObjectId(string objectId)
    {
        if (!isInit)
        {
            Init();
        }

        if (objectRegistryList.ConnectionEstablished)
        {
            int tmpCount = objectRegistryList.Value.ObjectIds.Count;

            if (localBufferObjectRegistryList.ObjectIds.Count > 0)
            {
                objectRegistryList.Value.ObjectIds = MergeLists(localBufferObjectRegistryList.ObjectIds, objectRegistryList.Value.ObjectIds);
                localBufferObjectRegistryList.ObjectIds.Clear();
            }

            objectRegistryList.Value.ObjectIds.Remove(objectId);
            if (objectRegistryList.Value.ObjectIds.Count != tmpCount)
                objectRegistryList.Value.Status = Variable_Status.Dirty;
        }
        else
        {
            localBufferObjectRegistryList.ObjectIds.Remove(objectId);
        }
    }

    private void Update()
    {
        if (objectRegistryList.IsConnected)
        {
            objectRegistryList.Update(1f);
        }
    }

    private HashSet<string> MergeLists(HashSet<string> A, HashSet<string> B)
    {
        HashSet<string> C = new HashSet<string>(A);
        C.UnionWith(B);
        return C;
    }

}
