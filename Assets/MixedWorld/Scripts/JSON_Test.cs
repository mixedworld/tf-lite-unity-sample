using MixedWorld.Mqtt;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSON_Test : MonoBehaviour
{
    
    public SharedPropertyManager<ObjectIdentity> propertyA;
    public SharedPropertyManager<ObjectIdentity> propertyB;
    public SharedPropertyManager<ObjectIdentityList> propertyList;


    ObjectIdentity acc = new ObjectIdentity();
    ObjectIdentity act = new ObjectIdentity();

    void OnSharedPropertyUpdate(object sender, SampleEventArgs e)
    {
        StartCoroutine(SpawnAvatar(e.senderName.ToString()));
    }

    private IEnumerator SpawnAvatar(string name)
    {
        yield return new WaitForEndOfFrame();
        Debug.LogFormat("Property changed by {0}", name);
    }

    // Start is called before the first frame update
    void Start()
    {
        MainTopicBuilder.Instance.Maintopic = "Main";
        MainTopicBuilder.Instance.Subtopic = "SharedProperty";

        propertyA = new SharedPropertyManager<ObjectIdentity>("","None", this.GetType().Name, nameof(propertyA));
        propertyB = new SharedPropertyManager<ObjectIdentity>("","None", this.GetType().Name, nameof(propertyB));
        propertyList = new SharedPropertyManager<ObjectIdentityList>("","None", this.GetType().Name, nameof(propertyList));

        propertyA.OnSharedPropertyUpdate += OnSharedPropertyUpdate;
        //propertyA.isAllowedEcho = true;

        propertyA.Value = new ObjectIdentity();
        propertyB.Value = acc;

        acc.ID = "12345";

        propertyA.Value.Components = new List<string> { "a", "b", "c" };
        acc.Components = new List<string> { "x", "y", "z" };

        propertyList.Value = new ObjectIdentityList();
        propertyList.Value.ObjectIdentities = new List<ObjectIdentity>();
        propertyList.Value.ObjectIdentities.Add(acc);
        propertyList.Value.ObjectIdentities.Add(propertyB.Value);
    }
    
    // Update is called once per frame
    void Update()
    {
        if (propertyA.IsConnected && propertyB.IsConnected && propertyList.IsConnected)
        {

            propertyA.Value.ID = "1234";
            propertyA.Value.CreatedDate = DateTime.Now;
            propertyA.Update();

            acc.ID = "56789";
            acc.CreatedDate = DateTime.Now;
            propertyB.Value = acc;

            propertyB.Update();

            propertyList.Update();

        }
    }
}
