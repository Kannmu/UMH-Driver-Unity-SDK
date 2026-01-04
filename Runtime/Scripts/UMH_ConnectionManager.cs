using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public class UMH_ConnectionManager
    {
        public UMH_Serial Serial { get; private set; }
        public bool IsConnected => Serial != null && Serial.IsConnected;
        
        public event Action<UMH_Serial> OnConnected;
        public event Action OnDisconnected;

        private bool _isScanning;

        public UMH_ConnectionManager()
        {
            Serial = new UMH_Serial();
        }

        public void ManualConnect(string portName, int baudRate)
        {
            // If already connected, disconnect first?
            if (IsConnected) Disconnect();

            if (Serial == null) Serial = new UMH_Serial();

            if (Serial.Connect(portName, baudRate))
            {
                Debug.Log($"[UMH] Manually connected to {portName}");
                OnConnected?.Invoke(Serial);
            }
        }

        public void Disconnect()
        {
            if (Serial != null)
            {
                Serial.Dispose();
                OnDisconnected?.Invoke();
            }
        }

        public void Reconnect()
        {
            Disconnect();
            // Re-create serial to ensure fresh state
            Serial = new UMH_Serial();
            _ = ScanAndConnectAsync();
        }

        public async Task ScanAndConnectAsync()
        {
            if (_isScanning || IsConnected) return;
            _isScanning = true;

            string[] ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
            {
                _isScanning = false;
                return;
            }
            Debug.Log($"[UMH] Scanning ports: {string.Join(", ", ports)}");

            var tcs = new TaskCompletionSource<UMH_Serial>();
            var tasks = new List<Task>();

            foreach (var port in ports)
            {
                // Capture loop variable
                string currentPort = port;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var tempSerial = new UMH_Serial();
                        if (await CheckPort(tempSerial, currentPort))
                        {
                            if (!tcs.TrySetResult(tempSerial))
                            {
                                tempSerial.Dispose();
                            }
                        }
                        else
                        {
                            tempSerial.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        // Debug.LogError($"Error scanning port {currentPort}: {ex.Message}");
                    }
                }));
            }

            var completedTask = await Task.WhenAny(tcs.Task, Task.WhenAll(tasks));

            if (completedTask == tcs.Task)
            {
                var newSerial = await tcs.Task;
                // Replace current serial
                if (Serial != null) Serial.Dispose();
                Serial = newSerial;
                
                Debug.Log($"[UMH] Device Connected on {Serial.PortName}");
                OnConnected?.Invoke(Serial);
            }

            _isScanning = false;
        }

        private async Task<bool> CheckPort(UMH_Serial serial, string port)
        {
            if (port.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0) return false;

            if (!serial.Connect(port, 115200, printLog: false)) return false;

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
