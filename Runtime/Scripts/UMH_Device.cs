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
        public float TransducerSpace;
        public Transducer[] Transducers;
        public int NumTransducers => Transducers.Length;
    }

    public struct Transducer
    {
        public float Size;
        public Vector3 Position;
        public float Phase;
    }

    public class UMH_Device : MonoBehaviour
    {
        public static UMH_Device Instance { get; private set; }
        public UMH_Array array;

        void Awake()
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
