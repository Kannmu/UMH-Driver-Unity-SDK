using System.Collections;
using UnityEngine;
using UMH;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace UMH
{

    public class UMH_Controller : MonoBehaviour
    {
        [Header("Controller Function Settings")]

        [SerializeField, Tooltip("Enable or disable the UI.")]
        private bool enableUI = true;
        public bool EnableUI
        {
            get => enableUI;
            set
            {
                enableUI = value;
                UpdateUIActivity();
            }
        }

        [Header("Refresh Rates Settings")]

        [SerializeField, Tooltip("Refresh rate for getting device status.")]
        private float getDeviceStatusRate = 30f;
        public float GetDeviceStatusRate
        {
            get => getDeviceStatusRate;
            set
            {
                if (value <= 0.0f)
                {
                    Debug.LogError("Refresh rate must be greater than 0.0f");
                    return;
                }
                getDeviceStatusRate = value;
                UMH_API.SetRefreshRate(value);
            }
        }

        [SerializeField, Tooltip("Refresh rate for updating UI and focus point.")]
        private float uiUpdateRate = 30.0f;

        public WaitForSeconds WaitUIUpdate;

        public float UIUpdateRate
        {
            get => uiUpdateRate;
            set
            {
                if (value <= 0.0f)
                {
                    Debug.LogError("UI update rate must be greater than 0.0f");
                    return;
                }
                uiUpdateRate = value;
                WaitUIUpdate = new WaitForSeconds(1.0f / uiUpdateRate);
            }
        }


        public GameObject TextPrefab;

        private bool _needsUpdate = false;

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                _needsUpdate = true;
            }
            else
            {
#if UNITY_EDITOR
                // Use delayCall to avoid SendMessage errors during OnValidate (e.g. SetActive triggering hierarchy changes)
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this != null)
                    {
                        UpdateUIActivity();
                    }
                };
