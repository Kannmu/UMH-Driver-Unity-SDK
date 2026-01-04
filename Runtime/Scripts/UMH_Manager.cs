using System;
using System.Collections.Concurrent;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public class UMH_Manager : MonoBehaviour
    {
        public static UMH_Manager Instance { get; private set; }
        
        public bool IsConnected => _connectionManager != null && _connectionManager.IsConnected;
        
        // Events
        public event Action<byte[]> OnDataReceived; // Raw frame? Maybe keep for debug
        public event Action<UMH_Device_Status> OnStatusReceived;
        public event Action<UMH_Device_Config> OnConfigReceived;
        public event Action<Stimulation> OnStimulationSent;
        public event Action<byte> OnErrorReceived;
        
        public float RefreshRate = 30.0f;
        
        private UMH_ConnectionManager _connectionManager;
        private UMH_ProtocolHandler _protocolHandler;
        
        private readonly ConcurrentQueue<Action> _mainThreadActions = new ConcurrentQueue<Action>();

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

            _connectionManager = new UMH_ConnectionManager();
            _connectionManager.OnConnected += HandleConnected;
            _connectionManager.OnDisconnected += HandleDisconnected;

            // Hook up API events to this Manager's events
            OnStatusReceived += UMH_Device.HandleStatusUpdate;
            OnConfigReceived += UMH_API.SetDeviceConfig;
            OnStimulationSent += UMH_Stimulation.HandleStimulationSent;
        }

        private void Start()
        {
            _ = _connectionManager.ScanAndConnectAsync();
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
            if (_connectionManager != null)
            {
                _connectionManager.OnConnected -= HandleConnected;
                _connectionManager.OnDisconnected -= HandleDisconnected;
                _connectionManager.Disconnect();
            }
            
            if (_protocolHandler != null)
            {
                _protocolHandler.Dispose();
            }

            OnStatusReceived -= UMH_Device.HandleStatusUpdate;
            OnConfigReceived -= UMH_API.SetDeviceConfig;
            OnStimulationSent -= UMH_Stimulation.HandleStimulationSent;
        }

        private void HandleConnected(UMH_Serial serial)
        {
            // Clean up old handler if exists
            if (_protocolHandler != null)
            {
                _protocolHandler.OnStatusReceived -= DispatchStatusReceived;
                _protocolHandler.OnConfigReceived -= DispatchConfigReceived;
                _protocolHandler.OnErrorReceived -= DispatchErrorReceived;
                _protocolHandler.OnStimulationSent -= DispatchStimulationSent;
                _protocolHandler.Dispose();
            }

            _protocolHandler = new UMH_ProtocolHandler(serial);
            _protocolHandler.OnStatusReceived += DispatchStatusReceived;
            _protocolHandler.OnConfigReceived += DispatchConfigReceived;
            _protocolHandler.OnErrorReceived += DispatchErrorReceived;
            _protocolHandler.OnStimulationSent += DispatchStimulationSent;
            
            // Immediately request config upon connection
            _ = GetConfigAsync();
            
            serial.OnFrameReceived += DispatchRawFrame;
        }

        private void HandleDisconnected()
        {
            if (_protocolHandler != null)
            {
                _protocolHandler.Dispose();
                _protocolHandler = null;
            }
        }

        // Dispatchers to Main Thread
        private void DispatchStatusReceived(UMH_Device_Status status) => _mainThreadActions.Enqueue(() => OnStatusReceived?.Invoke(status));
        private void DispatchConfigReceived(UMH_Device_Config config) => _mainThreadActions.Enqueue(() => OnConfigReceived?.Invoke(config));
        private void DispatchErrorReceived(byte error) => _mainThreadActions.Enqueue(() => OnErrorReceived?.Invoke(error));
        private void DispatchStimulationSent(Stimulation stim) => _mainThreadActions.Enqueue(() => OnStimulationSent?.Invoke(stim));
        private void DispatchRawFrame(byte[] frame) => _mainThreadActions.Enqueue(() => OnDataReceived?.Invoke(frame));


        // Public API delegated to ConnectionManager
        public void Connect(string portName, int baudRate)
        {
            _connectionManager.ManualConnect(portName, baudRate);
        }

        public void Reconnect()
        {
            _connectionManager.Reconnect();
        }

        public async Task GetConfigAsync()
        {
            if (_protocolHandler != null)
                await _protocolHandler.GetConfigAsync();
        }

        public async Task GetStatusAsync()
        {
            if (_protocolHandler != null)
                await _protocolHandler.GetStatusAsync();
        }

        // Public API delegated to ProtocolHandler
        public async Task SetStimulationAsync(Stimulation stimulation)
        {
            if (_protocolHandler != null)
                await _protocolHandler.SetStimulationAsync(stimulation);
        }
        
        public async Task SetPhasesAsync(float[] phases)
        {
            if (_protocolHandler != null)
                await _protocolHandler.SetPhasesAsync(phases);
        }

        private IEnumerator GetStatusCoroutine()
        {
            while (true)
            {
                yield return new WaitUntil(() => IsConnected);
                while (IsConnected)
                {
                    _ = GetStatusAsync(); // Fire and forget (it's async task now)
                    yield return new WaitForSeconds(1.0f / RefreshRate);
                }
                yield return null;
            }
        }
    }
}
