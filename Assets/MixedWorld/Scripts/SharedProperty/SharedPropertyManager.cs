using MixedWorld.Mqtt;
using MixedWorld.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

[Flags]
public enum Variable_Status
{
    None = 0b_0000_0000,  // 0
    New = 0b_0000_0001,  // 1
    Sending = 0b_0000_0010,  // 2
    Dirty = 0b_0000_0100,  // 4
    Sended = 0b_0000_1000,  // 8
    Recvied = 0b_0001_0000,  // 16
    Refresh = 0b_0010_0000,  // 32
    //Sunday = 0b_0100_0000,  // 64
    //Weekend = Saturday | Sunday
}

[Flags]
public enum SenderRecieverSharedMode
{
    Sender = 0b_0000_0001,  // 1
    Receiver = 0b_0000_0010,  // 2
    Both = Receiver | Sender,  // 4

}
public class Singleton_<T> where T : new()
{
    private static readonly Lazy<T>
        lazy =
        new Lazy<T>
            (() => new T());

    public static T Instance { get { return lazy.Value; } }
}
public class MainTopicBuilder : Singleton_<MainTopicBuilder>
{

    /// /maintopic/subtopic/object/propertyId
    /// 
    private string _Maintopic;
    private string _Subtopic;
    public string Maintopic
    {
        get { return _Maintopic; }
        set { _Maintopic = value; }
    }
    public string Subtopic
    {
        get { return _Subtopic; }
        set { _Subtopic = value; }
    }
    public string GetMainTopic()
    {
        return Maintopic + "/" + Subtopic;
    }
}

public class PropertyTopicBuilder
{
    /// /maintopic/subtopic/object/propertyId
    private string _ObjectName;
    private string _ClassName;
    private string _PorpertyName;
    public string ObjectName
    {
        get { return _ObjectName; }
        set { _ObjectName = value; }
    }
    public string ClassName
    {
        get { return _ClassName; }
        set { _ClassName = value; }
    }
    public string PorpertyName
    {
        get { return _PorpertyName; }
        set { _PorpertyName = value; }
    }
    public string GetFullTopic()
    {
        return MainTopicBuilder.Instance.GetMainTopic() + "/" + ObjectName + "/" + ClassName + "/" + PorpertyName;
    }
}

public class Variable<T>
{
    private T _value;
    private readonly Action<T> _onValueChangedCallback;

    public Variable(Action<T> onValueChangedCallback, T value = default)
    {
        _value = value;
        _onValueChangedCallback = onValueChangedCallback;
    }

    public void SetValue(T value)
    {
        if (!EqualityComparer<T>.Default.Equals(_value, value))
        {
            _value = value;
            _onValueChangedCallback?.Invoke(value);
        }
    }

    public T GetValue()
    {
        return _value;
    }

    public static implicit operator T(Variable<T> variable)
    {
        return variable.GetValue();
    }
}



public class SharedPropertyManager<T> : ConnectionBase
{

    //TODO
    //Objectname, Topic, QoS, Retain, Frequency, ID

    public string className;
    public string objectName;

    public bool isAllowedEcho = false;
    public SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

    private bool useGlobalRefreshRateHz = true;
    private float refreshRateHz = 5f;

    private float idleUntilTime = 0f;
    

    private PropertyTopicBuilder propertyTopicBuilder = null;

    public event EventHandler<SampleEventArgs> OnSharedPropertyUpdate;

    private Variable_Status _Status = Variable_Status.New;
    public Variable_Status StatusFlag
    {
        get { return _Status; }
        set { _Status = value; }
    }

    // *** Locking ***
    private object m_ValueLock;

    // *** Value buffer ***
    private Variable<T> m_Value;

    // *** Access to value ***
    internal T Value
    {
        get
        {
            lock (m_ValueLock)
            {
                return m_Value;
            }
        }
        set
        {
            lock (m_ValueLock)
            {
                m_Value.SetValue(value);
            }
        }
    }

    // ***********************
    // *** Con-/Destructor ***
    // ***********************

    ~SharedPropertyManager()
    {
        GlobalEventManager.Instance.OnRefreshAllSharedPropertiesListener -= OnRefreshValue;
        Disconnect();
    }

    internal SharedPropertyManager(string rootTopic, string objectName, string className, string propertyName)
    {
        string combinedTopic = objectName;
        if (rootTopic != "")
        {
            combinedTopic = rootTopic + "/" + objectName;
        }
        m_ValueLock = new object();
        m_Value = new Variable<T>(l => StatusFlag = Variable_Status.New);
        SetObjectIdAndPropertyId(combinedTopic, className, propertyName);
        Init();
    }

    internal SharedPropertyManager(string rootTopic, string objectName, string className, string propertyName, T value)
    {
        string combinedTopic = objectName;
        if (rootTopic != "")
        {
            combinedTopic = rootTopic + "/" + objectName;
        }
        m_ValueLock = new object();
        m_Value = new Variable<T>(l => StatusFlag = Variable_Status.New, value);
        SetObjectIdAndPropertyId(combinedTopic, className, propertyName);
        Value = value;
        Init();
    }

