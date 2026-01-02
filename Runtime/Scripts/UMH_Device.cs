using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMH
{
    public class UMH_Device_Status
    {
        public float Voltage { get; set; }
        public float Temperature { get; set; }
        public UMH_Stimulation_Type StimulationType { get; set; }
        public double StimulationRefreshDeltaTime { get; set; }
        public float LoopFreq { get; set; }
        public int CalibrationMode { get; set; }
        public int PlaneMode { get; set; }
    }

    public enum UMH_Device_Version
    {
        V4,
        V5
    }

    public class UMH_Device : MonoBehaviour
    {
        public static UMH_Device Instance { get; private set; }

        public static readonly UMH_Array UMH_Array_Ins = new(UMH_Device_Version.V5);

        public UMH_Device_Version Version
        {
            get => UMH_Array_Ins.Version;
            set
            {
                UMH_Array_Ins.Version = value;
            }
        }

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
        }
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
