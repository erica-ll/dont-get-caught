namespace DontGetCaught.Input
{
    /// <summary>One normalized airflow, fork-state, and connection snapshot.</summary>
    public readonly struct GameInputSample
    {
        public float Airflow { get; }
        public bool ForkReturned { get; }
        public bool IsConnected { get; }

        public GameInputSample(float airflow, bool forkReturned, bool isConnected)
        {
            Airflow = Clamp01(airflow);
            ForkReturned = forkReturned;
            IsConnected = isConnected;
        }

        public static GameInputSample Disconnected => new GameInputSample(0f, false, false);

        private static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            return value > 1f ? 1f : value;
        }
    }
}
