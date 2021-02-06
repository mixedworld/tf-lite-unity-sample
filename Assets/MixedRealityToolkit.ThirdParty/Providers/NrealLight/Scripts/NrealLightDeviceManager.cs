using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Input.Utilities;
using Microsoft.MixedReality.Toolkit.Utilities;
using NRKernal;
using UnityEngine;
using UnityEngine.UI;
using TrackingState = Microsoft.MixedReality.Toolkit.TrackingState;

namespace MixedWorld.Nreal.Light.MixedReality.Toolkit.Input
{
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem),
        SupportedPlatforms.Android|SupportedPlatforms.WindowsEditor|SupportedPlatforms.MacEditor|SupportedPlatforms.LinuxEditor,
        "Nreal Light Device Manager")]
    public class NrealLightDeviceManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {
        private NrealLightController controller = null;
        private NRKernal.NRExamples.NRHomeMenu homeMenu = null;

        public NrealLightDeviceManager(
            IMixedRealityInputSystem inputSystem,
            string name,
            uint priority,
            BaseMixedRealityProfile profile) : base(inputSystem, name, priority, profile) { }

        public bool CheckCapability(MixedRealityCapability capability) => capability == MixedRealityCapability.ArticulatedHand;

        public override IMixedRealityController[] GetActiveControllers() => GetOrCreateController();

        private IMixedRealityController[] GetOrCreateController()
        {
            if (controller == null)
            {
                NRInput.LaserVisualActive = false;
                NRInput.ReticleVisualActive = false;

                var inputSystem = Service as IMixedRealityInputSystem;
                var handedness = NRInput.DomainHand == ControllerHandEnum.Left ? Handedness.Left : Handedness.Right;
                var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
                var inputSource = inputSystem?.RequestNewGenericInputSource($"Nreal Light Controller", pointers, InputSourceType.Hand);
                controller = new NrealLightController(TrackingState.NotTracked, handedness, inputSource);
                if (!controller.Enabled)
                {
                    Debug.LogWarning($"Nreal Light Controller was not setup correctly.");
                }
                for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
                {
                    controller.InputSource.Pointers[i].Controller = controller;
                }
                inputSystem.RaiseSourceDetected(controller.InputSource, controller);
            }
            return new[] {
                controller
            };
        }
        public override void Update()
        {
            base.Update();

            if (controller==null)
            {
                NRInput.LaserVisualActive = false;
                NRInput.ReticleVisualActive = false;

                var inputSystem = Service as IMixedRealityInputSystem;
                var handedness = NRInput.DomainHand == ControllerHandEnum.Left ? Handedness.Left : Handedness.Right;
                var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
                var inputSource = inputSystem?.RequestNewGenericInputSource($"Nreal Light Controller", pointers, InputSourceType.Hand);
                controller = new NrealLightController(TrackingState.NotTracked, handedness, inputSource);
                if (!controller.Enabled)
                {
                    Debug.LogWarning($"Nreal Light Controller was not setup correctly.");
                    return;
                }
                for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
                {
                    controller.InputSource.Pointers[i].Controller = controller;
                }
                inputSystem.RaiseSourceDetected(controller.InputSource, controller);
            }
            controller.UpdateController();

            if (homeMenu==null)
            {
                homeMenu = GameObject.FindObjectOfType<NRKernal.NRExamples.NRHomeMenu>();
                var canvas = homeMenu?.GetComponentInChildren<Canvas>();
                if (canvas!=null)
                {
                    if (canvas.gameObject.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
                    if (canvas.gameObject.GetComponent<CanvasUtility>() == null) canvas.gameObject.AddComponent<CanvasUtility>();
                }
            }
        }
    }
}
