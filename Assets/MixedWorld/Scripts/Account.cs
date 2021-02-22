using System;
using System.Collections.Generic;


//public class Account 
//{
//    public long ID;
//    public string Name;
//    public List<string> Roles;
//    public DateTime CreateDate;
//}

public class ObjectIdentityList
{
    private List<ObjectIdentity> _Accounts;

    public List<ObjectIdentity> ObjectIdentities
    {
        get { return _Accounts; }
        set { _Accounts = value; }
    }
}

public class ObjectIdentity
{
    private string _ID;
    private string _Name;
    private List<string> _Components;
    private DateTime _CreateDataTime;

    public string ID
    {
        get{ return _ID; }
        set{ _ID = value;}
    }

    public List<string> Components
    {
        get{ return _Components; }
        set{ _Components = value; }
    }
    
    public DateTime CreatedDate
    {
        get { return _CreateDataTime; }
        set { _CreateDataTime = value;}
    }

}