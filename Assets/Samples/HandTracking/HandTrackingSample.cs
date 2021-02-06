using System.Collections.Generic;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TensorFlowLite;
using Cysharp.Threading.Tasks;
using MixedWorld.Util;

public class HandTrackingSample : MonoBehaviour
{
    [SerializeField, FilePopup("*.tflite")] string palmModelFile = "coco_ssd_mobilenet_quant.tflite";
    [SerializeField, FilePopup("*.tflite")] string landmarkModelFile = "coco_ssd_mobilenet_quant.tflite";

    [SerializeField] RawImage cameraView = null;
    [SerializeField] RawImage debugPalmView = null;
    [SerializeField] RectTransform handRect = null;
    [SerializeField] MWTrackableHand rightHand;
    [SerializeField] bool runBackground;
    [SerializeField] bool isNreal = true;
    [SerializeField] float palmPointSize = 1f;
    [SerializeField] float palmLineSize = 1f;
    [SerializeField] bool flipHorizontal = false;
    [SerializeField] bool flipVertical = false;
    [SerializeField] bool drawFrames = false;
    [SerializeField] bool worldSpace = false;
    [SerializeField] float localScaleFac = 0.000185f;



    WebCamTexture webcamTexture;
    Texture nrealCamTexture = null;
    PalmDetect palmDetect;
    HandLandmarkDetect landmarkDetect;

    // just cache for GetWorldCorners
    Vector3[] rtCorners = new Vector3[4];
    Vector3[] worldJoints = new Vector3[HandLandmarkDetect.JOINT_COUNT];
    PrimitiveDraw draw;
    List<PalmDetect.Result> palmResults;
    HandLandmarkDetect.Result landmarkResult;
    UniTask<bool> task;
    CancellationToken cancellationToken;



    void Start()
    {
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
            nrealCamTexture = cameraView.texture;
        }
        else
        {
            webcamTexture = new WebCamTexture(cameraName, 1280, 720, 30);
            cameraView.texture = webcamTexture;
            webcamTexture.Play();
            Debug.Log($"Starting camera: {cameraName}");
        }

        draw = new PrimitiveDraw();
    }

    void OnDestroy()
    {
        webcamTexture?.Stop();
        palmDetect?.Dispose();
        landmarkDetect?.Dispose();
    }

    void Update()
    {
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

        if (palmResults == null || palmResults.Count <= 0) return;
        DrawFrames(palmResults);

        if (landmarkResult == null || landmarkResult.score < 0.2f) return;
        DrawCropMatrix(landmarkDetect.CropMatrix);
        DrawJoints(landmarkResult.joints);
    }

    void Invoke()
    {
        if (isNreal)
        {
            palmDetect.Invoke(cameraView.texture);
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

        palmResults = palmDetect.GetResults(0.7f, 0.3f);


        if (palmResults.Count <= 0) return;

        // Detect only first palm
        if (isNreal)
        {
            landmarkDetect.Invoke(cameraView.texture, palmResults[0]);
        }
        else
        {
            landmarkDetect.Invoke(webcamTexture, palmResults[0]);
        }
        debugPalmView.texture = landmarkDetect.inputTex;

        landmarkResult = landmarkDetect.GetResult();
    }

    async UniTask<bool> InvokeAsync()
    {
        if (isNreal)
        {
            palmResults = await palmDetect.InvokeAsync(cameraView.texture, cancellationToken);
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
            landmarkResult = await landmarkDetect.InvokeAsync(cameraView.texture, palmResults[0], cancellationToken);
        }
        else
        {
            landmarkResult = await landmarkDetect.InvokeAsync(webcamTexture, palmResults[0], cancellationToken);
        }
        debugPalmView.texture = landmarkDetect.inputTex;

        return true;
    }

    void DrawFrames(List<PalmDetect.Result> palms)
    {
        //Dont draw the bounding frame if disabled.
        if (!drawFrames) return;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];

        draw.color = Color.green;
        foreach (var palm in palms)
        {
            draw.Rect(MathTF.Lerp(min, max, palm.rect, true), 0.02f, min.z);

            foreach (var kp in palm.keypoints)
            {
                draw.Point(MathTF.Lerp(min, max, (Vector3)kp, true), 0.05f);
            }
        }
        draw.Apply();
    }

    void DrawCropMatrix(in Matrix4x4 matrix)
    {
        // Dont draw the Bounding Box Frame if disabled.
        if (!drawFrames) return;

        draw.color = Color.red;

        Vector3 min = rtCorners[0];
        Vector3 max = rtCorners[2];
        Matrix4x4 mtx = Matrix4x4.identity;
        if (isNreal)
        {
            mtx = WebCamUtil.GetMatrix(0, flipHorizontal, flipVertical) * matrix.inverse;
        }
        else
        {
            mtx = WebCamUtil.GetMatrix(-webcamTexture.videoRotationAngle, flipHorizontal, webcamTexture.videoVerticallyMirrored ^ flipVertical)
            * matrix.inverse;
        }

        Vector3 a = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 0, 0)));
        Vector3 b = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 0, 0)));
        Vector3 c = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(1, 1, 0)));
        Vector3 d = MathTF.LerpUnclamped(min, max, mtx.MultiplyPoint3x4(new Vector3(0, 1, 0)));

        draw.Quad(a, b, c, d, 0.02f);
        draw.Apply();
    }

    void DrawJoints(Vector3[] joints)
    {
        draw.color = Color.blue;

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

        // sphere
        for (int i = 0; i < HandLandmarkDetect.JOINT_COUNT; i++)
        {
            this.rightHand.Joints[i].localPosition = worldJoints[i];
            //draw.Cube(worldJoints[i], palmPointSize);
        }

        // Connection Lines
        var connections = HandLandmarkDetect.CONNECTIONS;
        for (int i = 0, n = 0; i < connections.Length; i += 2, n++)
        {
            Vector3 wj0 = worldJoints[connections[i]];
            Vector3 wj1 = worldJoints[connections[i + 1]];
            rightHand.Bones[n].up = (wj1 - wj0);

            //Quaternion q;
            //Vector3 a = Vector3.Cross(wj0,wj1);
            //q.x = a.x;
            //q.y = a.y;
            //q.z = a.z;
            //q.w = Mathf.Sqrt(wj0.magnitude * wj1.magnitude) + Vector3.Dot(wj0, wj1);

            //rightHand.Bones[n].localRotation = q.normalized;
            rightHand.Bones[n].localPosition = wj0;
            //rightHand.Bones[n].localRotation = Quaternion.Inverse(rightHand.Bones[n].parent.parent.rotation) * rightHand.Bones[n].rotation;

            rightHand.Bones[n].localScale = new Vector3(0.001f,Vector3.Distance(wj0, wj1),0.001f);
            //draw.Line3D(
            //    worldJoints[connections[i]],
            //    worldJoints[connections[i + 1]],
            //    palmLineSize);
        }

        draw.Apply();
    }

    public void ToggleHorizontal()
    {
        flipHorizontal = !flipHorizontal;
        Debug.Log("Toggle Horizontal pressed. " + flipHorizontal);

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
}
