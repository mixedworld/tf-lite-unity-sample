using NRKernal;
using UnityEngine;

public class NRRenderPlaneFocus : MonoBehaviour
{
    private Transform m_HeadTransfrom;
    private Vector3 m_FocusPosition;
    RaycastHit hitResult;

    void Start()
    {
        m_HeadTransfrom = NRSessionManager.Instance.CenterCameraAnchor;
    }

    void Update()
    {
        if (Physics.Raycast(new Ray(m_HeadTransfrom.position, m_HeadTransfrom.forward), out hitResult, 100))
        {
            m_FocusPosition = m_HeadTransfrom.InverseTransformPoint(hitResult.point);
            NRSessionManager.Instance.NRRenderer?.SetFocusDistance(m_FocusPosition.magnitude);
        }
    }
}
