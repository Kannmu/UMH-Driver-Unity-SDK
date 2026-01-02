using System.Collections;
using UnityEngine;
using UMH;
using TMPro;
using UnityEngine.EventSystems;

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

        private bool _needsUpdate = false;

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                _needsUpdate = true;
            }
            else
            {
                UpdateUIActivity();
            }
        }

        private void UpdateUIActivity()
        {
            Transform ui = transform.Find("UI");
            if (ui != null)
            {
                ui?.gameObject.SetActive(EnableUI);
                // pause the UpdateUI coroutine specifically

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
        private Transform _info;
        private const string _info_num_color = "#3a8b3aff";
        private const string _info_x_axis_color = "#8b3a3aff";
        private const string _info_y_axis_color = "#3a8b3aff";
        private const string _info_z_axis_color = "#3a648bff";

        // Cached UI Components to avoid GetComponent calls in Update loops
        private TextMeshProUGUI _voltageText;
        private TextMeshProUGUI _temperatureText;
        private TextMeshProUGUI _stimulationTypeText;
        private TextMeshProUGUI _deltaTimeText;
        private TextMeshProUGUI _loopFreqText;
        private TextMeshProUGUI _calibrationModeText;
        private TextMeshProUGUI _planeModeText;
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
            // Automatically add EventSystem to the scene if it doesn't exist
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

        // Start is called before the first frame update
        void Start()
        {
            InitializeUIComponents();

            UMH_API.SetRefreshRate(getDeviceStatusRate);

            // Initialize the wait object
            WaitUIUpdate ??= new WaitForSeconds(1.0f / uiUpdateRate);
        }

        private void InitializeUIComponents()
        {
            Transform _uiCanvas = transform.Find("UI");
            if (_uiCanvas == null) return;

            _info = _uiCanvas.Find("Info");
            if (_info != null)
            {
                _voltageText = _info.Find("Voltage")?.GetComponent<TextMeshProUGUI>();
                _temperatureText = _info.Find("Temperature")?.GetComponent<TextMeshProUGUI>();
                _stimulationTypeText = _info.Find("StimulationType")?.GetComponent<TextMeshProUGUI>();
                _deltaTimeText = _info.Find("DeltaTime")?.GetComponent<TextMeshProUGUI>();
                _loopFreqText = _info.Find("LoopFreq")?.GetComponent<TextMeshProUGUI>();
                _calibrationModeText = _info.Find("Calibration")?.GetComponent<TextMeshProUGUI>();
                _planeModeText = _info.Find("Plane")?.GetComponent<TextMeshProUGUI>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_needsUpdate)
            {
                UpdateUIActivity();
                _needsUpdate = false;
            }
        }

        void FixedUpdate()
        {

        }

        

        private IEnumerator UpdateUI()
        {
            while (true)
            {
                if (UMH_API.IsConnected)
                {
                    UpdateDeviceStatusUI();
                }
                yield return WaitUIUpdate;
            }
        }

        private void UpdateDeviceStatusUI()
        {
            UMH_Device_Status status = UMH_API.GetDeviceStatus();

            if (_voltageText != null)
                _voltageText.text = $"Voltage: \t <color={_info_num_color}>{status.Voltage:F2}V</color>";

            if (_temperatureText != null)
                _temperatureText.text = $"Temperature: \t <color={_info_num_color}>{status.Temperature:F2}°C</color>";

            if(_stimulationTypeText != null)
                _stimulationTypeText.text = $"StimulationType: \t <color={_info_num_color}>{status.StimulationType}</color>";

            if (_deltaTimeText != null)
            {
                double dt = status.StimulationRefreshDeltaTime;
                (double dtVal, string dtUnit) = GetTimeWithUnit(dt);
                _deltaTimeText.text = $"DeltaTime: \t <color={_info_num_color}>{dtVal:F3}{dtUnit}</color>";
            }

            if (_loopFreqText != null)
            {
                double freq = status.LoopFreq;
                (double freqVal, string freqUnit) = GetFrequencyWithUnit(freq);
                _loopFreqText.text = $"LoopFreq: \t <color={_info_num_color}>{freqVal:F2}{freqUnit}</color>";
            }

            if (_calibrationModeText != null)
                _calibrationModeText.text = $"Calibration: \t <color={(status.CalibrationMode == 1 ? "#ff0000ff" : "#3a8b3aff")}>{(status.CalibrationMode == 1 ? "Cleaned" : "Calibrated")}</color>";

            if (_planeModeText != null)
                _planeModeText.text = $"Plane Mode: \t <color={(status.PlaneMode == 1 ? "#ff0000ff" : "#3a8b3aff")}>{(status.PlaneMode == 1 ? "True" : "False")}</color>";
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
