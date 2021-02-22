using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnbiasedTimeManager;
using UnityEngine;
using MixedWorld.Sharing;

public class HelixMove : SharedPropertyBase
{
    private Variable<float> _radius;
    private Variable<float> _speed;
    public HelixMove()
    {
        _radius = new Variable<float>(l => Status = Variable_Status.Dirty);
        _speed = new Variable<float>(l => Status = Variable_Status.Dirty);
    }

    public float Radius
    {
        get { return _radius; }
        set { _radius.SetValue(value); }
    }
    
    public float Speed
    {
        get { return _speed; }
        set { _speed.SetValue(value); }
    }
}
[RequireComponent(typeof(ObjectIdentifier))]
[RequireComponent(typeof(ObjectDNA))]
public class ObjectBehaviourHelix : MonoBehaviour
{

    public float angle = 0;
    public float speed = 5; //2*PI in degress is 360, so you get 5 seconds to complete a circle
    public float radius = 1;

    public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

    private SharedPropertyManager<HelixMove> propertyBehaviour;
    private ObjectIdentifier objectIdentifier;
    private ObjectDNA objectDNA;

    float progress = 0;

    float globalTime = 0;
    

    void OnObjectChanged(object sender, SampleEventArgs e)
    {
        
            StartCoroutine(ObjectChanged(e.senderName.ToString()));
    }

    private IEnumerator ObjectChanged(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);

        speed = propertyBehaviour.Value.Speed;
        radius = propertyBehaviour.Value.Radius;
    }
    private void OnEnable()
    {
        objectIdentifier = EnsureObjectIdentifier();
        objectDNA = EnsureObjectDNA();

        objectDNA?.AddComponentItem(this.GetType().Name);

        UnbiasedTime.Instance.onTimeReceive += OnTimeReceive;
    }


    private void OnDisable()
    {
        objectDNA?.RemoveComponentItem(this.GetType().Name);

        UnbiasedTime.Instance.onTimeReceive -= OnTimeReceive;
    }
    // Start is called before the first frame update
    void Start()
    {

        propertyBehaviour = new SharedPropertyManager<HelixMove>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(propertyBehaviour));

        propertyBehaviour.OnSharedPropertyUpdate += OnObjectChanged;
        propertyBehaviour.isAllowedEcho = true; // <- dont use for smoothmoves
        propertyBehaviour.sharedMode = sharedMode;
        propertyBehaviour.Value = new HelixMove();

    }

    private void OnTimeReceive(bool isSucceed, ulong time)
    {
        if (isSucceed)
        {
            Debug.Log(time);
            Debug.Log(UnbiasedTime.GetDateTime(time));

            globalTime = (UnbiasedTime.GetDateTime(time).Millisecond * 0.001f) - Time.time;
        }
        else
        {
            //No reliable time 
        }
    }

    // Update is called once per frame
    void Update()
    {
        propertyBehaviour.sharedMode = sharedMode;

        angle += (speed == 0 ? 0 : ((2 * Mathf.PI) / speed)) * Time.deltaTime;
        transform.position = new Vector3(Mathf.Cos(angle) * radius, angle * 0.1f, Mathf.Sin(angle) * radius);

        if (Mathf.Abs(angle) > 360f)
        {
            angle += speed >= 0 ? 360f : -360f;
            speed *= -1f;
        }


        propertyBehaviour.Value.Speed = speed;
        propertyBehaviour.Value.Radius = radius;
        propertyBehaviour.Update(1f);


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
