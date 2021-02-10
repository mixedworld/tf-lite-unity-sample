using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Util
{
    public class MWHandBone : MonoBehaviour
    {
        [SerializeField] Transform joint;

        public void UpdateBone(bool up = false)
        {
            if (joint == null)
                return;
            if (up)
                transform.up = joint.localPosition - transform.parent.localPosition;
            transform.localScale = new Vector3(0.2f,Vector3.Distance(transform.parent.localPosition, joint.localPosition)/transform.parent.localScale.x,0.2f);
        }

        void Update()
        {
            if (joint == null)
            {
                return;
            }
            //if (transform.hasChanged)
            //{
            //    UpdateBone(false);
            //    transform.hasChanged = false;
            //}
        }
    }
}

