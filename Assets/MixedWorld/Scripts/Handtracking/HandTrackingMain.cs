using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using Cysharp.Threading.Tasks;
using MixedWorld.Util;
using Microsoft.MixedReality.Toolkit.UI;
using TMPro;
using System.Collections;

namespace MixedWorld.Handtracking
{
    public class HandTrackingMain : MonoBehaviour
    {
        [SerializeField, FilePopup("*.tflite")] string palmModelFile = "coco_ssd_mobilenet_quant.tflite";
        [SerializeField, FilePopup("*.tflite")] string landmarkModelFile = "coco_ssd_mobilenet_quant.tflite";

        [SerializeField] RawImage cameraView = null;
        [SerializeField] RectTransform handRect = null;
        [SerializeField] MWTrackableHand rightHand;
        [SerializeField] MWTrackableHand leftHand;
        [SerializeField] RenderTexture targetRT = null;
        [SerializeField] Material EffectsMat = null;
        [SerializeField] TextMeshPro ScaleInfo = null;
        [SerializeField] bool runBackground;
        [SerializeField] bool isNreal = true;
        //[SerializeField] float palmPointSize = 1f;
        //[SerializeField] float palmLineSize = 1f;
        [SerializeField] bool flipHorizontal = false;
        [SerializeField] bool flipVertical = false;
        [SerializeField] bool drawFrames = false;
        [SerializeField] bool worldSpace = false;
        [SerializeField] bool postProcess = true;
        [SerializeField] bool twoHands = false;
        


        // Should be something bigger. maybe 0.0004f
        [SerializeField] float localScaleFac = 0.000404f;


        WebCamTexture webcamTexture;
        Texture nrealCamTexture = null;
        PalmDetect palmDetect;
        HandLandmarkDetect landmarkDetect;


        // just cache for GetWorldCorners
        Vector3[] rtCorners = new Vector3[4];
        Vector3[] worldJoints = new Vector3[HandLandmarkDetect.JOINT_COUNT];
        //PrimitiveDraw draw;
        List<PalmDetect.Result> palmResults;
        HandLandmarkDetect.Result landmarkResultS, landmarkResultF;
        UniTask<bool> task;
        CancellationToken cancellationToken;


        // for Image effects
        private Vector2 scale = new Vector2(1f, 1f);
        private Vector2 offset = Vector2.zero;
        private bool rightDrawn;
        private bool leftDrawn;
        private bool isReady = false;

        IEnumerator Start()
        {
            yield return new WaitUntil(() => { return cameraView != null && cameraView.texture != null; });
            isReady = true;
            string palmPath = Path.Combine(Application.streamingAssetsPath, palmModelFile);
            palmDetect = new PalmDetect(palmPath);

            string landmarkPath = Path.Combine(Application.streamingAssetsPath, landmarkModelFile);
            landmarkDetect = new HandLandmarkDetect(landmarkPath);
            Debug.Log($"landmark dimension: {landmarkDetect.Dim}");

            string cameraName = WebCamUtil.FindName(new WebCamUtil.PreferSpec()
            {
                isFrontFacing = false,
                kind = WebCamKind.WideAngle,
            });
            //Just for Debug:
            //cameraName = "Integrated Camera";
            if (isNreal)
            {
                nrealCamTexture = targetRT;
                scale.y = -1f;
                offset.y = 1f;
            }
            else
            {
                webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
                cameraView.texture = webcamTexture;
                webcamTexture.Play();
                Debug.Log($"Starting camera: {cameraName}");
            }
            targetRT.dimension = cameraView.texture.dimension;
            //draw = new PrimitiveDraw();
        }

        void OnDestroy()
        {
            webcamTexture?.Stop();
            palmDetect?.Dispose();
            landmarkDetect?.Dispose();
        }

        void Update()
        {
            if (!isReady) return;
            if (postProcess)
            {
                Graphics.Blit(cameraView.texture, targetRT, EffectsMat);
            }
            else
            {
                // Texture is already flipped now so we can just assign it
                targetRT = (RenderTexture)cameraView.texture;
                //Graphics.Blit(cameraView.texture, targetRT, new Vector2(1.0f, -1.0f), new Vector2(0.0f, 1.0f));
            }

            if (runBackground)
            {
                if (task.Status.IsCompleted())
                {
                    task = InvokeAsync();
                }
            }
            else
            {
                Invoke();
            }
            if (cameraView.texture.height != handRect.sizeDelta.y)
            {
                handRect.sizeDelta = new Vector2(cameraView.texture.width, cameraView.texture.height);
            }
            if (palmResults == null || palmResults.Count <= 0) return;
            //DrawFrames(palmResults);

            // FirstHand
            rightDrawn = false;
            leftDrawn = false;
            if (landmarkResultF != null && landmarkResultF.score >= 0.2f)
            {
                if (rightHand.DistToNewPos(landmarkResultF.joints[0]) < leftHand.DistToNewPos(landmarkResultF.joints[0]))
                {
                    DrawJoints(landmarkResultF.joints, rightHand);
                    rightDrawn = true;
                }
                else
                {
                    DrawJoints(landmarkResultF.joints, leftHand);
                    leftDrawn = true;
                }
            }
            //DrawCropMatrix(landmarkDetect.CropMatrix);
            // SecondHand
            if (landmarkResultS != null && landmarkResultS.score >= 0.2f)
            {
                if (leftDrawn)
                {
                    DrawJoints(landmarkResultS.joints, rightHand);
                }
                else if (rightDrawn)
                {
                    DrawJoints(landmarkResultS.joints, leftHand);
                }
                else if (rightHand.DistToNewPos(landmarkResultS.joints[0]) < leftHand.DistToNewPos(landmarkResultS.joints[0]))
                {
                    DrawJoints(landmarkResultS.joints, rightHand);
                }
                else
                {
                    DrawJoints(landmarkResultS.joints, leftHand);
                }
            }
        }

