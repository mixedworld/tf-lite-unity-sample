using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MixedWorld.Util
{
    public class RawImageHelper : MonoBehaviour
    {
        [SerializeField] bool flipV = false;
        [SerializeField] bool flipH = false;
        [SerializeField] bool rotate180 = false;

        private RawImage theImage;

        void Start()
        {
            theImage = GetComponent<RawImage>();
            if (theImage != null)
            {

                if (flipV || flipH)
                {
                    // flipping is done via the UV rect of the raw image. Third value is Vertical, fourth is Horizontal.
                    theImage.uvRect = new Rect(1, 0, flipV ? -1 : 1, flipH ? -1 : 1);

                }
                if (rotate180)
                {
                    // Sometimes when a Raw Image if flipped it also needs to be rotated:
                    theImage.rectTransform.localEulerAngles = new Vector3(0f, 0f, 180f);
                }
            }
        }

        void Update()
        {

        }
    }
}

