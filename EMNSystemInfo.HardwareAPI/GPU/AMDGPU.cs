// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// Class that represents temperature sensors for AMD GPUs
    /// </summary>
    public class AMDGPUTemperatures
    {
        /// <summary>
        /// Gets the GPU core temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? Core { get; internal set; }

        /// <summary>
        /// Gets the GPU hot spot temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? HotSpot { get; internal set; }

        /// <summary>
        /// Gets the GPU liquid temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? Liquid { get; internal set; }

        /// <summary>
        /// Gets the GPU memory temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? Memory { get; internal set; }

        /// <summary>
        /// Gets the GPU MVDD temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? MVDD { get; internal set; }

        /// <summary>
        /// Gets the GPU PLX temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? PLX { get; internal set; }

        /// <summary>
        /// Gets the GPU SoC temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? SoC { get; internal set; }

        /// <summary>
        /// Gets the GPU VDDC temperature, in degrees Celsius (°C). This property is nullable.
        /// </summary>
        public double? VDDC { get; internal set; }
    }

    /// <summary>
    /// Class that represents power sensors for AMD GPUs
    /// </summary>
    public class AMDGPUPowerSensors
    {
        /// <summary>
        /// Gets the GPU core power, in watts (W). This property is nullable.
        /// </summary>
        public double? Core { get; internal set; }

        /// <summary>
        /// Gets the GPU PPT power, in watts (W). This property is nullable.
        /// </summary>
        public double? PPT { get; internal set; }

        /// <summary>
        /// Gets the GPU SoC power, in watts (W). This property is nullable.
        /// </summary>
        public double? SoC { get; internal set; }

        /// <summary>
        /// Gets the GPU total power, in watts (W). This property is nullable.
        /// </summary>
        public double? Total { get; internal set; }
    }

    /// <summary>
    /// Class that represents an individual AMD GPU
    /// </summary>
    public sealed class AMDGPU : GPU
    {
        private readonly int _adapterIndex;
        private readonly IntPtr _context = IntPtr.Zero;
        private double? _coreClock;
        private double? _coreLoad;
        private double? _coreVoltage;
        private readonly int _currentOverdriveApiLevel;
        private double? _fan;
        private double? _fanControlPercentage;
        private readonly bool _frameMetricsStarted;
        private double? _fullscreenFps;
        private double? _memoryClock;
        private double? _memoryLoad;
        private double? _memoryVoltage;
        private readonly bool _overdriveApiSupported;
        private double? _socClock;
        private double? _socVoltage;
        private bool? _newQueryPmLogDataGetExists;

        /// <summary>
        /// Gets the GPU core clock speed, in megahertz (MHz). This property is nullable.
        /// </summary>
        public double? CoreClockSpeed => _coreClock;

        /// <summary>
        /// Gets the GPU core load percantage. This property is nullable.
        /// </summary>
        public double? CoreLoad => _coreLoad;

        /// <summary>
        /// Gets the GPU core voltage, in volts (V). This property is nullable.
        /// </summary>
        public double? CoreVoltage => _coreVoltage;

        /// <summary>
        /// Gets the GPU fan speed, in revolutions per minute (RPM). This property is nullable.
        /// </summary>
        public double? FanRPM => _fan;

        /// <summary>
        /// Gets the GPU fan speed percentage. This property is nullable.
        /// </summary>
        public double? FanSpeedPercentage => _fanControlPercentage;

        /// <summary>
        /// Gets the GPU fullscreen FPS. This property is nullable.
        /// </summary>
        public double? FullscreenFPS
        {
            get
            {
                if (_frameMetricsStarted)
                {
                    return _fullscreenFps;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the GPU memory clock speed, in megahertz (MHz). This property is nullable.
        /// </summary>
        public double? MemoryClockSpeed => _memoryClock;

        /// <summary>
        /// Gets the GPU memory load percentage. This property is nullable.
        /// </summary>
        public double? MemoryLoad => _memoryLoad;

        /// <summary>
        /// Gets the GPU memory voltage, in volts (V). This property is nullable.
        /// </summary>
        public double? MemoryVoltage => _memoryVoltage;

        /// <summary>
        /// Gets the GPU SoC clock speed, in megahertz (MHz). This property is nullable.
        /// </summary>
        public double? SoCClock => _socClock;

        /// <summary>
        /// Gets the GPU SoC voltage, in volts (V). This property is nullable.
        /// </summary>
        public double? SoCVoltage => _socVoltage;

        /// <summary>
        /// Gets the GPU temperature sensors.
        /// </summary>
        public AMDGPUTemperatures Temperatures { get; private set; } = new();

        /// <summary>
        /// Gets the GPU power sensors.
        /// </summary>
        public AMDGPUPowerSensors PowerSensors { get; private set; } = new();

        /// <summary>
        /// Gets the GPU bus number.
        /// </summary>
        public int BusNumber { get; }

        /// <summary>
        /// Gets the GPU device number.
        /// </summary>
        public int DeviceNumber { get; }

        internal AMDGPU(ATIADLxx.ADLAdapterInfo adapterInfo) 
        {
            Type = GPUType.AMDGPU; 

            _adapterIndex = adapterInfo.AdapterIndex;
            _gpuName = adapterInfo.AdapterName.Trim();
            BusNumber = adapterInfo.BusNumber;
            DeviceNumber = adapterInfo.DeviceNumber;

            string[] deviceIds = D3DDisplayDevice.GetDeviceIdentifiers();
            if (deviceIds != null)
            {
                foreach (string deviceId in deviceIds)
                {
                    string actualDeviceId = D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

                    if ((actualDeviceId.IndexOf(adapterInfo.PNPString, StringComparison.OrdinalIgnoreCase) != -1 ||
                         adapterInfo.PNPString.IndexOf(actualDeviceId, StringComparison.OrdinalIgnoreCase) != -1))
                    {
                        Initialize(deviceId);

                        break;
                    }
                }
            }

            int supported = 0;
            int enabled = 0;
            int version = 0;

            if (ATIADLxx.ADL_Method_Exists(nameof(ATIADLxx.ADL2_Adapter_FrameMetrics_Caps)) && ATIADLxx.ADL2_Adapter_FrameMetrics_Caps(_context, _adapterIndex, ref supported) == ATIADLxx.ADLStatus.ADL_OK)
            {
                if (supported == ATIADLxx.ADL_TRUE && ATIADLxx.ADL2_Adapter_FrameMetrics_Start(_context, _adapterIndex, 0) == ATIADLxx.ADLStatus.ADL_OK)
                {
                    _frameMetricsStarted = true;
                    _fullscreenFps = -1;
                }
            }

            if (ATIADLxx.ADL_Overdrive_Caps(_adapterIndex, ref supported, ref enabled, ref version) == ATIADLxx.ADLStatus.ADL_OK)
            {
                _overdriveApiSupported = supported == ATIADLxx.ADL_TRUE;
                _currentOverdriveApiLevel = version;
            }
            else
            {
                _currentOverdriveApiLevel = -1;
            }

            if (_currentOverdriveApiLevel >= 5 && ATIADLxx.ADL_Method_Exists(nameof(ATIADLxx.ADL2_Main_Control_Create)) && ATIADLxx.ADL2_Main_Control_Create(ATIADLxx.Main_Memory_Alloc, _adapterIndex, ref _context) != ATIADLxx.ADLStatus.ADL_OK)
            {
                _context = IntPtr.Zero;
            }

            ATIADLxx.ADLFanSpeedInfo fanSpeedInfo = new();
            if (ATIADLxx.ADL_Overdrive5_FanSpeedInfo_Get(_adapterIndex, 0, ref fanSpeedInfo) != ATIADLxx.ADLStatus.ADL_OK)
            {
                fanSpeedInfo.MaxPercent = 100;
                fanSpeedInfo.MinPercent = 0;
            }

            Update();
        }

        public override void Update()
        {
            base.Update();

            if (_frameMetricsStarted)
            {
                float framesPerSecond = 0;
                if (ATIADLxx.ADL2_Adapter_FrameMetrics_Get(_context, _adapterIndex, 0, ref framesPerSecond) == ATIADLxx.ADLStatus.ADL_OK)
                {
                    _fullscreenFps = framesPerSecond;
                }
            }

            if (_overdriveApiSupported)
            {
                double? od5Temp = null;
                GetOD5Temperature(ref od5Temp);
                Temperatures.Core = od5Temp;
                GetOD5FanSpeed(ATIADLxx.ADL_DL_FANCTRL_SPEED_TYPE_RPM, ref _fan);
                GetOD5FanSpeed(ATIADLxx.ADL_DL_FANCTRL_SPEED_TYPE_PERCENT, ref _fanControlPercentage);
                GetOD5CurrentActivity();

                if (_currentOverdriveApiLevel >= 6)
                {
                    double? power = null;
                    GetOD6Power(ATIADLxx.ADLODNCurrentPowerType.ODN_GPU_TOTAL_POWER, ref power);
                    PowerSensors.Total = power;
                    GetOD6Power(ATIADLxx.ADLODNCurrentPowerType.ODN_GPU_PPT_POWER, ref power);
                    PowerSensors.PPT = power;
                    GetOD6Power(ATIADLxx.ADLODNCurrentPowerType.ODN_GPU_SOCKET_POWER, ref power);
                    PowerSensors.SoC = power;
                    GetOD6Power(ATIADLxx.ADLODNCurrentPowerType.ODN_GPU_CHIP_POWER, ref power);
                    PowerSensors.Core = power;
                }

                if (_currentOverdriveApiLevel >= 7)
                {
                    double? temp = null;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.EDGE, ref temp, -256, 0.001, false);
                    Temperatures.Core = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.MEM, ref temp);
                    Temperatures.Memory = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.VRVDDC, ref temp);
                    Temperatures.VDDC = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.VRMVDD, ref temp);
                    Temperatures.MVDD = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.LIQUID, ref temp);
                    Temperatures.Liquid = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.PLX, ref temp);
                    Temperatures.PLX = temp;
                    GetODNTemperature(ATIADLxx.ADLODNTemperatureType.HOTSPOT, ref temp);
                    Temperatures.HotSpot = temp;
                }
            }

            if (_currentOverdriveApiLevel >= 8 || !_overdriveApiSupported)
            {
                ATIADLxx.ADLPMLogDataOutput logDataOutput = new();

                _newQueryPmLogDataGetExists ??= ATIADLxx.ADL_Method_Exists(nameof(ATIADLxx.ADL2_New_QueryPMLogData_Get));

                if (_newQueryPmLogDataGetExists == true && ATIADLxx.ADL2_New_QueryPMLogData_Get(_context, _adapterIndex, ref logDataOutput) == ATIADLxx.ADLStatus.ADL_OK)
                {
                    double? sensor = null;
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_EDGE, ref sensor, reset: false);
                    Temperatures.Core = sensor;
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_MEM, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_VRVDDC, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_VRMVDD, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_LIQUID, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_PLX, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_HOTSPOT, ref sensor, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_TEMPERATURE_SOC, ref sensor);

                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_CLK_GFXCLK, ref _coreClock, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_CLK_SOCCLK, ref _socClock);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_CLK_MEMCLK, ref _memoryClock, reset: false);

                    const int fanRpmIndex = (int)ATIADLxx.ADLSensorType.PMLOG_FAN_RPM;
                    const int fanPercentageIndex = (int)ATIADLxx.ADLSensorType.PMLOG_FAN_PERCENTAGE;

                    if (logDataOutput.sensors.Length is > fanRpmIndex and > fanPercentageIndex && logDataOutput.sensors[fanRpmIndex].value != ushort.MaxValue && logDataOutput.sensors[fanRpmIndex].supported != 0)
                    {
                        _fan = logDataOutput.sensors[fanRpmIndex].value;
                        _fanControlPercentage = logDataOutput.sensors[fanPercentageIndex].value;
                    }

                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_GFX_VOLTAGE, ref _coreVoltage, 0.001f, false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_SOC_VOLTAGE, ref _socVoltage, 0.001f);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_MEM_VOLTAGE, ref _memoryVoltage, 0.001f);

                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_INFO_ACTIVITY_GFX, ref _coreLoad, reset: false);
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_INFO_ACTIVITY_MEM, ref _memoryLoad);

                    double? power = null;
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_ASIC_POWER, ref power, reset: false);
                    PowerSensors.Total = power;
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_GFX_POWER, ref power, reset: false);
                    PowerSensors.Core = power;
                    GetPMLog(logDataOutput, ATIADLxx.ADLSensorType.PMLOG_SOC_POWER, ref power, reset: false);
                    PowerSensors.SoC = power;
                }
            }
        }

        private void GetOD5CurrentActivity()
        {
            ATIADLxx.ADLPMActivity adlpmActivity = new();
            if (ATIADLxx.ADL_Overdrive5_CurrentActivity_Get(_adapterIndex, ref adlpmActivity) == ATIADLxx.ADLStatus.ADL_OK)
            {
                if (adlpmActivity.EngineClock > 0)
                {
                    _coreClock = 0.01f * adlpmActivity.EngineClock;
                }
                else
                {
                    _coreClock = null;
                }

                if (adlpmActivity.MemoryClock > 0)
                {
                    _memoryClock = 0.01f * adlpmActivity.MemoryClock;
                }
                else
                {
                    _memoryClock = null;
                }

                if (adlpmActivity.Vddc > 0)
                {
                    _coreVoltage = 0.001f * adlpmActivity.Vddc;
                }
                else
                {
                    _coreVoltage = null;
                }

                _coreLoad = Math.Min(adlpmActivity.ActivityPercent, 100);
            }
            else
            {
                _coreClock = null;
                _memoryClock = null;
                _coreVoltage = null;
                _coreLoad = null;
            }
        }

        private void GetOD5FanSpeed(int speedType, ref double? fanSpeed)
        {
            ATIADLxx.ADLFanSpeedValue fanSpeedValue = new() { SpeedType = speedType };
            if (ATIADLxx.ADL_Overdrive5_FanSpeed_Get(_adapterIndex, 0, ref fanSpeedValue) == ATIADLxx.ADLStatus.ADL_OK)
            {
                fanSpeed = fanSpeedValue.FanSpeed;
            }
            else
            {
                fanSpeed = null;
            }
        }

        private void GetOD5Temperature(ref double? temperatureCore)
        {
            ATIADLxx.ADLTemperature temperature = new();
            if (ATIADLxx.ADL_Overdrive5_Temperature_Get(_adapterIndex, 0, ref temperature) == ATIADLxx.ADLStatus.ADL_OK)
            {
                temperatureCore = 0.001f * temperature.Temperature;
            }
            else
            {
                temperatureCore = null;
            }
        }

        /// <summary>
        /// Gets the OverdriveN temperature.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="sensor">The sensor.</param>
        /// <param name="minTemperature">The minimum temperature.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="reset">If set to <c>true</c>, resets the sensor value to <c>null</c>.</param>
        private void GetODNTemperature(ATIADLxx.ADLODNTemperatureType type, ref double? tempSensor, double minTemperature = -256, double scale = 1, bool reset = true)
        {
            // If a sensor isn't available, some cards report 54000 degrees C.
            // 110C is expected for Navi, so 256C should be enough to use as a maximum.

            int maxTemperature = (int)(256 / scale);
            minTemperature = (int)(minTemperature / scale);

            int temperature = 0;
            if (ATIADLxx.ADL2_OverdriveN_Temperature_Get(_context, _adapterIndex, type, ref temperature) == ATIADLxx.ADLStatus.ADL_OK && temperature >= minTemperature && temperature <= maxTemperature)
            {
                tempSensor = (float)(scale * temperature);
            }
            else if (reset)
            {
                tempSensor = null;
            }
        }

        /// <summary>
        /// Gets a PMLog sensor value.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="sensorType">Type of the sensor.</param>
        /// <param name="sensor">The sensor.</param>
        /// <param name="factor">The factor.</param>
        private void GetPMLog(ATIADLxx.ADLPMLogDataOutput data, ATIADLxx.ADLSensorType sensorType, ref double? sensor, float factor = 1.0f, bool reset = true)
        {
            int i = (int)sensorType;
            if (i < data.sensors.Length && data.sensors[i].supported != 0)
            {
                sensor = data.sensors[i].value * factor;
            }
            else if (reset)
            {
                sensor = null;
            }
        }

        /// <summary>
        /// Gets the Overdrive6 power.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="sensor">The sensor.</param>
        private void GetOD6Power(ATIADLxx.ADLODNCurrentPowerType type, ref double? powerSensor)
        {
            int powerOf8 = 0;
            if (ATIADLxx.ADL2_Overdrive6_CurrentPower_Get(_context, _adapterIndex, type, ref powerOf8) == ATIADLxx.ADLStatus.ADL_OK)
            {
                powerSensor = powerOf8 >> 8;
            }
            else
            {
                powerSensor = null;
            }
        }

        public override void Close()
        {
            if (_frameMetricsStarted)
                ATIADLxx.ADL2_Adapter_FrameMetrics_Stop(_context, _adapterIndex, 0);

            if (_context != IntPtr.Zero)
                ATIADLxx.ADL2_Main_Control_Destroy(_context);

            base.Close();
        }
    }
}
