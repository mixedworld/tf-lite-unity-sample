using System;
using System.Collections.Generic;


//public class Account 
//{
//    public long ID;
//    public string Name;
//    public List<string> Roles;
//    public DateTime CreateDate;
//}

public class ObjectIdentityList : SharedPropertyBase
{
    private Variable<List<ObjectIdentity>> _Accounts;

    public ObjectIdentityList()
    {
        _Accounts = new Variable<List<ObjectIdentity>>(l => Status = Variable_Status.Dirty);
    }
    public List<ObjectIdentity> ObjectIdentities
    {
        get { return _Accounts; }
        set { _Accounts.SetValue(value); }
    }
}

public class ObjectIdentity : SharedPropertyBase
{
    private Variable<string> _ID;
    private Variable<string> _Name;
    private Variable<List<string>> _Components;
    private Variable<DateTime> _CreateDataTime;

    public ObjectIdentity()
    {
        _ID = new Variable<string>(l => Status = Variable_Status.Dirty);
        _Name = new Variable<string>(s => Status = Variable_Status.Dirty);
        _Components = new Variable<List<string>>(l => Status = Variable_Status.Dirty);
        _CreateDataTime = new Variable<DateTime>(l => Status = Variable_Status.Dirty);
    }

    public string ID
    {
        get{ return _ID; }
        set{ _ID.SetValue(value);}
    }

    public List<string> Components
    {
        get{ return _Components; }
        set{ _Components.SetValue(value); }
    }
    
    public DateTime CreatedDate
    {
        get { return _CreateDataTime; }
        set { _CreateDataTime.SetValue(value);}
    }

}