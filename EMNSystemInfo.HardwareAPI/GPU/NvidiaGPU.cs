// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.NativeInterop;
using System;
using System.Collections.Generic;
using Microsoft.Win32;
using static EMNSystemInfo.HardwareAPI.NativeInterop.NVAPI;
using static EMNSystemInfo.HardwareAPI.NativeInterop.NvidiaML;

namespace EMNSystemInfo.HardwareAPI.GPU
{
    /// <summary>
    /// Temperature sensor type for NVIDIA GPUs
    /// </summary>
    public enum NvidiaTempSensorType
    {
        None = 0,

        /// <summary>
        /// Entire GPU temperature.
        /// </summary>
        Gpu = 1,

        /// <summary>
        /// GPU memory temperature
        /// </summary>
        Memory = 2,

        /// <summary>
        /// GPU power supply temperature
        /// </summary>
        PowerSupply = 4,

        /// <summary>
        /// GPU board temperature
        /// </summary>
        Board = 8,

        /// <summary>
        /// Visual computing board temperature
        /// </summary>
        VisualComputingBoard = 9,

        /// <summary>
        /// Visual computing inlet temperature
        /// </summary>
        VisualComputingInlet = 10,

        /// <summary>
        /// Visual computing outlet temperature
        /// </summary>
        VisualComputingOutlet = 11,

        /// <summary>
        /// Temperature of all the components
        /// </summary>
        All = 15,

        /// <summary>
        /// Temperature sensor type is unknown
        /// </summary>
        Unknown = -1
    }

    public enum NvidiaClockType
    {
        Graphics = 0,
        Memory = 4,
        Processor = 7,
        Video = 8,
        Undefined = MAX_CLOCKS_PER_GPU
    }

    public enum NvidiaLoadType
    {
        Gpu, // Core
        FrameBuffer, // Memory Controller
        VideoEngine, // Video Engine
        BusInterface // Bus
    }

    public enum NvidiaPowerType : uint
    {
        Gpu = 0,
        Board
    }

    public struct NvidiaTempSensor
    {
        public NvidiaTempSensorType Type { get; set; }
        public double Value { get; set; }
    }

    public struct NvidiaClockSensor
    {
        public NvidiaClockType Type { get; set; }
        public double Value { get; set; }
    }

    public struct NvidiaLoadSensor
    {
        public NvidiaLoadType Type { get; set; }
        public double Value { get; set; }
    }

    public struct NvidiaPowerSensor
    {
        public NvidiaPowerType Type { get; set; }
        public double Value { get; set; }
    }

    public sealed class NvidiaGPU : GPU
    {
        private readonly int _adapterIndex;
        private NvidiaClockSensor[] _clocks;
        private readonly int _clockVersion;
        private readonly NvDisplayHandle? _displayHandle;
        private double[] _fans;
        private readonly NvPhysicalGpuHandle _handle;
        private NvidiaLoadSensor[] _loads;
        private double _memoryFree;
        private double _memoryTotal;
        private double _memoryUsed;
        private readonly NvmlDevice? _nvmlDevice;
        private double? _pcieThroughputRx;
        private double? _pcieThroughputTx;
        private NvidiaPowerSensor[] _powers;
        private double _powerUsage;
        private NvidiaTempSensor[] _temperatures;
        private uint _thermalSensorsMask;
        private double? _hotSpotTemperature;
        private double? _memoryJunctionTemperature;

        public NvidiaClockSensor[] FrequencyClocks => _clocks;

        public double[] FanRPMs => _fans;

        public NvidiaLoadSensor[] Loads => _loads;

        public double MemoryFree => _memoryFree;

        public double MemoryTotal => _memoryTotal;

        public double MemoryUsed => _memoryUsed;

        public double? PCIeThroughputRX => _pcieThroughputRx;

        public double? PCIeThroughputTX => _pcieThroughputTx;

        public NvidiaPowerSensor[] PowerSensors => _powers;

        public double PowerUsage => _powerUsage;

        public NvidiaTempSensor[] Temperatures => _temperatures;

        public double? HotSpotTemperature => _hotSpotTemperature;

        public double? MemoryJunctionTemperature => _memoryJunctionTemperature;

