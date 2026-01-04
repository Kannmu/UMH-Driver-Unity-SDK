using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public class UMH_Serial : IDisposable
    {
        public event Action<byte[]> OnFrameReceived;

        private SerialPort _serialPort;
        private bool _isRunning;
        private Thread _readThread;
        private readonly UMH_RingBuffer _ringBuffer = new UMH_RingBuffer(16384); // 16KB buffer
        private readonly byte[] _readBuffer = new byte[4096]; // Temp buffer for reading from SerialPort

        private const byte Header1 = 0xAA;
        private const byte Header2 = 0x55;
        private const byte Tail1 = 0x0D;
        private const byte Tail2 = 0x0A;

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;
        public string PortName { get; private set; }
        public int BaudRate { get; private set; }

        public enum CommandType : byte
        {
            EnableDisable = 0x01,
            Ping = 0x02,
            GetConfig = 0x03,
            GetStatus = 0x04,
            SetStimulation = 0x05,
            SetPhases = 0x06,
        }

        public enum ResponseType : byte
        {
            ACK = 0x80,
            NACK = 0x81,
            Ping_ACK = 0x82,
            ReturnConfig = 0x83,
            ReturnStatus = 0x84,
            SACK = 0x85,
            Error = 0xFF
        }

        public bool Connect(string portName, int baudRate, bool printLog = true)
        {
            Disconnect();

            PortName = portName;
            BaudRate = baudRate;

            try
            {
                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    ReadTimeout = 100,
                    WriteTimeout = 500
                };
                _serialPort.Open();

                _isRunning = true;
                _readThread = new Thread(ReadLoop) { IsBackground = true };
                _readThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                if (printLog)
                {
                    Debug.LogError($"[UMH] Connection failed: {ex.Message}");
                }
                Disconnect();
                return false;
            }
        }

        public void Disconnect()
        {
            _isRunning = false;
            if (_readThread != null && _readThread.IsAlive)
            {
                // Wait briefly for thread to finish
                if (!_readThread.Join(200))
                {
                    _readThread.Abort(); // Force kill if stuck (rarely needed but safe for cleanup)
                }
            }

            if (_serialPort != null)
            {
                if (_serialPort.IsOpen) _serialPort.Close();
                _serialPort.Dispose();
                _serialPort = null;
            }

            _ringBuffer.Clear();
        }

        public async Task<bool> SendFrameAsync(CommandType cmd, byte[] data = null)
        {
            if (!IsConnected) return false;

            byte dataLen = (byte)(data?.Length ?? 0);
            byte checksum = CalculateChecksum((byte)cmd, dataLen, data);

            int frameLen = 7 + dataLen;
            byte[] frame = new byte[frameLen];
            int idx = 0;

            frame[idx++] = Header1;
            frame[idx++] = Header2;
            frame[idx++] = (byte)cmd;
            frame[idx++] = dataLen;
            
            if (data != null && dataLen > 0)
            {
                Array.Copy(data, 0, frame, idx, dataLen);
                idx += dataLen;
            }

            frame[idx++] = checksum;
            frame[idx++] = Tail1;
            frame[idx++] = Tail2;

            try
            {
                await _serialPort.BaseStream.WriteAsync(frame, 0, frame.Length);
                await _serialPort.BaseStream.FlushAsync();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UMH] Send failed: {ex.Message}");
                return false;
            }
        }

        private void ReadLoop()
        {
            while (_isRunning && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    int bytesRead = _serialPort.Read(_readBuffer, 0, _readBuffer.Length);
                    if (bytesRead > 0)
                    {
                        _ringBuffer.Write(_readBuffer, bytesRead);
                        ProcessBuffer();
                    }
                }
                catch (TimeoutException) { }
                catch (Exception ex)
                {
                    if (_isRunning)
                    {
                         Debug.LogWarning($"[UMH] Read loop exception: {ex.Message}");
                         Disconnect(); 
                    }
                }
            }
        }

        private void ProcessBuffer()
        {
            // We need at least 7 bytes for a minimal frame (Head1+Head2+Cmd+Len+Chk+Tail1+Tail2)
            while (_ringBuffer.Count >= 7)
            {
                // Peek headers
                if (_ringBuffer.PeekByte(0) != Header1 || _ringBuffer.PeekByte(1) != Header2)
                {
                    _ringBuffer.Skip(1);
                    continue;
                }

                // Peek data length
                byte dataLen = _ringBuffer.PeekByte(3);
                int totalLen = 7 + dataLen;

                // Wait for full packet
                if (_ringBuffer.Count < totalLen)
                {
                    break; 
                }

                // Check tails
                if (_ringBuffer.PeekByte(totalLen - 2) != Tail1 || _ringBuffer.PeekByte(totalLen - 1) != Tail2)
                {
                    _ringBuffer.Skip(1); // Invalid frame structure
                    continue;
                }

                // Read full frame
                byte[] frame = new byte[totalLen];
                _ringBuffer.Peek(frame, totalLen);

                // Verify Checksum
                byte cmd = frame[2];
                byte checksum = frame[totalLen - 3];
                
                // Extract payload for checksum calc
                byte[] payload = null;
                if (dataLen > 0)
                {
                    payload = new byte[dataLen];
                    Array.Copy(frame, 4, payload, 0, dataLen);
                }

                if (CalculateChecksum(cmd, dataLen, payload) == checksum)
                {
                    // Valid frame
                    _ringBuffer.Skip(totalLen); // Consume from buffer
                    OnFrameReceived?.Invoke(frame);
                }
                else
                {
                    // Invalid checksum
                    Debug.LogWarning("[UMH] Checksum mismatch");
                    _ringBuffer.Skip(1); // Skip 1 byte and try to resync
                }
            }
        }

        private byte CalculateChecksum(byte cmd, byte len, byte[] data)
        {
            int sum = cmd + len;
            if (data != null)
            {
                foreach (byte b in data) sum += b;
            }
            return (byte)(sum & 0xFF);
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}
