using System.Collections;
using UnityEngine;
using UMH;
using TMPro;
using UnityEngine.EventSystems;

namespace UMH
{
    public class UMH_Focus: MonoBehaviour
    {
        public IEnumerator UpdateFocus()
        {
            Vector3 FocusPos = Vector3.zero;
            Stimulation stimulation = UMH_API.CurrentStimulation;

            while (true)
            {
                float Progress = Time.time * stimulation.Frequency % 1f;

                switch (UMH_API.CurrentStimulation.Type)
                {
                    case UMH_Stimulation_Type.Point:
                        if (stimulation is PointStimulation pointStimulation)
                        {
                            FocusPos = pointStimulation.Position;
                        }
                        break;
                    case UMH_Stimulation_Type.Vibration:
                        if (stimulation is VibrationStimulation vibrationStimulation)
                        {
                            FocusPos = Progress > 0.5f ? vibrationStimulation.StartPosition : vibrationStimulation.EndPosition;
                        }
                        break;
                    case UMH_Stimulation_Type.Linear:
                        if (stimulation is LinearSTM linearStimulation)
                        {
                            FocusPos = Vector3.Lerp(linearStimulation.StartPosition, linearStimulation.EndPosition, Progress);
                        }
                        break;
                    case UMH_Stimulation_Type.Circular:
                        if (stimulation is CircularSTM circularStimulation)
                        {
                            Vector3 normal = circularStimulation.NormalVector.normalized;
                            if (normal == Vector3.zero) normal = Vector3.up;
                            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
                            if (tangent.sqrMagnitude < 0.0001f)
                            {
                                tangent = Vector3.Cross(normal, Vector3.right);
                            }
                            tangent.Normalize();
                            Vector3 bitangent = Vector3.Cross(normal, tangent);
                            float angle = Progress * 2.0f * Mathf.PI;
                            FocusPos = circularStimulation.CenterPosition + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * circularStimulation.Radius;
                        }
                        break;
                }
                transform.localPosition = FocusPos;
                DrawFocusPath(FocusPos);
                yield return UMH_Controller.Instance.WaitUIUpdate;
            }
        }

        public void DrawFocusPath(Vector3 position)
        {
            foreach (var point in UMH_Device.UMH_Array_Ins.ArrayConnerPoints)
            {
                Debug.DrawLine(this.transform.position + point, position + this.transform.position, Color.green, 1.0f / UMH_Controller.Instance.UIUpdateRate);
            }
        }
    }
}





