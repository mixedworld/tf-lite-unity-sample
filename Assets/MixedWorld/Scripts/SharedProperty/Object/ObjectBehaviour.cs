using Microsoft.MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedWorld.Sharing;


[RequireComponent(typeof(ObjectIdentifier))]
[RequireComponent(typeof(ObjectDNA))]
public class ObjectBehaviour : MonoBehaviour
{

    private SharedPropertyManager<ObjectIdentity> propertyBehaviour;
    private ObjectIdentifier objectIdentifier;
    private ObjectDNA objectDNA;

    void OnObjectChanged(object sender, SampleEventArgs e)
    {
        StartCoroutine(ObjectChanged(e.senderName.ToString()));
    }

    private IEnumerator ObjectChanged(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);
    }

    private void OnEnable()
    {
        objectIdentifier = EnsureObjectIdentifier();
        objectDNA = EnsureObjectDNA();

        objectDNA?.AddComponentItem(this.GetType().Name);
    }

    private void OnDisable()
    {
        objectDNA?.RemoveComponentItem(this.GetType().Name);
    }

    // Start is called before the first frame update
    void Start()
    {
        objectIdentifier = EnsureObjectIdentifier();

        propertyBehaviour = new SharedPropertyManager<ObjectIdentity>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(propertyBehaviour));

        propertyBehaviour.OnSharedPropertyUpdate += OnObjectChanged;
        propertyBehaviour.isAllowedEcho = true; // <- dont use for smoothmoves

        propertyBehaviour.Value = new ObjectIdentity();
        propertyBehaviour.Value.Components = new List<string>();

    }


    // Update is called once per frame
    void Update()
    {
        
    }

    private ObjectIdentifier EnsureObjectIdentifier()
    {
        var objectIdentifier = this.gameObject;
        return objectIdentifier.EnsureComponent<ObjectIdentifier>();
    }

    private ObjectDNA EnsureObjectDNA()
    {
        var objectDNA = this.gameObject;
        return objectDNA.EnsureComponent<ObjectDNA>();
    }

}
