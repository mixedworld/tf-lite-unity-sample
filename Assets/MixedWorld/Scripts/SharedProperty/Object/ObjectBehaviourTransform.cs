using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedWorld.Sharing;

public class TransformData
{
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 localScale;
}

[RequireComponent(typeof(ObjectIdentifier))]
[RequireComponent(typeof(ObjectDNA))]
public class ObjectBehaviourTransform : MonoBehaviour
{

    private SharedPropertyManager<TransformData> transformData;
    private ObjectIdentifier objectIdentifier;
    private ObjectDNA objectDNA;

    private Interpolator interpolator;

    private bool isSomeThingHappendWithMe = false;

    private bool isBeingManipulated = false;
    
    

    void OnObjectChanged(object sender, SampleEventArgs e)
    {
        StartCoroutine(ObjectChanged(e.senderName.ToString()));
    }

    private IEnumerator ObjectChanged(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);
        
        isSomeThingHappendWithMe = true;

        // update the placement to match the user's gaze.
        interpolator.SetTargetPosition(transformData.Value.position);

        // Rotate this object to face the user.
        interpolator.SetTargetRotation(transformData.Value.rotation);

        // Update objects local Scale.
        interpolator.SetTargetLocalScale(transformData.Value.localScale);
    }


    // Start is called before the first frame update
    void Start()
    {
        objectIdentifier = EnsureObjectIdentifier();

        transformData = new SharedPropertyManager<TransformData>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(transformData));

        interpolator = EnsureInterpolator();

        transformData.Value = new TransformData();

        transformData.Value.position = new Vector3();
        transformData.Value.rotation = new Quaternion();
        transformData.Value.localScale = new Vector3();

        transformData.Value.position = this.transform.position;
        transformData.Value.rotation = this.transform.rotation;
        transformData.Value.localScale = this.transform.localScale;
        isBeingManipulated = false;

        transformData.OnSharedPropertyUpdate += OnObjectChanged;
    }


    // Update is called once per frame
    void Update()
    {
        if (isBeingManipulated)
        {
            //this is usually sends by a hololens

            //my own movment
            /// just send status
            /// 

            transformData.Value.position = interpolator.TargetPosition;
            transformData.Value.rotation = interpolator.TargetRotation;
            transformData.Value.localScale = interpolator.TargetLocalScale;

            transformData.StatusFlag = Variable_Status.Dirty;

            // rb.isKinematic = true;
            isSomeThingHappendWithMe = true;

        }

        else if (transformData.Value.position != this.transform.position || transformData.Value.rotation != this.transform.rotation || transformData.Value.localScale != this.transform.localScale)
        {
            if (interpolator.Running)
            {
                // this is an action of my own
                //Debug.Log("interpolation is detected");
                // update the placement to match the user's gaze.
                interpolator.SetTargetPosition(transformData.Value.position);

                // Rotate this object to face the user.
                interpolator.SetTargetRotation(transformData.Value.rotation);
                //rb.isKinematic = true;

                interpolator.SetTargetLocalScale(transformData.Value.localScale);
            }
            else
            {
                //this is usually sends by an editor
                Debug.Log("editor movment is detected");
                interpolator.Reset();
                transformData.Value.position = this.transform.position;
                transformData.Value.rotation = this.transform.rotation;
                transformData.Value.localScale = this.transform.localScale;

                transformData.StatusFlag = Variable_Status.Dirty;
            }


            isSomeThingHappendWithMe = true;
        }
        else if (isSomeThingHappendWithMe)
        {
            // Debug.Log("Something happend with me");
            // all is ready, but something happend with me
            isSomeThingHappendWithMe = false;

        }

        transformData.Update();
    }

    void OnManipulationStarted(ManipulationEventData eventData)
    {
        isBeingManipulated = true;
    }
    void OnManipulationEnded(ManipulationEventData eventData)
    {
        isBeingManipulated = false;
    }

    private void OnEnable()
    {
        objectIdentifier = EnsureObjectIdentifier();
        objectDNA = EnsureObjectDNA();

        objectDNA?.AddComponentItem(this.GetType().Name);

        ManipulationHandler mh = this.gameObject.GetComponent<ManipulationHandler>();
        if (mh)
        {
            mh.OnManipulationStarted.AddListener(OnManipulationStarted);
            mh.OnManipulationEnded.AddListener(OnManipulationEnded);
        }

    }
    private void OnDisable()
    {
        objectDNA?.RemoveComponentItem(this.GetType().Name);

        this.gameObject.GetComponent<ManipulationHandler>()?.OnManipulationStarted.RemoveListener(OnManipulationStarted);
        this.gameObject.GetComponent<ManipulationHandler>()?.OnManipulationEnded.RemoveListener(OnManipulationEnded);

    }

    private Interpolator EnsureInterpolator()
    {
        var interpolatorHolder = this.gameObject;
        return interpolatorHolder.EnsureComponent<Interpolator>();
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
