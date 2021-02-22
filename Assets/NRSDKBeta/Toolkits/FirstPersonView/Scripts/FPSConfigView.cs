/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal.Beta.StreammingCast
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary> The FPS configuration view. </summary>
    public class FPSConfigView : MonoBehaviour
    {

        /// <summary> The cache server IP key. </summary>
        private const string CacheServerIPKey = "MWServerIP";
        /// <summary> The on click start. </summary>
        public Action<string> OnClickStart;
        /// <summary> The on click stop. </summary>
        public Action OnClickStop;
        /// <summary> The IP address. </summary>
        public InputField m_IPAddress;
        /// <summary>
        /// The default Server IP Address with port.
        /// </summary>
        [SerializeField] string defaultServer = @"mghighfive";
        /// <summary> The start button. </summary>
        public Button m_StartBtn;
        /// <summary> The stop control. </summary>
        public Button m_StopBtn;
        /// <summary> The hide control. </summary>
        public Button m_HideBtn;
        /// <summary> The panel root. </summary>
        public Transform m_PanelRoot;

        /// <summary> Starts this object. </summary>
        void Start()
        {
            m_IPAddress.text = PlayerPrefs.GetString(CacheServerIPKey);
            Debug.Log("IP ADDRESS: " + m_IPAddress.text);
            if (m_IPAddress.text == "")
            {
                m_IPAddress.text = defaultServer;
            }
            m_IPAddress.onValueChanged.Invoke(m_IPAddress.text);
            m_StartBtn.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString(CacheServerIPKey, m_IPAddress.text);
                OnClickStart?.Invoke(m_IPAddress.text);

                HidePanel();
            });

            m_StopBtn.onClick.AddListener(() =>
            {
                OnClickStop?.Invoke();

                HidePanel();
            });

            m_HideBtn.onClick.AddListener(() =>
            {
                ShowPanel();
            });

            ShowPanel();
        }

        /// <summary> Shows the panel. </summary>
        private void ShowPanel()
        {
            m_PanelRoot.gameObject.SetActive(true);
            m_HideBtn.gameObject.SetActive(false);
        }

        /// <summary> Hides the panel. </summary>
        private void HidePanel()
        {
            m_PanelRoot.gameObject.SetActive(false);
            m_HideBtn.gameObject.SetActive(true);
        }
    }
}
