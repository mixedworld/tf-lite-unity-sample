using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedWorld.Sharing;
using MixedWorld.Util;

public class HandJoints
{
    private Vector3 [] joints;
    public HandJoints()
    {
        joints = new Vector3[21];
    }

    public Vector3[] Joints
    {
        get { return joints; }
        set { joints = value; }
    }

}
[RequireComponent(typeof(ObjectIdentifier))]
public class SharedBehaviourHand : MonoBehaviour
{
    public MWTrackableHand hand = null;
    public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

    private SharedPropertyManager<HandJoints> sharedJoints;
    private ObjectIdentifier objectIdentifier;
    
    void OnObjectChanged(object sender, SampleEventArgs e)
    {
        
            StartCoroutine(ObjectChanged(e.senderName.ToString()));
    }

    private IEnumerator ObjectChanged(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);

        //copy to sensor
        if (hand != null)
        {
            hand.UpdateJoints((Vector3[])sharedJoints.Value.Joints.Clone(), true);
        }
    }
    private void OnEnable()
    {
        objectIdentifier = EnsureObjectIdentifier();
    }


    private void OnDisable()
    {
    }
    // Start is called before the first frame update
    IEnumerator Start()
    {
        MainTopicBuilder.Instance.Maintopic = "Main";
        MainTopicBuilder.Instance.Subtopic = "SharedProperty";

        objectIdentifier = EnsureObjectIdentifier();

        yield return new WaitUntil(() => { return objectIdentifier.isInitialized; });
 
        sharedJoints = new SharedPropertyManager<HandJoints>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(sharedJoints));

        sharedJoints.OnSharedPropertyUpdate += OnObjectChanged;
        sharedJoints.sharedMode = sharedMode;
        sharedJoints.Value = new HandJoints();

    }


    // Update is called once per frame
    void Update()
    {
        if (sharedJoints == null)
        {
            return;
        }
        sharedJoints.sharedMode = sharedMode;

        
        if(hand != null && hand.isDirty)
        {
            sharedJoints.Value.Joints = hand.SharedJoints;
            hand.isDirty = false;
            sharedJoints.StatusFlag = Variable_Status.Dirty;
        }
        sharedJoints.Update();

    }

    private ObjectIdentifier EnsureObjectIdentifier()
    {
        var objectIdentifier = this.gameObject;
        return objectIdentifier.EnsureComponent<ObjectIdentifier>();
    }

}
