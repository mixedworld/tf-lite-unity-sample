
using Microsoft.MixedReality.Toolkit;
using System;
using System.Collections;
using System.Collections.Generic;
using UnbiasedTimeManager;
using UnityEngine;
using MixedWorld.Sharing;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PhysicallyData
{
    private float _radius;
    private float _speed;
    public float timeOfLastChange;
    public float angleOfLastChange;
    public float speedOfLastChange;


    public float Radius
    {
        get { return _radius; }
        set { _radius = value; }
    }

    public float Speed
    {
        get { return _speed; }
        set { _speed = value; }
    }


}

public class GloabalTimeHandling : Singleton_<GloabalTimeHandling>
{
    ulong globalNetTime = 0;
    float lastCurrentTime = 0;
    bool isAppTimeReady = false;

    private Stopwatch timer;

    public GloabalTimeHandling()
    {
        timer = new Stopwatch();
        UnbiasedTime.Instance.onTimeReceive += OnTimeReceive;
    }

     ~GloabalTimeHandling()
    {
        UnbiasedTime.Instance.onTimeReceive -= OnTimeReceive;
    }

    private void OnTimeReceive(bool isSucceed, ulong time)
    {
        if (isSucceed)
        {
            //Debug.Log("Delta apptime: " + (((UnbiasedTime.GetDateTime(time).Millisecond * 0.001f) - Time.realtimeSinceStartup) - appStartedTime));
           
            globalNetTime = time - 3823164671925; ///24.02.2021 15:11
            isAppTimeReady = isSucceed;
            timer.Stop();
            timer.Reset();
            timer.Start();
        }
        else
        {
            //No reliable time 
        }
    }

    public float GetCurrentTime()
    {
        return isAppTimeReady ? (float)(globalNetTime + (ulong)timer.ElapsedMilliseconds) *0.001f : 0;
    }


    public void SetDeltaTimeBegin()
    {
        lastCurrentTime = GetCurrentTime();
    }

    public float GetDeltaTime()
    {
        return isAppTimeReady ? GetCurrentTime() - lastCurrentTime : 0;
    }

    public float OneCall_GetDeltaTime()
    {
        float res = GetDeltaTime();
        SetDeltaTimeBegin();
        return isAppTimeReady ? res : 0;
    }

}

[RequireComponent(typeof(ObjectIdentifier))]
public class SharedPhysicallyBehaviour : MonoBehaviour
{
    
    public float speed = 0.05f; //2*PI in degress is 360, so you get 5 seconds to complete a circle
    public float radius = 0.5f;

    public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

    private SharedPropertyManager<PhysicallyData> propertyBehaviour;
    private ObjectIdentifier objectIdentifier;




    void OnObjectChanged(object sender, SampleEventArgs e)
    {



        StartCoroutine(ObjectChanged(e.senderName.ToString()));
    }

    private IEnumerator ObjectChanged(string name)
    {
        yield return new WaitForEndOfFrame();

        speed = propertyBehaviour.Value.Speed;
        radius = propertyBehaviour.Value.Radius;

    }
    private void OnEnable()
    {
        objectIdentifier = EnsureObjectIdentifier();

       
    }


    private void OnDisable()
    {


    }
    // Start is called before the first frame update
    void Start()
    {


        propertyBehaviour = new SharedPropertyManager<PhysicallyData>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(propertyBehaviour));

        propertyBehaviour.OnSharedPropertyUpdate += OnObjectChanged;
        //propertyBehaviour.isAllowedEcho = true;
        propertyBehaviour.sharedMode = sharedMode;
        propertyBehaviour.Value = new PhysicallyData();
        propertyBehaviour.Value.Speed = speed;
        propertyBehaviour.Value.Radius = radius;
    }



    public float GetDistance(float velocity, float seconds)
    {
        return velocity * seconds;
    }

    // Update is called once per frame
    void Update()
    {

        float time = GloabalTimeHandling.Instance.GetCurrentTime();

        if (propertyBehaviour.Value.Speed != speed)
        {
            propertyBehaviour.Value.angleOfLastChange = propertyBehaviour.Value.angleOfLastChange + GetDistance((2 * Mathf.PI) * propertyBehaviour.Value.Speed, time - propertyBehaviour.Value.timeOfLastChange);
            propertyBehaviour.Value.timeOfLastChange = time;
        }

        float angle = propertyBehaviour.Value.angleOfLastChange + GetDistance((2 * Mathf.PI) * speed, time - propertyBehaviour.Value.timeOfLastChange);

        angle = angle % 360;

        transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

        if (propertyBehaviour.Value.Speed != speed || propertyBehaviour.Value.Radius != radius)
        {
            propertyBehaviour.Value.timeOfLastChange = time;
            propertyBehaviour.Value.angleOfLastChange = angle;
            propertyBehaviour.sharedMode = sharedMode;
            propertyBehaviour.Value.speedOfLastChange = propertyBehaviour.Value.Speed;
            propertyBehaviour.Value.Speed = speed;
            propertyBehaviour.Value.Radius = radius;
            propertyBehaviour.StatusFlag = Variable_Status.Dirty;

        }

        propertyBehaviour.Update(1f);
    }



    private ObjectIdentifier EnsureObjectIdentifier()
    {
        var objectIdentifier = this.gameObject;
        return objectIdentifier.EnsureComponent<ObjectIdentifier>();
    }

}
