using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UMH
{
    public enum UMH_Stimulation_Type
    {
        Point = 0,
        Vibration = 1,
        Linear = 2,
        Circular = 3,
    }

    public abstract class Stimulation
    {
        public abstract UMH_Stimulation_Type Type { get; }
        public float Strength { get; set; } = 1.0f; // Strength of the Focus Point
        public float Frequency { get; set; } = 200f; // Vibration Frequency in Hz

        public abstract byte[] GetDataBytes();
    }

    public class PointStimulation : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Point;
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0); // Position of the Focus Point in meters
        
        public PointStimulation(Vector3 position, float strength = 1.0f, float frequency = 200f)
        {
            Position = position;
            Strength = strength;
            Frequency = frequency;
        }

        public override byte[] GetDataBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Position.x));
            bytes.AddRange(BitConverter.GetBytes(Position.z)); // Note: Z and Y might be swapped in protocol? Checking Manager: x, z, y
            bytes.AddRange(BitConverter.GetBytes(Position.y));
            return bytes.ToArray();
        }
    }

    public class VibrationStimulation : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Vibration;
        public Vector3 StartPosition { get; set; } = new Vector3(0, 0, 0);
        public Vector3 EndPosition { get; set; } = new Vector3(0, 0, 0);

        public VibrationStimulation(Vector3 startPosition, Vector3 endPosition, float strength = 1.0f, float frequency = 200f)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Strength = strength;
            Frequency = frequency;
        }

        public override byte[] GetDataBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(StartPosition.x));
            bytes.AddRange(BitConverter.GetBytes(StartPosition.z));
            bytes.AddRange(BitConverter.GetBytes(StartPosition.y));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.x));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.z));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.y));
            return bytes.ToArray();
        }
    }

    public class LinearSTM : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Linear;
        // Position property seems unused/redundant in original code for LinearSTM, removing or keeping for compatibility? 
        // Original had public Vector3 Position. But constructor didn't set it. I'll remove it to clean up.
        public Vector3 StartPosition { get; set; } = new Vector3(0, 0, 0);
        public Vector3 EndPosition { get; set; } = new Vector3(0, 0, 0);

        public LinearSTM(Vector3 startPosition, Vector3 endPosition, float strength = 1.0f, float frequency = 200f)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Strength = strength;
            Frequency = frequency;
        }

        public override byte[] GetDataBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(StartPosition.x));
            bytes.AddRange(BitConverter.GetBytes(StartPosition.z));
            bytes.AddRange(BitConverter.GetBytes(StartPosition.y));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.x));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.z));
            bytes.AddRange(BitConverter.GetBytes(EndPosition.y));
            return bytes.ToArray();
        }
    }

    public class CircularSTM : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Circular;
        public Vector3 CenterPosition { get; set; } = new Vector3(0, 0, 0);
        public Vector3 NormalVector { get; set; } = new Vector3(0, 0, 0);
        public float Radius { get; set; } = 0.0f;
        
        public CircularSTM(Vector3 centerPosition, Vector3 normalVector, float radius, float strength = 1.0f, float frequency = 200f)
        {
            CenterPosition = centerPosition;
            NormalVector = normalVector;
            Radius = radius;
            Strength = strength;
            Frequency = frequency;
        }

        public override byte[] GetDataBytes()
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(CenterPosition.x));
            bytes.AddRange(BitConverter.GetBytes(CenterPosition.z));
            bytes.AddRange(BitConverter.GetBytes(CenterPosition.y));
            bytes.AddRange(BitConverter.GetBytes(NormalVector.x));
            bytes.AddRange(BitConverter.GetBytes(NormalVector.z));
            bytes.AddRange(BitConverter.GetBytes(NormalVector.y));
            bytes.AddRange(BitConverter.GetBytes(Radius));
            return bytes.ToArray();
        }
    }

    public class UMH_Stimulation
    {
        public static Stimulation CurrentStimulation;

        public static IEnumerator CircleTraject()
        {
            float radius = 0.04f;
            float freq = 1.0f;
            float angle = 0.0f;
            while (true)
            {
                angle = Time.time % (1 / freq) / (1 / freq) * 2 * Mathf.PI;
                float x = radius * Mathf.Sin(angle);
                float y = radius * Mathf.Cos(angle);
                Stimulation point = new CircularSTM(new Vector3(x, y, 0.05f), new Vector3(0, 0, 1), 0.04f, 1f, 200);
                UMH_API.SetStimulation(point);
                yield return UMH_Controller.Instance.WaitUIUpdate;
            }
        }

        public static void HandleStimulationSent(Stimulation newStimulation)
        {
            CurrentStimulation = newStimulation;
        }
    }
}
