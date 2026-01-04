using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMH
{
    public enum UMH_ArrayType
    {
        Rect,
        Hex
    }
    
    public class UMH_Array
    {
        private static UMH_Array _instance;
        public static UMH_Array Instance => _instance ??= new UMH_Array();
        public static Transducer[] Transducers { get; private set; }
        private static Vector2[] ArrayConnerPoints { get; set; }
        private UMH_Array() { }

        /// <summary>
        /// 根据当前设备配置初始化阵列
        /// </summary>
        public void Init()
        {
            GenerateTransducers();
            CalculateArrayConnerPoints();
        }

        private static void GenerateTransducers()
        {
            switch (UMH_Device.DeviceConfig.ArrayType)
            {
                case UMH_ArrayType.Rect:
                    {
                        Transducers = new Transducer[UMH_Device.DeviceConfig.NumTransducers];
                        for (int i = 0; i < Transducers.Length; i++)
                        {
                            int row = i / UMH_Device.DeviceConfig.ArraySize;
                            int col = i % UMH_Device.DeviceConfig.ArraySize;
                            Transducers[i] = new Transducer();
                            Transducers[i].Position = new Vector2(col * UMH_Device.DeviceConfig.TransducerSpacing, -row * UMH_Device.DeviceConfig.TransducerSpacing);
                        }
                    }
                    break;
                case UMH_ArrayType.Hex:
                    break;
                default:
                    break;
            }

        }
        public static Vector2[] GetArrayConnerPoints()
        {
            return ArrayConnerPoints;
        }
        private static void CalculateArrayConnerPoints()
        {

        }
    }
}
