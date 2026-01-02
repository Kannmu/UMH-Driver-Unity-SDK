using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMH
{
    public enum ArrayType
    {
        Rect,
        Hex
    }

    public class UMH_Array
    {
        public ArrayType ArrayType;
        public Vector2 ArraySize;
        public Vector3[] ArrayConnerPoints;
        public float TransducerSpace;
        public Transducer[] Transducers;
        public int NumTransducers => Transducers.Length;


        public UMH_Device_Version Version
        {
            get => Version;
            set
            {
                switch (value)
                {
                    case UMH_Device_Version.V4:
                        ArrayType = ArrayType.Rect;
                        ArraySize = new Vector2(0.13f, 0.13f);
                        TransducerSpace = 16.602f * 1e-3f;
                        Transducers = new Transducer[64];
                        ArrayConnerPoints = new Vector3[]
                        {
                            new(-ArraySize.x / 2, 0, -ArraySize.y / 2),
                            new(ArraySize.x / 2, 0, -ArraySize.y / 2),
                            new(ArraySize.x / 2, 0, ArraySize.y / 2),
                            new(-ArraySize.x / 2, 0, ArraySize.y / 2),
                        };
                        break;
                    case UMH_Device_Version.V5:
                        ArrayType = ArrayType.Hex;
                        ArraySize = new Vector2(0.1023f, 0.09f);
                        TransducerSpace = 10.0f * 1e-3f;
                        Transducers = new Transducer[60];
                        ArrayConnerPoints = new Vector3[]
                        {
                            new(-ArraySize.x / 2, 0, 0.0f),
                            new(ArraySize.x / 2, 0, 0.0f),
                            new(-(ArraySize.x / 2)*Mathf.Cos(60*Mathf.Deg2Rad), 0, ArraySize.y / 2),
                            new((ArraySize.x / 2)*Mathf.Cos(60*Mathf.Deg2Rad), 0, ArraySize.y / 2),
                            new(-(ArraySize.x / 2)*Mathf.Cos(60*Mathf.Deg2Rad), 0, -ArraySize.y / 2),
                            new((ArraySize.x / 2)*Mathf.Cos(60*Mathf.Deg2Rad), 0, -ArraySize.y / 2),
                        };
                        break;
                }
            }
        }

        public UMH_Array(UMH_Device_Version version)
        {
            Version = version;
        }

        public void GenerateTransducers(Transducer[] transducers)
        {
            
        }

    }
}

