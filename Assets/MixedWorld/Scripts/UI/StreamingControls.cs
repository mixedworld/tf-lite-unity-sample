using MixedWorld.VideoStream;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MixedWorld.UI
{
    public class StreamingControls : MonoBehaviour
    {
        [SerializeField] private VideoStreamRTP videoStreamer = null;
        /// <summary> The IP address. </summary>
        [SerializeField] public InputField m_IPAddress;
        private readonly string CacheServerIPKey = "MWRTPServerIP";

        private string lastInput = "";

        void Start()
        {
            lastInput = PlayerPrefs.GetString(CacheServerIPKey);
            if (lastInput == "")
            {
                m_IPAddress.text = videoStreamer.receiverURL;
            }
            else
            {
                m_IPAddress.text = lastInput;
                m_IPAddress.onValueChanged.Invoke(m_IPAddress.text);
            }
            Debug.Log("IP ADDRESS: " + m_IPAddress.text);
            m_IPAddress.onEndEdit.AddListener((string text) =>
            {
                if (text == "")
                {
                    m_IPAddress.text = lastInput;
                    m_IPAddress.onValueChanged.Invoke(m_IPAddress.text);
                }
                else
                {
                    lastInput = text;
                    PlayerPrefs.SetString(CacheServerIPKey, lastInput);
                }
            });
        }
    }
}
