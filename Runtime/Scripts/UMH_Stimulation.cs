using System.Collections;
using UnityEngine;
using UMH;
using TMPro;
using UnityEngine.EventSystems;

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
    }

    public class LinearSTM : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Linear;
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0); // Position of the Focus Point in meters
        public Vector3 StartPosition { get; set; } = new Vector3(0, 0, 0);
        public Vector3 EndPosition { get; set; } = new Vector3(0, 0, 0);

        public LinearSTM(Vector3 startPosition, Vector3 endPosition, float strength = 1.0f, float frequency = 200f)
        {
            StartPosition = startPosition;
            EndPosition = endPosition;
            Strength = strength;
            Frequency = frequency;
        }
    }

    public class CircularSTM : Stimulation
    {
        public override UMH_Stimulation_Type Type => UMH_Stimulation_Type.Circular;
        public Vector3 CenterPosition { get; set; } = new Vector3(0, 0, 0); // Center Position of the Circular Stimulation in meters
        public Vector3 NormalVector { get; set; } = new Vector3(0, 0, 0); // Normal Vector of the Circular Stimulation in meters
        public float Radius { get; set; } = 0.0f; // Radius of the Circular Stimulation in meters
        public CircularSTM(Vector3 centerPosition, Vector3 normalVector, float radius, float strength = 1.0f, float frequency = 200f)
        {
            CenterPosition = centerPosition;
            NormalVector = normalVector;
            Radius = radius;
            Strength = strength;
            Frequency = frequency;
        }
    }

    public class UMH_Stimulation
    {
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
                UMH_API.SetPoints(point);
                yield return UMH_Controller.Instance.WaitUIUpdate;
            }
        }
    }
}