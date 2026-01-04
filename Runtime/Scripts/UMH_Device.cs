using System.Collections;
using UnityEngine;

namespace UMH
{
    public class UMH_Device_Config
    {
        public int Version { get; set; }  = 5; // Version of the configuration
        public UMH_ArrayType ArrayType { get; set; } = UMH_ArrayType.Hex; // Array type (Square or Hex)
        public int ArraySize { get; set; } = 5; // Number of transducers in the edge of the array
        public int NumTransducers { get; set; } // Number of transducers in the array
        public float TransducerSize { get; set; } = 10e-3f; // in meters
        public float TransducerSpacing { get; set; } = 10e-3f; // in meters
    }

    public class UMH_Device_Status
    {
        public float Voltage_VDDA { get; set; }
        public float Voltage_3V3 { get; set; }
        public float Voltage_5V0 { get; set; }
        public float Temperature { get; set; }
        public UMH_Stimulation_Type StimulationType { get; set; }
        public double DeltaTime { get; set; }
        public float LoopFreq { get; set; }
        public int CalibrationMode { get; set; }
        public int PhaseSetMode { get; set; }
    }

    public class UMH_Device : MonoBehaviour
    {
        public GameObject TransducerPrefab;

        public static UMH_Device Instance { get; private set; }

        public static UMH_Device_Config DeviceConfig { get; set; } = new UMH_Device_Config();

        public static UMH_Device_Status DeviceStatus { get; set; } = new UMH_Device_Status();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            UMH_Array.Instance.Init();
        }

        public static void HandleStatusUpdate(UMH_Device_Status newStatus)
        {
            DeviceStatus = newStatus;
        }


    }
}