#endif
            }
        }

        private void UpdateUIActivity()
        {
            Transform ui = transform.Find("Canvas");
            if (ui != null)
            {
                ui.gameObject.SetActive(EnableUI);

                if (EnableUI)
                {
                    if (Application.isPlaying && gameObject.activeInHierarchy && _updateUICoroutine == null)
                    {
                        _updateUICoroutine = StartCoroutine(UpdateUI());
                    }
                }
                else
                {
                    if (Application.isPlaying && _updateUICoroutine != null)
                    {
                        StopCoroutine(_updateUICoroutine);
                        _updateUICoroutine = null;
                    }
                }
            }
        }

        public static UMH_Controller Instance { get; private set; }
        private Transform _config, _status;
        private const string _info_num_color = "#3a8b3aff";
        // private const string _info_x_axis_color = "#8b3a3aff";
        // private const string _info_y_axis_color = "#3a8b3aff";
        // private const string _info_z_axis_color = "#3a648bff";
        private Dictionary<string, TextMeshProUGUI> _configTexts = new Dictionary<string, TextMeshProUGUI>();
        private Dictionary<string, TextMeshProUGUI> _statusTexts = new Dictionary<string, TextMeshProUGUI>();
        private Coroutine _updateUICoroutine = null;

        private void OnEnable()
        {
            WaitUIUpdate ??= new WaitForSeconds(1.0f / uiUpdateRate);
            UpdateUIActivity();
        }

        private void OnDisable()
        {
            _updateUICoroutine = null;
        }

        private void Awake()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

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

        void Start()
        {
            InitializeUIComponents();

            UMH_API.SetRefreshRate(getDeviceStatusRate);

            WaitUIUpdate ??= new WaitForSeconds(1.0f / uiUpdateRate);
        }

        private void InitializeUIComponents()
        {
            Transform _ui = transform.Find("Canvas")?.Find("UI");
            if (_ui == null) return;

            _config = _ui.Find("Config");
            _status = _ui.Find("Status");

            // foreach (Transform child in _config)
            // {
            //     DestroyImmediate(child.gameObject);
            // }
            // foreach (Transform child in _status)
            // {
            //     DestroyImmediate(child.gameObject);
            // }

            // Create Config Text UI Components based on the elements in UMH_Device_Config
            foreach (var prop in typeof(UMH_Device_Config).GetProperties())
            {
                GameObject textObject = Instantiate(TextPrefab, _config);
                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.text = $"{prop.Name}: \t\t ...";
                _configTexts[prop.Name] = text;
            }

            // Create Status Text UI Components based on the elements in UMH_Device_Status
            foreach (var prop in typeof(UMH_Device_Status).GetProperties())
            {
                GameObject textObject = Instantiate(TextPrefab, _status);
                TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
                text.text = $"{prop.Name}: \t\t ...";
                _statusTexts[prop.Name] = text;
            }

        }

        void Update()
        {
            if (_needsUpdate)
            {
                UpdateUIActivity();
                _needsUpdate = false;
            }
        }

        private IEnumerator UpdateUI()
        {
            while (true)
            {
                if (UMH_API.IsConnected)
                {
                    UpdateDeviceConfigUI();
                    UpdateDeviceStatusUI();
                }
                yield return WaitUIUpdate;
            }
        }

        private void UpdateDeviceConfigUI()
        {
            UMH_Device_Config config = UMH_API.GetDeviceConfig();
            if (config == null) return;

            foreach (var prop in typeof(UMH_Device_Config).GetProperties())
            {
                if (_configTexts.TryGetValue(prop.Name, out var textComp))
                {
                    var value = prop.GetValue(config);
                    string displayValue;
                    string color = _info_num_color;

                    switch (prop.Name)
                    {
                        case nameof(UMH_Device_Config.TransducerSize):
                        case nameof(UMH_Device_Config.TransducerSpacing):
                            float meters = (float)value;
                            if (meters < 0.1f)
                                displayValue = $"{meters * 1000:F2}mm";
                            else
                                displayValue = $"{meters:F3}m";
                            break;
                        default:
                            displayValue = value.ToString();
                            break;
                    }

                    textComp.text = $"{prop.Name}: \t\t <color={color}>{displayValue}</color>";
                }
            }
        }

        private void UpdateDeviceStatusUI()
        {
            UMH_Device_Status status = UMH_API.GetDeviceStatus();
            if (status == null) return;

            foreach (var prop in typeof(UMH_Device_Status).GetProperties())
            {
                if (_statusTexts.TryGetValue(prop.Name, out var textComp))
                {
                    var value = prop.GetValue(status);
                    string displayValue;
                    string color = _info_num_color;

                    switch (prop.Name)
                    {
                        case nameof(UMH_Device_Status.Voltage_VDDA):
                        case nameof(UMH_Device_Status.Voltage_3V3):
                        case nameof(UMH_Device_Status.Voltage_5V0):
                            displayValue = $"{value:F2}V";
                            break;
                        case nameof(UMH_Device_Status.Temperature):
                            displayValue = $"{value:F2}°C";
                            break;
                        case nameof(UMH_Device_Status.DeltaTime):
                            (double dtVal, string dtUnit) = GetTimeWithUnit((double)value);
                            displayValue = $"{dtVal:F3}{dtUnit}";
                            break;
                        case nameof(UMH_Device_Status.LoopFreq):
                            (double freqVal, string freqUnit) = GetFrequencyWithUnit((float)value);
                            displayValue = $"{freqVal:F2}{freqUnit}";
                            break;
                        case nameof(UMH_Device_Status.CalibrationMode):
                            int calMode = (int)value;
                            color = calMode == 1 ? "#ff0000ff" : "#3a8b3aff";
                            displayValue = calMode == 1 ? "Cleaned" : "Calibrated";
                            break;
                        case nameof(UMH_Device_Status.PhaseSetMode):
                            int phaseMode = (int)value;
                            color = phaseMode == 1 ? "#ff0000ff" : "#3a8b3aff";
                            displayValue = phaseMode == 1 ? "True" : "False";
                            break;
                        default:
                            displayValue = value.ToString();
                            break;
                    }

                    textComp.text = $"{prop.Name}: \t\t <color={color}>{displayValue}</color>";
                }
            }
        }

        private (double, string) GetTimeWithUnit(double dt)
        {
            (double threshold, string unit, double multiplier)[] scales =
            {
            (1e-9, "ps", 1e12),
            (1e-6, "ns", 1e9 ),
            (1e-3, "μs", 1e6 ),
            (1.0 , "ms", 1e3 ),
            (1e3 , "s" , 1.0  ),
        };

            foreach (var (threshold, unit, multiplier) in scales)
            {
                if (dt < threshold)
                    return (dt * multiplier, unit);
            }

            return (dt, "s");
        }

        private (double, string) GetFrequencyWithUnit(double freq)
        {
            (double threshold, string unit)[] scales =
            {
            (1e9, "GHz"),
            (1e6, "MHz"),
            (1e3, "kHz"),
            (1.0, "Hz")
        };

            foreach (var (threshold, unit) in scales)
            {
                if (freq >= threshold)
                    return (freq / threshold, unit);
            }

            return (freq, "Hz");
        }

        public void Reconnect()
        {
            UMH_API.Reconnect();
        }
    }
}
