using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public class UMH_Manager : MonoBehaviour
    {
        public static UMH_Manager Instance { get; private set; }
        public bool IsConnected => _serial != null && _serial.IsConnected;
        public event Action<byte[]> OnDataReceived;
        public event Action<UMH_Device_Status> OnStatusReceived;
        public event Action<Stimulation> OnStimulationSent;
        public event Action<byte> OnErrorReceived;
        public float RefreshRate = 30.0f;
        private UMH_Serial _serial;
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();
        private bool _isScanning;
        private const string _common_info_color = "#686868ff";
        private const string _error_color = "#FF4040";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Instance == null)
            {
                var obj = new GameObject("[UMH_Manager]");
                obj.hideFlags = HideFlags.HideInHierarchy;
                Instance = obj.AddComponent<UMH_Manager>();
                DontDestroyOnLoad(obj);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _serial = new UMH_Serial();

            _serial.OnFrameReceived -= HandleFrameReceived;
            _serial.OnFrameReceived += HandleFrameReceived;

            OnStatusReceived -= UMH_API.HandleStatusUpdate;
            OnStatusReceived += UMH_API.HandleStatusUpdate;

            OnStimulationSent -= UMH_API.HandleStimulationSent;
            OnStimulationSent += UMH_API.HandleStimulationSent;
        }

        private void Start()
        {
            _ = ScanAndConnectAsync();
            StartCoroutine(GetStatusCoroutine());
        }

        private void Update()
        {
            while (_mainThreadActions.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }

        private void OnDestroy()
        {
            if (_serial != null)
            {
                _serial.OnFrameReceived -= HandleFrameReceived;
                _serial.Dispose();
            }
        }

        public void Connect(string portName, int baudRate)
        {
            _serial.Connect(portName, baudRate);
        }

        public void Reconnect()
        {
            if (_serial != null)
            {
                _serial.Dispose();
            }

            _serial = new UMH_Serial();
            
            _serial.OnFrameReceived -= HandleFrameReceived;
            _serial.OnFrameReceived += HandleFrameReceived;
            _ = ScanAndConnectAsync();
        }

        public async Task<bool> SendCommandAsync(UMH_Serial.CommandType cmd, byte[] data = null)
        {
            if (!_serial.IsConnected) return false;
            return await _serial.SendFrameAsync(cmd, data);
        }

        private void HandleFrameReceived(byte[] frame)
        {
            _mainThreadActions.Enqueue(() => 
            {
                OnDataReceived?.Invoke(frame);
                ProcessFrame(frame);
            });
        }

        private void ProcessFrame(byte[] frame)
        {
            if (frame.Length < 4) return;
            
            UMH_Serial.ResponseType type = (UMH_Serial.ResponseType)frame[2];
            byte dataLen = frame[3];
            byte[] payload = null;
            
            if (dataLen > 0 && frame.Length >= 7 + dataLen)
            {
                payload = new byte[dataLen];
                Array.Copy(frame, 4, payload, 0, dataLen);
            }

            switch (type)
            {
                case UMH_Serial.ResponseType.ACK:
                    Debug.Log($"<color={_common_info_color}>[UMH] Command Acknowledged (ACK) at {DateTime.Now:HH:mm:ss.fff}</color>");
                    break;
                case UMH_Serial.ResponseType.NACK:
                    Debug.LogWarning($"<color={_common_info_color}>[UMH] Command Not Acknowledged (NACK) at {DateTime.Now:HH:mm:ss.fff}</color>");
                    break;
                case UMH_Serial.ResponseType.SACK:
                    // Debug.Log($"<color={_common_info_color}>[UMH] Stimulation Sent at {DateTime.Now:HH:mm:ss.fff}</color>");
                    break;
                case UMH_Serial.ResponseType.ReturnStatus:
                    if (payload != null && payload.Length > 0)
                    {
                        UMH_Device_Status newStatus = new();
                        int offset = 0;
                        newStatus.Voltage = BitConverter.ToSingle(payload[offset..(offset += 4)]);
                        newStatus.Temperature = BitConverter.ToSingle(payload[offset..(offset += 4)]);
                        newStatus.StimulationRefreshDeltaTime = BitConverter.ToDouble(payload[offset..(offset += 8)]);
                        newStatus.LoopFreq = BitConverter.ToSingle(payload[offset..(offset += 4)]);
                        newStatus.StimulationType = (UMH_Stimulation_Type)payload[offset ++];
                        newStatus.CalibrationMode = BitConverter.ToInt32(payload[offset..(offset += 4)]);
                        newStatus.PlaneMode = BitConverter.ToInt32(payload[offset..(offset += 4)]);
                        OnStatusReceived?.Invoke(newStatus);
                        // Debug.Log($"<color={_common_info_color}>[UMH] Status Received: Voltage={newStatus.Voltage:F2}V, Temperature={newStatus.Temperature:F1}°C, LoopFreq={newStatus.LoopFreq}Hz at {DateTime.Now:HH:mm:ss.fff}</color>");
                    }
                    break;
                case UMH_Serial.ResponseType.Ping_ACK:
                    // Ping ACK is mainly used for connection verification in ScanAndConnectAsync
                    // But we can log it here if needed
                    break;
                case UMH_Serial.ResponseType.Error:
                    if (payload != null && payload.Length > 0)
                    {
                        OnErrorReceived?.Invoke(payload[0]);
                        Debug.LogError($"<color={_error_color}>[UMH] Error Received: Code {payload[0]:X2}</color>");
                    }
                    break;
            }
        }

        #region Protocol Commands

        /// <summary>
        /// Command 0x04: Point Info
        /// </summary>
        public async void SetStimulationAsync(Stimulation stimulation)
        {
            List<byte> payload = new()
            {
                // 1. stimulation_type
                (byte)stimulation.Type
            };

            // 2. data
            switch (stimulation.Type)
            {
                case UMH_Stimulation_Type.Point:
                    if (stimulation is PointStimulation point)
                    {
                        // float[3] (position)
                        payload.AddRange(BitConverter.GetBytes(point.Position.x));
                        payload.AddRange(BitConverter.GetBytes(point.Position.z));
                        payload.AddRange(BitConverter.GetBytes(point.Position.y));
                    }
                    
                    break;
                case UMH_Stimulation_Type.Vibration:
                    if (stimulation is VibrationStimulation vibration)
                    {
                        // 2*float[3] (vibration start, vibration end)
                        payload.AddRange(BitConverter.GetBytes(vibration.StartPosition.x));
                        payload.AddRange(BitConverter.GetBytes(vibration.StartPosition.z));
                        payload.AddRange(BitConverter.GetBytes(vibration.StartPosition.y));
                        payload.AddRange(BitConverter.GetBytes(vibration.EndPosition.x));
                        payload.AddRange(BitConverter.GetBytes(vibration.EndPosition.z));
                        payload.AddRange(BitConverter.GetBytes(vibration.EndPosition.y));
                    }
                    break;
                case UMH_Stimulation_Type.Linear:
                    if (stimulation is LinearSTM linear)
                    {
                        // 2*float[3] (start position, end position)
                        payload.AddRange(BitConverter.GetBytes(linear.StartPosition.x));
                        payload.AddRange(BitConverter.GetBytes(linear.StartPosition.z));
                        payload.AddRange(BitConverter.GetBytes(linear.StartPosition.y));

                        payload.AddRange(BitConverter.GetBytes(linear.EndPosition.x));
                        payload.AddRange(BitConverter.GetBytes(linear.EndPosition.z));
                        payload.AddRange(BitConverter.GetBytes(linear.EndPosition.y));
                    }
                    break;
                case UMH_Stimulation_Type.Circular:
                    if (stimulation is CircularSTM circular)
                    {
                        // float[3] (center position, radius) -> Assuming center(3 floats) + radius(1 float)
                        payload.AddRange(BitConverter.GetBytes(circular.CenterPosition.x));
                        payload.AddRange(BitConverter.GetBytes(circular.CenterPosition.z));
                        payload.AddRange(BitConverter.GetBytes(circular.CenterPosition.y));

                        payload.AddRange(BitConverter.GetBytes(circular.NormalVector.x));
                        payload.AddRange(BitConverter.GetBytes(circular.NormalVector.z));
                        payload.AddRange(BitConverter.GetBytes(circular.NormalVector.y));

                        payload.AddRange(BitConverter.GetBytes(circular.Radius));
                    }
                    break;
            }

            // 3. strength
            payload.AddRange(BitConverter.GetBytes(stimulation.Strength));

            // 4. frequency
            payload.AddRange(BitConverter.GetBytes(stimulation.Frequency));
            
            OnStimulationSent?.Invoke(stimulation);
            await SendCommandAsync(UMH_Serial.CommandType.SetStimulation, payload.ToArray());
        }
        /// <summary>
        /// Command 0x05: SetPhases
        /// </summary>
        /// <param name="phases">Number of phases to set</param>
        public async void SetPhasesAsync(float[] phases)
        {
            // Protocol limit: Max payload is 255 bytes. 4 bytes per float => max 63 phases.
            if (phases.Length * 4 > 255)
            {
                Debug.LogError($"[UMH] SetPhasesAsync: Too many phases ({phases.Length}). Protocol supports max 63 floats per packet.");
                return;
            }

            byte[] data = new byte[phases.Length * 4];
            for (int i = 0; i < phases.Length; i++)
            {
                Array.Copy(BitConverter.GetBytes(phases[i]), 0, data, i * 4, 4);
            }
            await SendCommandAsync(UMH_Serial.CommandType.SetPhases, data);
        }

        /// <summary>
        /// Command 0x02: Enable/Disable
        /// </summary>
        /// <param name="enable">true to enable, false to disable</param>
        public async void SetEnableAsync(bool enable)
        {
            byte val = enable ? (byte)0x01 : (byte)0x00;
            await SendCommandAsync(UMH_Serial.CommandType.EnableDisable, new byte[] { val });
        }

        /// <summary>
        /// Command 0x03: GetStatus
        /// </summary>
        public async void GetStatusAsync()
        {
            await SendCommandAsync(UMH_Serial.CommandType.GetStatus);
        }

        #endregion

        private IEnumerator GetStatusCoroutine()
        {
            while (true)
            {
                yield return new WaitUntil(() => UMH_API.IsConnected);
                while (UMH_API.IsConnected)
                {
                    UMH_API.SendGetStatusCommand();
                    yield return new WaitForSeconds(1.0f / RefreshRate);
                }
                yield return null;
            }
        }


        private async Task ScanAndConnectAsync()
        {
            if (_isScanning || IsConnected) return;
            _isScanning = true;

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                _isScanning = false;
                return;
            }
            Debug.Log($"Scanning ports: {string.Join(", ", ports)}");

            var tcs = new TaskCompletionSource<UMH_Serial>();
            var tasks = new List<Task>();

            foreach (var port in ports)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var serial = new UMH_Serial();
                        if (await CheckPort(serial, port))
                        {
                            if (!tcs.TrySetResult(serial))
                            {
                                serial.Dispose();
                            }
                        }
                        else
                        {
                            serial.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error scanning port {port}: {ex.Message}");
                    }
                }));
            }

            var completedTask = await Task.WhenAny(tcs.Task, Task.WhenAll(tasks));

            if (completedTask == tcs.Task)
            {
                var newSerial = await tcs.Task;
                if (_serial != null)
                {
                    _serial.OnFrameReceived -= HandleFrameReceived;
                    _serial.Dispose();
                }
                _serial = newSerial;
                _serial.OnFrameReceived += HandleFrameReceived;
                Debug.Log($"UMH Device Connected on {_serial.PortName}");
            }

            _isScanning = false;
        }
        private async Task<bool> CheckPort(UMH_Serial serial, string port)
        {
            // Skip ports with "Bluetooth" in the name as requested
            if (port.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            if (!serial.Connect(port, 115200)) return false;

            byte pingVal = (byte)new System.Random().Next(0, 255);
            var tcs = new TaskCompletionSource<bool>();
            
            Action<byte[]> handler = (frame) =>
            {
                if (frame.Length > 4 && 
                    frame[2] == (byte)UMH_Serial.ResponseType.Ping_ACK && 
                    frame[4] == pingVal)
                {
                    tcs.TrySetResult(true);
                }
            };

            serial.OnFrameReceived += handler;
            await serial.SendFrameAsync(UMH_Serial.CommandType.Ping, new byte[] { pingVal });

            var task = await Task.WhenAny(tcs.Task, Task.Delay(200));
            serial.OnFrameReceived -= handler;

            return task == tcs.Task && tcs.Task.Result;
        }
    }
}
