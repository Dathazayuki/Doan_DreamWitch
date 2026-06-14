using System;
using System.Collections.Generic;
using UnityEngine;

namespace DreamKnight.EditorTools
{
    [CreateAssetMenu(fileName = "JpViTranslationTable", menuName = "DreamKnight/Localization/JP-VI Translation Table")]
    public class JpViTranslationTableSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            [TextArea(1, 4)]
            public string japanese;

            [TextArea(1, 4)]
            public string vietnamese;

            public bool enabled = true;
        }

        [SerializeField] private bool useContainsReplacement = true;
        [SerializeField] private List<Entry> entries = new List<Entry>(128);

        [Header("Auto Translate (Online)")]
        [SerializeField] private bool autoTranslateEnabled = true;
        [SerializeField] private string translateEndpoint = "https://libretranslate.com/translate";
        [SerializeField] private string apiKey;
        [SerializeField] private int requestTimeoutSeconds = 20;
        [SerializeField] private bool useFallbackPublicEndpoints = true;

        public bool UseContainsReplacement => useContainsReplacement;
        public List<Entry> Entries => entries;
        public bool AutoTranslateEnabled => autoTranslateEnabled;
        public string TranslateEndpoint => translateEndpoint;
        public string ApiKey => apiKey;
        public int RequestTimeoutSeconds => requestTimeoutSeconds;
        public bool UseFallbackPublicEndpoints => useFallbackPublicEndpoints;
    }
}
