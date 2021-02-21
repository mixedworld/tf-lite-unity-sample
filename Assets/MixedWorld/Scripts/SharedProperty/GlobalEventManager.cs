using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Utility
{
    public class GlobalEventManager : Singleton<GlobalEventManager>
    {
        public event EventHandler<SampleEventArgs> OnRefreshAllSharedPropertiesListener;
        // Start is called before the first frame update

        public void SendRefreshToAllSharedProperties()
        {
            OnRefreshSharedProperties(new SampleEventArgs());
        }

        protected virtual void OnRefreshSharedProperties(SampleEventArgs e)
        {
            EventHandler<SampleEventArgs> handler = OnRefreshAllSharedPropertiesListener;
            if (handler != null)
            {
                handler(this, e);
            }
        }
    }
}
