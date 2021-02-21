/****************************************************************************
* Copyright 2021 mixed.world. All rights reserved.
*                                                                                                                                                                                                                                                                 
* https://mixed.world      
* 
*****************************************************************************/

namespace MixedWorld.Streaming
{
    using System;
    using UnityEngine;
    using UnityEngine.UI;

    /// <summary> RTP Configuration Menu </summary>
    public class RTPConfigView : MonoBehaviour
    {
        /// <summary> The key for storing the IP in PlayerPrefs. </summary>
        private const string PrefsIPStorageKey = "RTPServerIP";
        /// <summary> OnClick handler for streaming. </summary>
        public Action<string> OnClickStreaming;
        /// <summary> OnClick handler for cancel button. </summary>
        public Action OnClickCancel;
        /// <summary> The IP address. </summary>
        public InputField m_IPAddress;
        /// <summary> The streaming button. </summary>
        public Button m_StreamingBtn;
        /// <summary> The cancel button. </summary>
        public Button m_CancelBtn;
        /// <summary> The menu toggle button. </summary>
        public Button m_MenuToggleBtn;
        /// <summary> The panel root. </summary>
        public Transform m_PanelRoot;

        /// <summary> Starts this object. </summary>
        void Start()
        {
            if (PlayerPrefs.HasKey(PrefsIPStorageKey))
            {
                m_IPAddress.text = PlayerPrefs.GetString(PrefsIPStorageKey);
            }
            m_StreamingBtn.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString(PrefsIPStorageKey, m_IPAddress.text);
                OnClickStreaming?.Invoke(m_IPAddress.text);

                HidePanel();
            });

            m_CancelBtn.onClick.AddListener(() =>
            {
                OnClickCancel?.Invoke();

                HidePanel();
            });

            m_MenuToggleBtn.onClick.AddListener(() =>
            {
                ShowPanel();
            });

            ShowPanel();
        }

        /// <summary> Shows the panel. </summary>
        private void ShowPanel()
        {
            m_PanelRoot.gameObject.SetActive(true);
            m_MenuToggleBtn.gameObject.SetActive(false);
        }

        /// <summary> Hides the panel. </summary>
        private void HidePanel()
        {
            m_PanelRoot.gameObject.SetActive(false);
            m_MenuToggleBtn.gameObject.SetActive(true);
        }
    }
}
