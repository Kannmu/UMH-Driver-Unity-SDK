using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace UMH
{
    public class UMH_ProtocolHandler
    {
        private readonly UMH_Serial _serial;

        public event Action<UMH_Device_Config> OnConfigReceived;
        public event Action<UMH_Device_Status> OnStatusReceived;
        public event Action<byte> OnErrorReceived;
        public event Action<Stimulation> OnStimulationSent;

        private const string _common_info_color = "#686868ff";
        private const string _error_color = "#FF4040";

        public UMH_ProtocolHandler(UMH_Serial serial)
        {
            _serial = serial;
            _serial.OnFrameReceived += HandleFrame;
        }

        public void Dispose()
        {
            if (_serial != null)
            {
                _serial.OnFrameReceived -= HandleFrame;
            }
        }

        private void HandleFrame(byte[] frame)
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
                    // Debug.Log($"<color={_common_info_color}>[UMH] Command Acknowledged (ACK)</color>");
                    break;
                case UMH_Serial.ResponseType.NACK:
                    Debug.LogWarning($"<color={_common_info_color}>[UMH] Command Not Acknowledged (NACK)</color>");
                    break;
                case UMH_Serial.ResponseType.Ping_ACK:
                    break;
                case UMH_Serial.ResponseType.ReturnConfig:
                    if (payload != null && payload.Length > 0)
                    {
                        ParseConfig(payload);
                    }
                    break;
                case UMH_Serial.ResponseType.ReturnStatus:
                    if (payload != null && payload.Length > 0)
                    {
                        ParseStatus(payload);
                    }
                    break;
                case UMH_Serial.ResponseType.SACK:
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
        private void ParseConfig(byte[] payload)
        {
            try
            {
                UMH_Device_Config newConfig = new();
                int offset = 0;
                newConfig.Version = BitConverter.ToInt32(payload, offset); offset += 4;
                newConfig.ArrayType = (UMH_ArrayType)payload[offset++];
                newConfig.ArraySize = BitConverter.ToInt32(payload, offset); offset += 4;
                newConfig.NumTransducers = BitConverter.ToInt32(payload, offset); offset += 4;
                newConfig.TransducerSize = BitConverter.ToSingle(payload, offset); offset += 4;
                newConfig.TransducerSpacing = BitConverter.ToSingle(payload, offset); offset += 4;

                OnConfigReceived?.Invoke(newConfig);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UMH] Error parsing config: {ex.Message}");
            }
        }

        private void ParseStatus(byte[] payload)
        {
            try
            {
                UMH_Device_Status newStatus = new();
                int offset = 0;
                newStatus.Voltage_VDDA = BitConverter.ToSingle(payload, offset); offset += 4;
                newStatus.Voltage_3V3 = BitConverter.ToSingle(payload, offset); offset += 4;
                newStatus.Voltage_5V0 = BitConverter.ToSingle(payload, offset); offset += 4;
                newStatus.Temperature = BitConverter.ToSingle(payload, offset); offset += 4;
                newStatus.DeltaTime = BitConverter.ToDouble(payload, offset); offset += 8;
                newStatus.LoopFreq = BitConverter.ToSingle(payload, offset); offset += 4;
                newStatus.StimulationType = (UMH_Stimulation_Type)payload[offset++];
                newStatus.CalibrationMode = BitConverter.ToInt32(payload, offset); offset += 4;
                newStatus.PhaseSetMode = BitConverter.ToInt32(payload, offset); offset += 4;

                OnStatusReceived?.Invoke(newStatus);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UMH] Error parsing status: {ex.Message}");
            }
        }

        public async Task SetEnableAsync(bool enable)
        {
            byte val = enable ? (byte)0x01 : (byte)0x00;
            await _serial.SendFrameAsync(UMH_Serial.CommandType.EnableDisable, new byte[] { val });
        }

        public async Task GetConfigAsync()
        {
            await _serial.SendFrameAsync(UMH_Serial.CommandType.GetConfig);
        }

        public async Task GetStatusAsync()
        {
            await _serial.SendFrameAsync(UMH_Serial.CommandType.GetStatus);
        }


        public async Task SetStimulationAsync(Stimulation stimulation)
        {
            if (stimulation == null) return;

            List<byte> payload = new List<byte>();

            // 1. Type
            payload.Add((byte)stimulation.Type);

            // 2. Data
            byte[] dataBytes = stimulation.GetDataBytes();
            if (dataBytes != null)
                payload.AddRange(dataBytes);

            // 3. Strength
            payload.AddRange(BitConverter.GetBytes(stimulation.Strength));

            // 4. Frequency
            payload.AddRange(BitConverter.GetBytes(stimulation.Frequency));

            if (await _serial.SendFrameAsync(UMH_Serial.CommandType.SetStimulation, payload.ToArray()))
            {
                OnStimulationSent?.Invoke(stimulation);
            }
        }

        public async Task SetPhasesAsync(float[] phases)
        {
            if (phases == null || phases.Length == 0) return;

            if (phases.Length * 4 > 255)
            {
                Debug.LogError($"[UMH] SetPhasesAsync: Too many phases ({phases.Length}). Protocol supports max 63 floats per packet.");
                return;
            }

            byte[] data = new byte[phases.Length * 4];
            Buffer.BlockCopy(phases, 0, data, 0, data.Length);

            await _serial.SendFrameAsync(UMH_Serial.CommandType.SetPhases, data);
        }


    }
}
