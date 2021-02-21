using Microsoft.MixedReality.Toolkit;
using MixedWorld.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedWorld.Sharing;



public class ComponentList : SharedPropertyBase
{
    public HashSet<string> components;
}

[RequireComponent(typeof(ObjectIdentifier))]
public class ObjectDNA : MonoBehaviour
{
    private bool isInit = false;

    private SharedPropertyManager<ComponentList> componentList;

    private ComponentList localBufferComponentList;

    private ObjectIdentifier objectIdentifier;

    void OnComponentListChanged(object sender, SampleEventArgs e)
    {
        StartCoroutine(ComponentListChanged(e.senderName.ToString()));
    }

    private IEnumerator ComponentListChanged(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);

        if (isInit)
        {
            //get current components
            HashSet<string> tmpList = new HashSet<string>();
            foreach (var component in GetComponents<Component>())
            {
                tmpList.Add(component.GetType().Name);
            }

            //more current component then shared? -> update shared
            foreach (var component in tmpList)
            {
                if (!componentList.Value.components.Contains(component))
                {
                    componentList.Value.Status = Variable_Status.Dirty;
                    break;
                }
            }

            //make a big list of all components
            localBufferComponentList.components = MergeLists(localBufferComponentList.components, tmpList);
            componentList.Value.components = MergeLists(localBufferComponentList.components, componentList.Value.components);
            localBufferComponentList.components.Clear();

            //add all new components form the shared part
            foreach (var component in componentList.Value.components)
            {
                if (!tmpList.Contains(component))
                {
                    Debug.LogFormat("Should add component {0}", component.ToString());
                    gameObject.AddComponent(Type.GetType(component));
                }
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

        objectIdentifier = EnsureObjectIdentifier();

        ObjectRegistry.Instance.AddObjectId(objectIdentifier.ObjectId);
        
        componentList = new SharedPropertyManager<ComponentList>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(componentList));
        componentList.Value = new ComponentList();
        componentList.isAllowedEcho = true;

        componentList.Value.components = new HashSet<string>();

        componentList.OnSharedPropertyUpdate += OnComponentListChanged;

        localBufferComponentList = new ComponentList();
        localBufferComponentList.components = new HashSet<string>();

        isInit = true;
        foreach (var component in GetComponents<Component>())
        {
            AddComponentItem(component.GetType().Name);
        }


        
    }

    public void AddComponentItem(string objectId)
    {
        if (!isInit)
        {
            Init();
        }

        if (componentList.ConnectionEstablished)
        {
            int tmpCount = componentList.Value.components.Count;

            if (localBufferComponentList.components.Count > 0)
            {
                componentList.Value.components = MergeLists(localBufferComponentList.components, componentList.Value.components);
                localBufferComponentList.components.Clear();
            }

            componentList.Value.components.Add(objectId);

            if (componentList.Value.components.Count != tmpCount)
                componentList.Value.Status = Variable_Status.Dirty;
        }
        else
        {
            localBufferComponentList.components.Add(objectId);
        }

     }

     public void RemoveComponentItem(string objectId)
     {

        if (!isInit)
        {
            Init();
        }

        if (componentList.ConnectionEstablished)
        {
            if (localBufferComponentList.components.Count > 0)
            {
                componentList.Value.components = MergeLists(localBufferComponentList.components, componentList.Value.components);
                localBufferComponentList.components.Clear();
            }

            componentList.Value.components.Remove(objectId);
            componentList.Value.Status = Variable_Status.Dirty;
        }
        else
        {
            localBufferComponentList.components.Remove(objectId);
        }
    }

    private void Update()
    {
        if (componentList.IsConnected)
        {
            componentList.Update(1f);
        }
    }

    private void OnDestroy()
    {
        ObjectRegistry.Instance?.RemoveObjectId(objectIdentifier?.ObjectId);
    }


    private ObjectIdentifier EnsureObjectIdentifier()
    {
        var objectIdentifier = this.gameObject;
        return objectIdentifier.EnsureComponent<ObjectIdentifier>();
    }

    private HashSet<string> MergeLists(HashSet<string> A, HashSet<string> B)
    {
        HashSet<string> C = new HashSet<string>(A);
        C.UnionWith(B);
        return C;
    }

}
