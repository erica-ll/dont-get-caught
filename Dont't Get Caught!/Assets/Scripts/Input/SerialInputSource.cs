using System;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DontGetCaught.Input
{
    /// <summary>
    /// Reads airflow and fork Hall-sensor state from an Arduino-compatible serial device.
    /// The cup return state remains keyboard-controlled for the mixed MVP prototype.
    /// </summary>
    public sealed class SerialInputSource : MonoBehaviour, IGameInputSource
    {
        private static readonly Regex DeltaPattern = new Regex(
            @"(?:^|[,;\s{])delta\s*[:=]\s*(-?\d+(?:\.\d+)?)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ForkPattern = new Regex(
            @"(?:^|[,;\s{])fork(?:returned)?\s*[:=]\s*(0|1|true|false)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        [Header("Serial")]
        [Tooltip("Leave blank to auto-detect /dev/cu.usbmodem*, /dev/cu.usbserial*, or /dev/cu.wchusbserial* on macOS.")]
        [SerializeField] private string portName = "";
        [SerializeField] private int baudRate = 115200;
        [SerializeField, Min(0.1f)] private float reconnectIntervalSeconds = 2f;

        [Header("Airflow Mapping")]
        [Tooltip("Delta at or below this value becomes zero airflow.")]
        [SerializeField, Min(0f)] private float deltaThreshold = 20f;
        [Tooltip("Delta at or above this value becomes maximum airflow.")]
        [SerializeField, Min(1f)] private float deltaAtMaximumAirflow = 460f;

        [Header("Temporary Return-State Controls")]
        [SerializeField] private Key forkToggleKey = Key.F;
        [SerializeField] private Key cupToggleKey = Key.C;
        [SerializeField] private bool forkReturned = true;
        [SerializeField] private bool cupReturned = true;

        private SerialPort serialPort;
        private Thread readerThread;
        private volatile bool readerRunning;
        private volatile bool connected;
        private volatile float latestAirflow;
        private volatile float latestDelta;
        private volatile bool latestForkReturned;
        private volatile bool hasHardwareForkSignal;
        private float nextReconnectTime;
        private string activePortName = "";
        private string pendingStatus = "Not connected";
        private string displayedStatus = "Not connected";

        public GameInputSample CurrentSample { get; private set; } = GameInputSample.Disconnected;
        public string SourceName => connected ? $"Arduino Serial ({activePortName})" : "Arduino Serial";
        public float RawDelta => latestDelta;
        public string Status => displayedStatus;

        private void OnEnable()
        {
            TryOpenPort();
        }

        private void Update()
        {
            UpdateReturnStateSimulation();

            if (!connected && Time.unscaledTime >= nextReconnectTime)
            {
                TryOpenPort();
            }

            displayedStatus = pendingStatus;
            CurrentSample = new GameInputSample(
                connected ? latestAirflow : 0f,
                hasHardwareForkSignal ? latestForkReturned : forkReturned,
                cupReturned,
                connected);
        }

        private void OnDisable()
        {
            ClosePort();
            CurrentSample = GameInputSample.Disconnected;
        }

        private void OnApplicationQuit()
        {
            ClosePort();
        }

        private void TryOpenPort()
        {
            nextReconnectTime = Time.unscaledTime + reconnectIntervalSeconds;
            if (readerRunning || connected)
            {
                return;
            }

            string resolvedPort = ResolvePortName();
            if (string.IsNullOrWhiteSpace(resolvedPort))
            {
                pendingStatus = "No Arduino serial port found";
                return;
            }

            try
            {
                serialPort = new SerialPort(resolvedPort, baudRate)
                {
                    ReadTimeout = 250,
                    NewLine = "\n",
                    DtrEnable = true,
                    RtsEnable = false
                };
                serialPort.Open();

                activePortName = resolvedPort;
                connected = true;
                readerRunning = true;
                hasHardwareForkSignal = false;
                pendingStatus = $"Connected: {activePortName}";

                readerThread = new Thread(ReadLoop)
                {
                    IsBackground = true,
                    Name = "Arduino serial reader"
                };
                readerThread.Start();
            }
            catch (Exception exception)
            {
                connected = false;
                readerRunning = false;
                pendingStatus = $"Serial open failed: {exception.Message}";
                DisposeSerialPort();
            }
        }

        private void ReadLoop()
        {
            while (readerRunning && serialPort != null && serialPort.IsOpen)
            {
                try
                {
                    string line = serialPort.ReadLine();
                    if (TryParseDelta(line, out float delta))
                    {
                        latestDelta = delta;
                        latestAirflow = NormalizeDelta(delta, deltaThreshold, deltaAtMaximumAirflow);
                    }

                    if (TryParseForkReturned(line, out bool parsedForkReturned))
                    {
                        latestForkReturned = parsedForkReturned;
                        hasHardwareForkSignal = true;
                    }
                }
                catch (TimeoutException)
                {
                    // A short timeout lets shutdown and unplug events be noticed promptly.
                }
                catch (Exception exception)
                {
                    pendingStatus = $"Serial read stopped: {exception.Message}";
                    break;
                }
            }

            connected = false;
            readerRunning = false;
        }

        private void ClosePort()
        {
            readerRunning = false;
            connected = false;
            hasHardwareForkSignal = false;

            try
            {
                if (serialPort != null && serialPort.IsOpen)
                {
                    serialPort.Close();
                }
            }
            catch (Exception)
            {
                // The USB device might already be gone.
            }

            if (readerThread != null && readerThread.IsAlive)
            {
                readerThread.Join(500);
            }

            readerThread = null;
            DisposeSerialPort();
            pendingStatus = "Not connected";
        }

        private void DisposeSerialPort()
        {
            if (serialPort == null)
            {
                return;
            }

            serialPort.Dispose();
            serialPort = null;
        }

        private string ResolvePortName()
        {
            if (!string.IsNullOrWhiteSpace(portName))
            {
                return portName.Trim();
            }

            if (!Directory.Exists("/dev"))
            {
                return "";
            }

            string[] preferredPatterns =
            {
                "cu.usbmodem*",
                "cu.usbserial*",
                "cu.wchusbserial*"
            };

            foreach (string pattern in preferredPatterns)
            {
                string[] matches = Directory.GetFiles("/dev", pattern);
                if (matches.Length > 0)
                {
                    Array.Sort(matches, StringComparer.Ordinal);
                    return matches[0];
                }
            }

            return "";
        }

        private void UpdateReturnStateSimulation()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (!hasHardwareForkSignal && keyboard[forkToggleKey].wasPressedThisFrame)
            {
                forkReturned = !forkReturned;
            }

            if (keyboard[cupToggleKey].wasPressedThisFrame)
            {
                cupReturned = !cupReturned;
            }
        }

        private static float NormalizeDelta(float delta, float threshold, float maximum)
        {
            if (maximum <= threshold)
            {
                return delta > threshold ? 1f : 0f;
            }

            float normalized = (delta - threshold) / (maximum - threshold);
            if (normalized < 0f) return 0f;
            return normalized > 1f ? 1f : normalized;
        }

        private static bool TryParseDelta(string line, out float delta)
        {
            delta = 0f;
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            string trimmed = line.Trim();
            if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out delta))
            {
                return true;
            }

            Match match = DeltaPattern.Match(trimmed);
            return match.Success && float.TryParse(
                match.Groups[1].Value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out delta);
        }

        private static bool TryParseForkReturned(string line, out bool returned)
        {
            returned = false;
            if (string.IsNullOrWhiteSpace(line))
            {
                return false;
            }

            Match match = ForkPattern.Match(line);
            if (!match.Success)
            {
                return false;
            }

            string value = match.Groups[1].Value;
            returned = value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
            return true;
        }
    }
}