    internal SharedPropertyManager(string rootTopic, string objectName, string className, string propertyName, T value, object Lock)
    {
        string combinedTopic = objectName;
        if (rootTopic != "")
        {
            combinedTopic = rootTopic + "/" + objectName;
        }
        m_ValueLock = Lock;
        m_Value = new Variable<T>(l => StatusFlag = Variable_Status.New, value);
        SetObjectIdAndPropertyId(combinedTopic, className, propertyName);
        Value = value;
        Init();
    }

    // ********************************
    // *** Type casting overloading ***
    // ********************************
    public static implicit operator T(SharedPropertyManager<T> value)
    {
        return value.Value;
    }


    void Init()
    {
        className = nameof(SharedPropertyManager<T>);
        propertyTopicBuilder = new PropertyTopicBuilder();
        Connect();
        GlobalEventManager.Instance.OnRefreshAllSharedPropertiesListener += OnRefreshValue;
    }

    private void OnRefreshValue(object sender, SampleEventArgs e)
    {
        StatusFlag = Variable_Status.Refresh;
        Update();
    }



    private void SetObjectIdAndPropertyId(string objectName, string className, string propertyName)
    {
        if(propertyTopicBuilder == null) propertyTopicBuilder = new PropertyTopicBuilder();
        propertyTopicBuilder.ObjectName = objectName;
        propertyTopicBuilder.ClassName = className;
        propertyTopicBuilder.PorpertyName = propertyName;
        fullTopic = propertyTopicBuilder.GetFullTopic();
    }


    public void Update()
    {

        if (!ConnectionEstablished)
        {
            RecievedAllRetainMsg();
            return;
        }

        if (sharedMode == SenderRecieverSharedMode.Receiver) return;

        if (StatusFlag == Variable_Status.Dirty || StatusFlag == Variable_Status.Refresh)
        {
            UpdateProperty();
        }
        else
        {
            return;
        }

    }

    public void Update(float idleTime)
    {
        if (sharedMode == SenderRecieverSharedMode.Receiver) return;

        //idleUpdateCounter += UnityEngine.Time.deltaTime;
        {

            if (idleUntilTime < UnityEngine.Time.time || StatusFlag == Variable_Status.Refresh)
            {
                if (!ConnectionEstablished)
                {
                    RecievedAllRetainMsg();
                    return;
                }
                if (StatusFlag == Variable_Status.Dirty || StatusFlag == Variable_Status.Refresh)
                {
                    UpdateProperty();
                }
                else
                {
                    return;
                }
                idleUntilTime = UnityEngine.Time.time + idleTime;
            }
        }
    }

    private void UpdateProperty()
    {
        //string jsonValue = JsonConvert.SerializeObject(Value, Formatting.None, new JsonSerializerSettings
        //{

        //    ContractResolver = new CamelCasePropertyNamesContractResolver()
        //});

        
        StatusFlag = Variable_Status.Sending;


        var settings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.None
        };
        settings.Converters.Add(new StringEnumConverter());
        var jsonValue = JsonConvert.SerializeObject(Value, settings);


        JObject obj = new JObject();
        obj["Meta"] = new JObject(
            new JProperty("id", ClientId.ToString()),
            new JProperty("echo", isAllowedEcho));
        obj["Data"] = new JObject(new JProperty("Value", jsonValue));
        SendString(obj.ToString());
    }

    public override void OnMqttMessageReceived(MqttMessage mqttMessage)
    {
        JObject obj = new JObject();
        obj = JObject.Parse(Encoding.UTF8.GetString(mqttMessage.Payload));

        JObject jsonMeta = obj.Value<JObject>("Meta");
        if (jsonMeta != null)
        {
            if (jsonMeta.Value<string>("id").Equals(ClientId.ToString()))
            {

                StatusFlag = Variable_Status.Sended;
                //my own message
                //has been ignored
                if (acceptOwnRetainedMsg)
                {
                    acceptOwnRetainedMsg = false;
                }
                else
                {
                    bool isAllowed = jsonMeta.Value<bool>("echo");
                    //Debug.Log("Ignore myself Id: " + ClientId.ToString());
                    if (!isAllowed) return;
                }

            }
        }
        JObject jsonValue = obj.Value<JObject>("Data");
        if (jsonValue != null)
        {
            try
            {
                Value = JsonConvert.DeserializeObject<T>(jsonValue.Value<string>("Value"));
                StatusFlag = Variable_Status.Recvied;

                SampleEventArgs args = new SampleEventArgs();
                args.senderName = jsonMeta.Value<string>("id");
                if (sharedMode != SenderRecieverSharedMode.Sender)
                {
                    // Only update if it is not exculsively a sender. (can be receiver or both).
                    OnDataReceived(args);
                }
            }
            catch (Exception e)
            {
            }
        }
    }


    protected virtual void OnDataReceived(SampleEventArgs e)
    {
        EventHandler<SampleEventArgs> handler = OnSharedPropertyUpdate;
        if (handler != null)
        {
            handler(this, e);
        }
    }
}
