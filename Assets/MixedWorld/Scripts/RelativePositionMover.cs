using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Microsoft.MixedReality.Toolkit.UI;

namespace MixedWorld.Util
{
    public class RelativePositionMover : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] TextMeshPro info;
        [SerializeField] TextMeshPro sliderZ;
        [SerializeField] float sliderOffset = 1f;
        [SerializeField] bool dontUpdate = false;


        [SerializeField] bool movx = true, movy = true;
        [SerializeField] bool rotX = true, rotY = true, rotZ = true;
        [SerializeField] bool scaX = true, scaY = true, scaZ = true;

        private Vector3 pos = Vector3.zero;
        private float sliderValue = 0f;

        //Start values:
        //-0.01x, -0.14y, 70z
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (dontUpdate) return;
            if (transform.hasChanged)
            {
                //transform.hasChanged = false;
                info?.SetText(
                    $"Pos: ({transform.localPosition.x:F2},{transform.localPosition.y:F2},{sliderValue:F2})  \n" +
                    $"Rot: {transform.localRotation.ToString()} \n" +
                    $"Sca: {transform.localScale.ToString()}");

                // Set the local Transform values of the target object.
                if (movx || movy)
                {
                    target.localPosition = new Vector3(movx ? transform.localPosition.x : 0f, movy ? transform.localPosition.y : 0f, sliderValue);
                }

            }
        }

        public void OnSliderUpdated(SliderEventData eventData)
        {
            transform.hasChanged = true;
            sliderValue = sliderOffset + eventData.NewValue;
            sliderZ?.SetText($"{sliderValue:F2}");
        }
    }

}
