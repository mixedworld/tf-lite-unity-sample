﻿using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Physics;
using Microsoft.MixedReality.Toolkit.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MixedWorld.Sharing
{

    public class V
    {
        private Vector3 v;
        public float x, y, z;
        public V()
        {
            v = new Vector3(x, y, z);
        }

        public Vector3 getVector3()
        {
            return new Vector3(x, y, z);
        }

        public void setVector3(Vector3 vec)
        {
            x = vec.x;
            y = vec.y;
            z = vec.z;
        }
    }

    public class Q
    {
        private Quaternion q;
        public float x, y, z, w;
        public Q()
        {
            q = new Quaternion(x, y, z, w);
        }

        public Quaternion getQuaternion()
        {
            return new Quaternion(x, y, z, w);
        }

        public void setQuaternion(Quaternion quat)
        {
            x = quat.x;
            y = quat.y;
            z = quat.z;
            w = quat.w;
        }
    }

    public class SharedTransformData
    {
        public V p; //localposition
        public Q r; //localrotate
        public V s; //localscale
    }

    [RequireComponent(typeof(ObjectIdentifier))]
    [DisallowMultipleComponent]
    public class SharedBehaviourTransform : MonoBehaviour
    {

        [SerializeField]
        private SenderRecieverSharedMode sharedMode = SenderRecieverSharedMode.Both;

        private SharedPropertyManager<SharedTransformData> transformData;
        private ObjectIdentifier objectIdentifier;
        //private ObjectDNA objectDNA;

        public Transform rootToTransmit = null;

        private Interpolator interpolator;

        private bool isSomeThingHappendWithMe = false;

        private bool isBeingManipulated = false;
    
        public SenderRecieverSharedMode SharedMode {
            get { return transformData.sharedMode; }
            set
            {
                sharedMode = value;
                if (transformData != null)
                    transformData.sharedMode = sharedMode;
            }
        }
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (transformData != null)
                transformData.sharedMode = this.sharedMode;
        }
#endif

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
            interpolator.SetTargetLocalPosition(transformData.Value.p.getVector3());

            // Rotate this object to face the user.
            interpolator.SetTargetLocalRotation(transformData.Value.r.getQuaternion());

            // Update objects local Scale.
            interpolator.SetTargetLocalScale(transformData.Value.s.getVector3());
        }


        // Start is called before the first frame update
        IEnumerator Start()
        {
            MainTopicBuilder.Instance.Maintopic = "Main";
            MainTopicBuilder.Instance.Subtopic = "SharedProperty";

            objectIdentifier = EnsureObjectIdentifier();
            interpolator = EnsureInterpolator();

            yield return new WaitUntil(()=> { return objectIdentifier.isInitialized; });
            if (rootToTransmit == null)
            {
                rootToTransmit = this.transform;
            }
            transformData = new SharedPropertyManager<SharedTransformData>(objectIdentifier.rootId, objectIdentifier.ObjectId, this.GetType().Name, nameof(transformData));
            transformData.sharedMode = sharedMode;
            

            transformData.Value = new SharedTransformData();

            transformData.Value.p = new V();
            transformData.Value.r = new Q();
            transformData.Value.s = new V();

            transformData.Value.p.setVector3(rootToTransmit.localPosition);
            transformData.Value.r.setQuaternion(rootToTransmit.localRotation);
            transformData.Value.s.setVector3(rootToTransmit.localScale);
            isBeingManipulated = false;

            transformData.OnSharedPropertyUpdate += OnObjectChanged;
        }


        // Update is called once per frame
        void Update()
        {
            if (transformData == null)
            {
                return;
            }
            if (isBeingManipulated)
            {
                //this is usually sends by a hololens

                //my own movment
                /// just send status
                /// 

                transformData.Value.p.setVector3(interpolator.TargetLocalPosition);
                transformData.Value.r.setQuaternion(interpolator.TargetLocalRotation);
                transformData.Value.s.setVector3(interpolator.TargetLocalScale);

                transformData.StatusFlag = Variable_Status.Dirty;

                // rb.isKinematic = true;
                isSomeThingHappendWithMe = true;

            }

            else if (transformData.Value.p.getVector3() != rootToTransmit.localPosition || transformData.Value.r.getQuaternion() != rootToTransmit.localRotation || transformData.Value.s.getVector3() != rootToTransmit.localScale)
            {
                if (interpolator.Running)
                {
                    // this is an action of my own
                    //Debug.Log("interpolation is detected");
                    // update the placement to match the user's gaze.
                    interpolator.SetTargetPosition(transformData.Value.p.getVector3());

                    // Rotate this object to face the user.
                    interpolator.SetTargetRotation(transformData.Value.r.getQuaternion());
                    //rb.isKinematic = true;

                    interpolator.SetTargetLocalScale(transformData.Value.s.getVector3());
                }
                else
                {
                    //this is usually sends by an editor
                    Debug.Log("editor movment is detected");
                    interpolator.Reset();
                    transformData.Value.p.setVector3(rootToTransmit.localPosition);
                    transformData.Value.r.setQuaternion(rootToTransmit.localRotation);
                    transformData.Value.s.setVector3(rootToTransmit.localScale);

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
            interpolator = EnsureInterpolator();
            objectIdentifier = EnsureObjectIdentifier();
            //objectDNA = EnsureObjectDNA();

            //objectDNA?.AddComponentItem(this.GetType().Name);

            ManipulationHandler mh = this.gameObject.GetComponent<ManipulationHandler>();
            if (mh)
            {
                mh.OnManipulationStarted.AddListener(OnManipulationStarted);
                mh.OnManipulationEnded.AddListener(OnManipulationEnded);
            }

        }
        private void OnDisable()
        {
           // objectDNA?.RemoveComponentItem(this.GetType().Name);

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
}