        internal NvidiaGPU(int adapterIndex, NvPhysicalGpuHandle handle, NvDisplayHandle? displayHandle) : base()
        {
            Type = GPUType.NvidiaGPU;

            _adapterIndex = adapterIndex;
            _handle = handle;
            _displayHandle = displayHandle;

            bool hasBusId = NvAPI_GPU_GetBusId(handle, out uint busId) == NvStatus.OK;

            _gpuName = GetName(_handle);

            // Thermal settings.
            NvThermalSettings thermalSettings = GetThermalSettings(out NvStatus status);
            if (status == NvStatus.OK && thermalSettings.Count > 0)
            {
                _temperatures = new NvidiaTempSensor[thermalSettings.Count];

                for (int i = 0; i < thermalSettings.Count; i++)
                {
                    NvSensor sensor = thermalSettings.Sensor[i];

                    _temperatures[i] = new NvidiaTempSensor { Type = (NvidiaTempSensorType)sensor.Target };
                }
            }

            bool hasAnyThermalSensor = false;

            for (int thermalSensorsMaxBit = 0; thermalSensorsMaxBit < 32; thermalSensorsMaxBit++)
            {
                // Find the maximum thermal sensor mask value.
                _thermalSensorsMask = 1u << thermalSensorsMaxBit;

                GetThermalSensors(_thermalSensorsMask, out NvStatus thermalSensorsStatus);
                if (thermalSensorsStatus == NvStatus.OK)
                {
                    hasAnyThermalSensor = true;
                    continue;
                }

                _thermalSensorsMask--;
                break;
            }

            if (!hasAnyThermalSensor)
            {
                _thermalSensorsMask = 0;
            }

            // Clock frequencies.
            for (int clockVersion = 1; clockVersion <= 3; clockVersion++)
            {
                _clockVersion = clockVersion;

                NvGpuClockFrequencies clockFrequencies = GetClockFrequencies(out status);
                if (status == NvStatus.OK)
                {
                    var clocks = new List<NvidiaClockSensor>();
                    for (int i = 0; i < clockFrequencies.Clocks.Length; i++)
                    {
                        NvGpuClockFrequenciesDomain clock = clockFrequencies.Clocks[i];
                        if (clock.IsPresent && Enum.IsDefined(typeof(NvGpuPublicClockId), i))
                        {
                            clocks.Add(new NvidiaClockSensor { Type =(NvidiaClockType)i });
                        }
                    }

                    if (clocks.Count > 0)
                    {
                        _clocks = clocks.ToArray();

                        break;
                    }
                }
            }

            // Fans + controllers.
            NvFanCoolersStatus fanCoolers = GetFanCoolersStatus(out status);
            if (status == NvStatus.OK && fanCoolers.Count > 0)
            {
                _fans = new double[fanCoolers.Count];
            }
            else
            {
                GetTachReading(out status);
                if (status == NvStatus.OK)
                {
                    _fans = new double[1];
                }
            }

            // Load usages.
            NvDynamicPStatesInfo pStatesInfo = GetDynamicPstatesInfoEx(out status);
            if (status == NvStatus.OK)
            {
                var loads = new List<NvidiaLoadSensor>();
                for (int index = 0; index < pStatesInfo.Utilizations.Length; index++)
                {
                    NvDynamicPState load = pStatesInfo.Utilizations[index];
                    if (load.IsPresent && Enum.IsDefined(typeof(NvUtilizationDomain), index))
                    {
                        loads.Add(new NvidiaLoadSensor { Type = (NvidiaLoadType)index });
                    }
                }

                if (loads.Count > 0)
                {
                    _loads = loads.ToArray();
                }
            }
            else
            {
                NvUsages usages = GetUsages(out status);
                if (status == NvStatus.OK)
                {
                    var loads = new List<NvidiaLoadSensor>();
                    for (int index = 0; index < usages.Entries.Length; index++)
                    {
                        NvUsagesEntry load = usages.Entries[index];
                        if (load.IsPresent > 0 && Enum.IsDefined(typeof(NvUtilizationDomain), index))
                        {
                            loads.Add(new NvidiaLoadSensor { Type = (NvidiaLoadType)index });
                        }
                    }

                    if (loads.Count > 0)
                    {
                        _loads = loads.ToArray();
                    }
                }
            }

            // Power.
            NvPowerTopology powerTopology = GetPowerTopology(out NvStatus powerStatus);
            if (powerStatus == NvStatus.OK && powerTopology.Count > 0)
            {
                _powers = new NvidiaPowerSensor[powerTopology.Count];
                for (int i = 0; i < powerTopology.Count; i++)
                {
                    NvPowerTopologyEntry entry = powerTopology.Entries[i];
                    _powers[i] = new NvidiaPowerSensor { Type = (NvidiaPowerType)entry.Domain };
                }
            }

            if (NvidiaML.IsAvailable || Initialize())
            {
                if (hasBusId)
                    _nvmlDevice = NvmlDeviceGetHandleByPciBusId($" 0000:{busId:X2}:00.0") ?? NvmlDeviceGetHandleByIndex(_adapterIndex);
                else
                    _nvmlDevice = NvmlDeviceGetHandleByIndex(_adapterIndex);

                if (_nvmlDevice.HasValue)
                {
                    NvmlPciInfo? pciInfo = NvmlDeviceGetPciInfo(_nvmlDevice.Value);

                    if (pciInfo is { } pci)
                    {
                        string[] deviceIds = D3DDisplayDevice.GetDeviceIdentifiers();
                        if (deviceIds != null)
                        {
                            foreach (string deviceId in deviceIds)
                            {
                                if (deviceId.IndexOf("VEN_" + pci.pciVendorId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                    deviceId.IndexOf("DEV_" + pci.pciDeviceId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1 &&
                                    deviceId.IndexOf("SUBSYS_" + pci.pciSubSystemId.ToString("X"), StringComparison.OrdinalIgnoreCase) != -1)
                                {
                                    bool isMatch = false;

                                    string actualDeviceId = D3DDisplayDevice.GetActualDeviceIdentifier(deviceId);

                                    try
                                    {
                                        if (Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\nvlddmkm\Enum", adapterIndex.ToString(), null) is string adapterPnpId)
                                        {
                                            if (actualDeviceId.IndexOf(adapterPnpId, StringComparison.OrdinalIgnoreCase) != -1 ||
                                                adapterPnpId.IndexOf(actualDeviceId, StringComparison.OrdinalIgnoreCase) != -1)
                                            {
                                                isMatch = true;
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        // Ignored.
                                    }

                                    if (!isMatch)
                                    {
                                        try
                                        {
                                            string path = actualDeviceId;
                                            path = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Enum\" + path;

                                            if (Registry.GetValue(path, "LocationInformation", null) is string locationInformation)
                                            {
                                                // For example:
                                                // @System32\drivers\pci.sys,#65536;PCI bus %1, device %2, function %3;(38,0,0)

                                                int index = locationInformation.IndexOf('(');
                                                if (index != -1)
                                                {
                                                    index++;
                                                    int secondIndex = locationInformation.IndexOf(',', index);
                                                    if (secondIndex != -1)
                                                    {
                                                        string bus = locationInformation.Substring(index, secondIndex - index);

                                                        if (pci.bus.ToString() == bus)
                                                            isMatch = true;
                                                    }
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            // Ignored.
                                        }
                                    }

                                    if (isMatch)
                                    {
                                        _d3dDeviceId = deviceId;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Update();
        }

        public override void Update()
        {
            base.Update();

            NvStatus status;

            if (_temperatures is { Length: > 0 })
            {
                NvThermalSettings settings = GetThermalSettings(out status);
                // settings.Count is 0 when no valid data available, this happens when you try to read out this value with a high polling interval.
                if (status == NvStatus.OK && settings.Count > 0)
                {
                    for (int i = 0; i < _temperatures.Length; i++)
                    {
                        _temperatures[i].Value = settings.Sensor[i].CurrentTemp;
                    }
                }
            }

            if (_thermalSensorsMask > 0)
            {
                NvThermalSensors thermalSensors = GetThermalSensors(_thermalSensorsMask, out status);

                if (status == NvStatus.OK)
                {
                    _hotSpotTemperature = thermalSensors.Temperatures[1] / 256.0f;
                    _memoryJunctionTemperature = thermalSensors.Temperatures[9] / 256.0f;
                }
            }
            else
            {
                _hotSpotTemperature = null;
                _memoryJunctionTemperature = null;
            }

            if (_clocks is { Length: > 0 })
            {
                NvGpuClockFrequencies clockFrequencies = GetClockFrequencies(out status);
                if (status == NvStatus.OK)
                {
                    int current = 0;
                    for (int i = 0; i < clockFrequencies.Clocks.Length; i++)
                    {
                        NvGpuClockFrequenciesDomain clock = clockFrequencies.Clocks[i];
                        if (clock.IsPresent && Enum.IsDefined(typeof(NvGpuPublicClockId), i))
                            _clocks[current++].Value = clock.Frequency / 1000f;
                    }
                }
            }

            if (_fans is { Length: > 0 })
            {
                NvFanCoolersStatus fanCoolers = GetFanCoolersStatus(out status);
                if (status == NvStatus.OK && fanCoolers.Count > 0)
                {
                    for (int i = 0; i < fanCoolers.Count; i++)
                    {
                        NvFanCoolersStatusItem item = fanCoolers.Items[i];
                        _fans[i] = item.CurrentRpm;
                    }
                }
                else
                {
                    int tachReading = GetTachReading(out status);
                    if (status == NvStatus.OK)
                        _fans[0] = tachReading;
                }
            }

            if (_loads is { Length: > 0 })
            {
                NvDynamicPStatesInfo pStatesInfo = GetDynamicPstatesInfoEx(out status);
                if (status == NvStatus.OK)
                {
                    for (int index = 0; index < pStatesInfo.Utilizations.Length; index++)
                    {
                        NvDynamicPState load = pStatesInfo.Utilizations[index];
                        if (load.IsPresent && Enum.IsDefined(typeof(NvUtilizationDomain), index))
                            _loads[index].Value = load.Percentage;
                    }
                }
                else
                {
                    NvUsages usages = GetUsages(out status);
                    if (status == NvStatus.OK)
                    {
                        for (int index = 0; index < usages.Entries.Length; index++)
                        {
                            NvUsagesEntry load = usages.Entries[index];
                            if (load.IsPresent > 0 && Enum.IsDefined(typeof(NvUtilizationDomain), index))
                                _loads[index].Value = load.Percentage;
                        }
                    }
                }
            }

            if (_powers is { Length: > 0 })
            {
                NvPowerTopology powerTopology = GetPowerTopology(out status);
                if (status == NvStatus.OK && powerTopology.Count > 0)
                {
                    for (int i = 0; i < powerTopology.Count; i++)
                    {
                        NvPowerTopologyEntry entry = powerTopology.Entries[i];
                        _powers[i].Value = entry.PowerUsage / 1000f;
                    }
                }
            }

            if (_displayHandle != null)
            {
                NvMemoryInfo memoryInfo = GetMemoryInfo(out status);
                if (status == NvStatus.OK)
                {
                    uint free = memoryInfo.CurrentAvailableDedicatedVideoMemory;
                    uint total = memoryInfo.DedicatedVideoMemory;

                    _memoryTotal = total;

                    _memoryFree = free;

                    _memoryUsed = total - free;
                }
            }

            if (NvidiaML.IsAvailable && _nvmlDevice.HasValue)
            {
                int? result = NvmlDeviceGetPowerUsage(_nvmlDevice.Value);
                if (result.HasValue)
                {
                    _powerUsage = result.Value / 1000f;
                }

                // In MB/s, throughput sensors are passed as in KB/s.
                uint? rx = NvmlDeviceGetPcieThroughput(_nvmlDevice.Value, NvmlPcieUtilCounter.RxBytes);
                if (rx.HasValue)
                {
                    _pcieThroughputRx = rx * 1024d;
                }

                uint? tx = NvmlDeviceGetPcieThroughput(_nvmlDevice.Value, NvmlPcieUtilCounter.TxBytes);
                if (tx.HasValue)
                {
                    _pcieThroughputTx = tx * 1024d;
                }
            }
        }

        private static string GetName(NvPhysicalGpuHandle handle)
        {
            if (NvAPI_GPU_GetFullName(handle, out string gpuName) == NvStatus.OK)
            {
                string name = gpuName.Trim();
                return name.StartsWith("NVIDIA", StringComparison.OrdinalIgnoreCase) ? name : "NVIDIA " + name;
            }

            return "NVIDIA";
        }

        private NvMemoryInfo GetMemoryInfo(out NvStatus status)
        {
            if (NvAPI_GPU_GetMemoryInfo == null || _displayHandle == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvMemoryInfo memoryInfo = new() { Version = (uint)MAKE_NVAPI_VERSION<NvMemoryInfo>(2) };

            status = NvAPI_GPU_GetMemoryInfo(_displayHandle.Value, ref memoryInfo);
            return status == NvStatus.OK ? memoryInfo : default;
        }

        private NvGpuClockFrequencies GetClockFrequencies(out NvStatus status)
        {
            if (NvAPI_GPU_GetAllClockFrequencies == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvGpuClockFrequencies clockFrequencies = new() { Version = (uint)MAKE_NVAPI_VERSION<NvGpuClockFrequencies>(_clockVersion) };

            status = NvAPI_GPU_GetAllClockFrequencies(_handle, ref clockFrequencies);
            return status == NvStatus.OK ? clockFrequencies : default;
        }

        private NvThermalSettings GetThermalSettings(out NvStatus status)
        {
            if (NvAPI_GPU_GetThermalSettings == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvThermalSettings settings = new() { Version = (uint)MAKE_NVAPI_VERSION<NvThermalSettings>(1), Count = MAX_THERMAL_SENSORS_PER_GPU, };

            status = NvAPI_GPU_GetThermalSettings(_handle, (int)NvThermalTarget.All, ref settings);
            return status == NvStatus.OK ? settings : default;
        }

        private NvThermalSensors GetThermalSensors(uint mask, out NvStatus status)
        {
            if (NvAPI_GPU_ThermalGetSensors == null)
            {
                status = NvStatus.Error;
                return default;
            }

            var thermalSensors = new NvThermalSensors()
            {
                Version = (uint)MAKE_NVAPI_VERSION<NvThermalSensors>(2),
                Mask = mask
            };

            status = NvAPI_GPU_ThermalGetSensors(_handle, ref thermalSensors);
            return status == NvStatus.OK ? thermalSensors : default;
        }

        private NvFanCoolersStatus GetFanCoolersStatus(out NvStatus status)
        {
            if (NvAPI_GPU_ClientFanCoolersGetStatus == null)
            {
                status = NvStatus.Error;
                return default;
            }

            var coolers = new NvFanCoolersStatus
            {
                Version = (uint)MAKE_NVAPI_VERSION<NvFanCoolersStatus>(1),
                Items = new NvFanCoolersStatusItem[MAX_FAN_COOLERS_STATUS_ITEMS]
            };

            status = NvAPI_GPU_ClientFanCoolersGetStatus(_handle, ref coolers);
            return status == NvStatus.OK ? coolers : default;
        }

        private NvDynamicPStatesInfo GetDynamicPstatesInfoEx(out NvStatus status)
        {
            if (NvAPI_GPU_GetDynamicPstatesInfoEx == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvDynamicPStatesInfo pStatesInfo = new()
            {
                Version = (uint)MAKE_NVAPI_VERSION<NvDynamicPStatesInfo>(1),
                Utilizations = new NvDynamicPState[MAX_GPU_UTILIZATIONS]
            };

            status = NvAPI_GPU_GetDynamicPstatesInfoEx(_handle, ref pStatesInfo);
            return status == NvStatus.OK ? pStatesInfo : default;
        }

        private NvUsages GetUsages(out NvStatus status)
        {
            if (NvAPI_GPU_GetUsages == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvUsages usages = new() { Version = (uint)MAKE_NVAPI_VERSION<NvUsages>(1) };

            status = NvAPI_GPU_GetUsages(_handle, ref usages);
            return status == NvStatus.OK ? usages : default;
        }

        private NvPowerTopology GetPowerTopology(out NvStatus status)
        {
            if (NvAPI_GPU_ClientPowerTopologyGetStatus == null)
            {
                status = NvStatus.Error;
                return default;
            }

            NvPowerTopology powerTopology = new() { Version = MAKE_NVAPI_VERSION<NvPowerTopology>(1) };

            status = NvAPI_GPU_ClientPowerTopologyGetStatus(_handle, ref powerTopology);
            return status == NvStatus.OK ? powerTopology : default;
        }

        private int GetTachReading(out NvStatus status)
        {
            if (NvAPI_GPU_GetTachReading == null)
            {
                status = NvStatus.Error;
                return default;
            }

            status = NvAPI_GPU_GetTachReading(_handle, out int value);
            return value;
        }
    }
}