        void Invoke()
        {
            if (isNreal)
            {
                palmDetect.Invoke(targetRT);
            }
            else
            {
                palmDetect.Invoke(webcamTexture);
            }
            cameraView.material = palmDetect.transformMat;
            if (worldSpace)
            {
                handRect.GetWorldCorners(rtCorners);
            }
            else
            {
                handRect.GetLocalCorners(rtCorners);
            }
            //cameraView.rectTransform.GetWorldCorners(rtCorners);

            //palmResults = palmDetect.GetResults(0.7f, 0.3f);
            palmResults = palmDetect.GetResults(0.5f, 0.3f);

            if (palmResults.Count <= 0) return;

            // Detect two palms
            if (isNreal)
            {
                if (palmResults.Count > 1 && twoHands)
                {
                    landmarkDetect.Invoke(targetRT, palmResults[1]);
                    landmarkResultF = landmarkDetect.GetResult();
                }
                landmarkDetect.Invoke(targetRT, palmResults[0]);
                landmarkResultS = landmarkDetect.GetResult();
            }
            else
            {
                landmarkDetect.Invoke(webcamTexture, palmResults[0]);
                landmarkResultF = landmarkDetect.GetResult();
            }
        }

        async UniTask<bool> InvokeAsync()
        {
            if (isNreal)
            {
                palmResults = await palmDetect.InvokeAsync(targetRT, cancellationToken);
            }
            else
            {
                palmResults = await palmDetect.InvokeAsync(webcamTexture, cancellationToken);
            }
            cameraView.material = palmDetect.transformMat;
            if (worldSpace)
            {
                handRect.GetWorldCorners(rtCorners);
            }
            else
            {
                handRect.GetLocalCorners(rtCorners);
            }
            //cameraView.rectTransform.GetWorldCorners(rtCorners);

            if (palmResults.Count <= 0) return false;
            if (isNreal)
            {
                landmarkResultS = await landmarkDetect.InvokeAsync(targetRT, palmResults[0], cancellationToken);
                if (palmResults.Count > 1 && twoHands)
                {
                    landmarkResultF = await landmarkDetect.InvokeAsync(targetRT, palmResults[1], cancellationToken);
                }
            }
            else
            {
                landmarkResultF = await landmarkDetect.InvokeAsync(webcamTexture, palmResults[0], cancellationToken);
            }
            return true;
        }

        void DrawJoints(Vector3[] joints, MWTrackableHand hand)
        {
            // Get World Corners
            Vector3 min = rtCorners[0] * localScaleFac;
            Vector3 max = rtCorners[2] * localScaleFac;

            // Need to apply camera rotation and mirror on mobile
            Matrix4x4 mtx = Matrix4x4.identity;
            if (isNreal)
            {
                mtx = WebCamUtil.GetMatrix(0f, flipHorizontal, flipVertical);
            }
            else
            {
                mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, flipHorizontal, webcamTexture.videoVerticallyMirrored ^ flipVertical);
            }

            // Get joint locations in the world space
            float zScale = max.x - min.x;
            for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
            {
                Vector3 p0 = mtx.MultiplyPoint3x4(joints[i]);
                Vector3 p1 = MathTF.Lerp(min, max, p0);
                p1.z += (p0.z - 0.5f) * zScale;
                worldJoints[i] = p1;
            }
            hand.UpdateJoints(worldJoints);
        }

        public void ToggleHorizontal()
        {
            flipHorizontal = !flipHorizontal;
        }

        public void ToggleVertical()
        {
            flipVertical = !flipVertical;
        }

        public void ToggleDrawFrames()
        {
            drawFrames = !drawFrames;
        }

        public void ToggleWorldSpace()
        {
            worldSpace = !worldSpace;
        }

        public void UpdateHandScale(SliderEventData eventData)
        {
            //Default is 0.000185f
            // Min is 0.00005f
            //Max is 0.0005f

            // Optimum
            //0.0,-0.24,0.69
            //Scale 4.04
            //Scale 0.000404f is a slider value of 0.786667f
            localScaleFac = 0.00005f + eventData.NewValue * 0.00045f;
            ScaleInfo?.SetText($"{localScaleFac * 10000:F2}");
        }

        public void TogglePostProcess()
        {
            postProcess = !postProcess;
        }
        public void ToggleTwoHands()
        {
            twoHands = !twoHands;
        }
    }
}