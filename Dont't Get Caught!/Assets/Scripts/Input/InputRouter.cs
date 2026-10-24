using System;
using UnityEngine;

namespace DontGetCaught.Input
{
    /// <summary>
    /// Presents one selected input source to gameplay systems.
    /// Assign a MonoBehaviour that implements IGameInputSource in the Inspector.
    /// </summary>
    public sealed class InputRouter : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour sourceComponent;

        private IGameInputSource source;
        private bool hasWarnedAboutSource;

        public GameInputSample CurrentSample => source != null
            ? source.CurrentSample
            : GameInputSample.Disconnected;

        public string CurrentSourceName => source != null ? source.SourceName : "No input source";
        public string CurrentSourceStatus => source is SerialInputSource serial
            ? serial.Status
            : CurrentSourceName;
        public bool HasRawDelta => source is SerialInputSource;
        public float CurrentRawDelta => source is SerialInputSource serial ? serial.RawDelta : 0f;

        public event Action<GameInputSample> SampleUpdated;

        private void Awake()
        {
            ResolveSource();
        }

        private void Update()
        {
            if (source == null)
            {
                ResolveSource();
            }

            SampleUpdated?.Invoke(CurrentSample);
        }

        /// <summary>Allows an adapter to be swapped without changing gameplay code.</summary>
        public void SetSource(MonoBehaviour newSourceComponent)
        {
            sourceComponent = newSourceComponent;
            ResolveSource();
        }

        private void ResolveSource()
        {
            source = sourceComponent as IGameInputSource;
            if (source == null && !hasWarnedAboutSource)
            {
                Debug.LogWarning("InputRouter needs a component that implements IGameInputSource.", this);
                hasWarnedAboutSource = true;
            }

            if (source != null)
            {
                hasWarnedAboutSource = false;
            }
        }
    }
}
