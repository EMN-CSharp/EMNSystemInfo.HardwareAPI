// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Intel microarchitecture
    /// </summary>
    public enum IntelMicroarchitecture
    {
        Airmont,
        AlderLake,
        Atom,
        Broadwell,
        CannonLake,
        CometLake,
        Core,
        Goldmont,
        GoldmontPlus,
        Haswell,
        IceLake,
        IvyBridge,
        JasperLake,
        KabyLake,
        Nehalem,
        NetBurst,
        RocketLake,
        SandyBridge,
        Silvermont,
        Skylake,
        TigerLake,
        Tremont,
        Unknown
    }

    /// <summary>
    /// Power sensor type for Intel CPUs
    /// </summary>
    public enum IntelPowerSensorType
    {
        /// <summary>
        /// Power consumed by the entire package
        /// </summary>
        Package = 0,

        /// <summary>
        /// Power consumed by all the cores
        /// </summary>
        Cores = 1,

        /// <summary>
        /// Power consumed by the integrated GPU
        /// </summary>
        Graphics = 2,

        /// <summary>
        /// Power consumed by the CPU memory
        /// </summary>
        Memory = 3
    }

    /// <summary>
    /// Struct that represents an Intel power sensor
    /// </summary>
    public struct IntelPowerSensor
    {
        /// <summary>
        /// Gets the sensor value in watts (W)
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets the power sensor type
        /// </summary>
        public IntelPowerSensorType Type { get; set; }
    }

    /// <summary>
    /// Class that represents an individual Intel processor
    /// </summary>
    public sealed class IntelCPU : Processor
    {
        private double _busClock;
        private double _coreAvgTemp;
        private readonly double[] _coreClocks;
        private double _coreMax;
        private readonly CoreTemperature[] _coreTemperatures;
        private double _coreVoltage;
        private readonly double?[] _distToTjMaxTemperatures;

        private readonly uint[] _energyStatusMsrs = { MSR_PKG_ENERGY_STATUS, MSR_PP0_ENERY_STATUS, MSR_PP1_ENERY_STATUS, MSR_DRAM_ENERGY_STATUS };
        private readonly double _energyUnitMultiplier;
        private readonly uint[] _lastEnergyConsumed;
        private readonly DateTime[] _lastEnergyTime;

        private readonly IntelMicroarchitecture _microarchitecture;
        private CoreTemperature? _packageTemperature;
        private IntelPowerSensor?[] _powerSensors;
        private readonly double _timeStampCounterMultiplier;

        /// <summary>
        /// Gets the CPU microarchitecture
        /// </summary>
        public IntelMicroarchitecture Microarchitecture => _microarchitecture;

        public double EnergyUnitsMultiplier => _energyUnitMultiplier;

        /// <summary>
        /// Gets the bus clock speed, in megahertz (MHz).
        /// </summary>
        public double BusClock => _busClock;

        /// <summary>
        /// Gets the average temperature of all cores, in degrees Celsius (°C).
        /// </summary>
        public double CoreAverageTemperature => _coreAvgTemp;

        /// <summary>
        /// Gets the clock speed for all the cores. You can get the multiplier by dividing each value by the <see cref="BusClock"/> value.
        /// </summary>
        public double[] CoreFrequencyClocks => _coreClocks;

        /// <summary>
        /// Gets the maximum temperature reached by the package.
        /// </summary>
        public double CoreMaximumTemperature => _coreMax;

        /// <summary>
        /// Gets an array of <see cref="CoreTemperature"/>s for each core.
        /// </summary>
        public CoreTemperature[] CoreTemperatures => _coreTemperatures;

        /// <summary>
        /// Gets the CPU core voltage (VID), in volts (V).
        /// </summary>
        public double CoreVoltage => _coreVoltage;

        /// <summary>
        /// Gets the package temperature. This property is nullable.
        /// </summary>
        public CoreTemperature? PackageTemperature => _packageTemperature;

        /// <summary>
        /// Gets the CPU power sensors. Each element is nullable.
        /// </summary>
        public IntelPowerSensor?[] PowerSensors => _powerSensors;

        internal IntelCPU(int processorIndex, CPUID[][] cpuId) : base(processorIndex, cpuId)
        {
            Type = ProcessorType.IntelCPU;

            uint eax;

            // set tjMax
            double[] tjMax;
            switch (_family)
            {
                case 0x06:
                {
                    switch (_model)
                    {
                        case 0x0F: // Intel Core 2 (65nm)
                            _microarchitecture = IntelMicroarchitecture.Core;
                            switch (_stepping)
                            {
                                case 0x06: // B2
                                    switch (_coreCount)
                                    {
                                        case 2:
                                            tjMax = Doubles(80 + 10);
                                            break;
                                        case 4:
                                            tjMax = Doubles(90 + 10);
                                            break;
                                        default:
                                            tjMax = Doubles(85 + 10);
                                            break;
                                    }
                                    break;
                                case 0x0B: // G0
                                    tjMax = Doubles(90 + 10);
                                    break;
                                case 0x0D: // M0
                                    tjMax = Doubles(85 + 10);
                                    break;
                                default:
                                    tjMax = Doubles(85 + 10);
                                    break;
                            }
                            break;
                        case 0x17: // Intel Core 2 (45nm)
                            _microarchitecture = IntelMicroarchitecture.Core;
                            tjMax = Doubles(100);
                            break;
                        case 0x1C: // Intel Atom (45nm)
                            _microarchitecture = IntelMicroarchitecture.Atom;
                            switch (_stepping)
                            {
                                case 0x02: // C0
                                    tjMax = Doubles(90);
                                    break;
                                case 0x0A: // A0, B0
                                    tjMax = Doubles(100);
                                    break;
                                default:
                                    tjMax = Doubles(90);
                                    break;
                            }
                            break;
                        case 0x1A: // Intel Core i7 LGA1366 (45nm)
                        case 0x1E: // Intel Core i5, i7 LGA1156 (45nm)
                        case 0x1F: // Intel Core i5, i7
                        case 0x25: // Intel Core i3, i5, i7 LGA1156 (32nm)
                        case 0x2C: // Intel Core i7 LGA1366 (32nm) 6 Core
                        case 0x2E: // Intel Xeon Processor 7500 series (45nm)
                        case 0x2F: // Intel Xeon Processor (32nm)
                            _microarchitecture = IntelMicroarchitecture.Nehalem;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x2A: // Intel Core i5, i7 2xxx LGA1155 (32nm)
                        case 0x2D: // Next Generation Intel Xeon, i7 3xxx LGA2011 (32nm)
                            _microarchitecture = IntelMicroarchitecture.SandyBridge;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x3A: // Intel Core i5, i7 3xxx LGA1155 (22nm)
                        case 0x3E: // Intel Core i7 4xxx LGA2011 (22nm)
                            _microarchitecture = IntelMicroarchitecture.IvyBridge;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x3C: // Intel Core i5, i7 4xxx LGA1150 (22nm)
                        case 0x3F: // Intel Xeon E5-2600/1600 v3, Core i7-59xx
                        // LGA2011-v3, Haswell-E (22nm)
                        case 0x45: // Intel Core i5, i7 4xxxU (22nm)
                        case 0x46:
                            _microarchitecture = IntelMicroarchitecture.Haswell;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x3D: // Intel Core M-5xxx (14nm)
                        case 0x47: // Intel i5, i7 5xxx, Xeon E3-1200 v4 (14nm)
                        case 0x4F: // Intel Xeon E5-26xx v4
                        case 0x56: // Intel Xeon D-15xx
                            _microarchitecture = IntelMicroarchitecture.Broadwell;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x36: // Intel Atom S1xxx, D2xxx, N2xxx (32nm)
                            _microarchitecture = IntelMicroarchitecture.Atom;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x37: // Intel Atom E3xxx, Z3xxx (22nm)
                        case 0x4A:
                        case 0x4D: // Intel Atom C2xxx (22nm)
                        case 0x5A:
                        case 0x5D:
                            _microarchitecture = IntelMicroarchitecture.Silvermont;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x4E:
                        case 0x5E: // Intel Core i5, i7 6xxxx LGA1151 (14nm)
                        case 0x55: // Intel Core X i7, i9 7xxx LGA2066 (14nm)
                            _microarchitecture = IntelMicroarchitecture.Skylake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x4C: // Intel Airmont (Cherry Trail, Braswell)
                            _microarchitecture = IntelMicroarchitecture.Airmont;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x8E: // Intel Core i5, i7 7xxxx (14nm) (Kaby Lake) and 8xxxx (14nm++) (Coffee Lake)
                        case 0x9E:
                            _microarchitecture = IntelMicroarchitecture.KabyLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x5C: // Goldmont (Apollo Lake)
                        case 0x5F: // (Denverton)
                            _microarchitecture = IntelMicroarchitecture.Goldmont;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x7A: // Goldmont plus (Gemini Lake)
                            _microarchitecture = IntelMicroarchitecture.GoldmontPlus;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x66: // Intel Core i3 8xxx (10nm) (Cannon Lake)
                            _microarchitecture = IntelMicroarchitecture.CannonLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x7D: // Intel Core i3, i5, i7 10xxx (10nm) (Ice Lake)
                        case 0x7E:
                        case 0x6A: // Ice Lake server
                        case 0x6C:
                            _microarchitecture = IntelMicroarchitecture.IceLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0xA5:
                        case 0xA6: // Intel Core i3, i5, i7 10xxxU (14nm)
                            _microarchitecture = IntelMicroarchitecture.CometLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x86: // Tremont (10nm) (Elkhart Lake, Skyhawk Lake)
                            _microarchitecture = IntelMicroarchitecture.Tremont;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x8C: // Tiger Lake (10nm)
                        case 0x8D:
                            _microarchitecture = IntelMicroarchitecture.TigerLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x97: // Alder Lake (7nm)
                            _microarchitecture = IntelMicroarchitecture.AlderLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0x9C: // Jasper Lake (10nm)
                            _microarchitecture = IntelMicroarchitecture.JasperLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        case 0xA7: // Intel Core i5, i6, i7 11xxx (14nm) (Rocket Lake)
                            _microarchitecture = IntelMicroarchitecture.RocketLake;
                            tjMax = GetTjMaxFromMsr();
                            break;
                        default:
                            _microarchitecture = IntelMicroarchitecture.Unknown;
                            tjMax = Doubles(100);
                            break;
                    }
                }
                break;
                case 0x0F:
                {
                    switch (_model)
                    {
                        case 0x00: // Pentium 4 (180nm)
                        case 0x01: // Pentium 4 (130nm)
                        case 0x02: // Pentium 4 (130nm)
                        case 0x03: // Pentium 4, Celeron D (90nm)
                        case 0x04: // Pentium 4, Pentium D, Celeron D (90nm)
                        case 0x06: // Pentium 4, Pentium D, Celeron D (65nm)
                            _microarchitecture = IntelMicroarchitecture.NetBurst;
                            tjMax = Doubles(100);
                            break;
                        default:
                            _microarchitecture = IntelMicroarchitecture.Unknown;
                            tjMax = Doubles(100);
                            break;
                    }
                }
                break;
                default:
                    _microarchitecture = IntelMicroarchitecture.Unknown;
                    tjMax = Doubles(100);
                break;
            }
            // set timeStampCounterMultiplier
            switch (_microarchitecture)
            {
                case IntelMicroarchitecture.Atom:
                case IntelMicroarchitecture.Core:
                case IntelMicroarchitecture.NetBurst:
                {
                    if (Ring0.ReadMsr(IA32_PERF_STATUS, out uint _, out uint edx))
                    {
                        _timeStampCounterMultiplier = ((edx >> 8) & 0x1f) + 0.5 * ((edx >> 14) & 1);
                    }

                    break;
                }
                case IntelMicroarchitecture.Airmont:
                case IntelMicroarchitecture.AlderLake:
                case IntelMicroarchitecture.Broadwell:
                case IntelMicroarchitecture.CannonLake:
                case IntelMicroarchitecture.CometLake:
                case IntelMicroarchitecture.Goldmont:
                case IntelMicroarchitecture.GoldmontPlus:
                case IntelMicroarchitecture.Haswell:
                case IntelMicroarchitecture.IceLake:
                case IntelMicroarchitecture.IvyBridge:
                case IntelMicroarchitecture.JasperLake:
                case IntelMicroarchitecture.KabyLake:
                case IntelMicroarchitecture.Nehalem:
                case IntelMicroarchitecture.RocketLake:
                case IntelMicroarchitecture.SandyBridge:
                case IntelMicroarchitecture.Silvermont:
                case IntelMicroarchitecture.Skylake:
                case IntelMicroarchitecture.TigerLake:
                case IntelMicroarchitecture.Tremont:
                {
                    if (Ring0.ReadMsr(MSR_PLATFORM_INFO, out eax, out uint _))
                    {
                        _timeStampCounterMultiplier = (eax >> 8) & 0xff;
                    }
                }
                break;
                default:
                    _timeStampCounterMultiplier = 0;
                    break;
            }

            // check if processor supports a digital thermal sensor at core level
            if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 1) != 0 && _microarchitecture != IntelMicroarchitecture.Unknown)
            {
                _coreTemperatures = new CoreTemperature[_coreCount];
                for (int i = 0; i < _coreTemperatures.Length; i++)
                {
                    _coreTemperatures[i] = new CoreTemperature { TjMax = tjMax[i], TSlope = 1 };
                }
            }
            else
                _coreTemperatures = new CoreTemperature[0];

            // check if processor supports a digital thermal sensor at package level
            if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 0x40) != 0 && _microarchitecture != IntelMicroarchitecture.Unknown)
            {
                _packageTemperature = new CoreTemperature { TjMax = tjMax[0], TSlope = 1 };
            }

            // dist to tjmax sensor
            if (cpuId[0][0].Data.GetLength(0) > 6 && (cpuId[0][0].Data[6, 0] & 1) != 0 && _microarchitecture != IntelMicroarchitecture.Unknown)
            {
                _distToTjMaxTemperatures = new double?[_coreCount];
            }
            else
                _distToTjMaxTemperatures = new double?[0];

            _coreClocks = new double[_coreCount];

            if (_microarchitecture == IntelMicroarchitecture.Airmont ||
                _microarchitecture == IntelMicroarchitecture.AlderLake ||
                _microarchitecture == IntelMicroarchitecture.Broadwell ||
                _microarchitecture == IntelMicroarchitecture.CannonLake ||
                _microarchitecture == IntelMicroarchitecture.CometLake ||
                _microarchitecture == IntelMicroarchitecture.Goldmont ||
                _microarchitecture == IntelMicroarchitecture.GoldmontPlus ||
                _microarchitecture == IntelMicroarchitecture.Haswell ||
                _microarchitecture == IntelMicroarchitecture.IceLake ||
                _microarchitecture == IntelMicroarchitecture.IvyBridge ||
                _microarchitecture == IntelMicroarchitecture.JasperLake ||
                _microarchitecture == IntelMicroarchitecture.KabyLake ||
                _microarchitecture == IntelMicroarchitecture.RocketLake ||
                _microarchitecture == IntelMicroarchitecture.SandyBridge ||
                _microarchitecture == IntelMicroarchitecture.Silvermont ||
                _microarchitecture == IntelMicroarchitecture.Skylake ||
                _microarchitecture == IntelMicroarchitecture.TigerLake ||
                _microarchitecture == IntelMicroarchitecture.Tremont)
            {
                _powerSensors = new IntelPowerSensor?[_energyStatusMsrs.Length];
                _lastEnergyTime = new DateTime[_energyStatusMsrs.Length];
                _lastEnergyConsumed = new uint[_energyStatusMsrs.Length];

                if (Ring0.ReadMsr(MSR_RAPL_POWER_UNIT, out eax, out uint _))
                    switch (_microarchitecture)
                    {
                        case IntelMicroarchitecture.Silvermont:
                        case IntelMicroarchitecture.Airmont:
                            _energyUnitMultiplier = 1.0e-6f * (1 << (int)((eax >> 8) & 0x1F));
                            break;
                        default:
                            _energyUnitMultiplier = 1.0f / (1 << (int)((eax >> 8) & 0x1F));
                            break;
                    }

                if (_energyUnitMultiplier != 0)
                {
                    for (int i = 0; i < _energyStatusMsrs.Length; i++)
                    {
                        if (!Ring0.ReadMsr(_energyStatusMsrs[i], out eax, out uint _))
                            continue;

                        _lastEnergyTime[i] = DateTime.UtcNow;
                        _lastEnergyConsumed[i] = eax;
                        _powerSensors[i] = new IntelPowerSensor { Type = (IntelPowerSensorType)i };
                    }
                }
            }

            Update();
        }

        private double[] Doubles(double f)
        {
            double[] result = new double[_coreCount];
            for (int i = 0; i < _coreCount; i++)
                result[i] = f;

            return result;
        }

        private double[] GetTjMaxFromMsr()
        {
            double[] result = new double[_coreCount];
            for (int i = 0; i < _coreCount; i++)
            {
                if (Ring0.ReadMsr(IA32_TEMPERATURE_TARGET, out uint eax, out uint _, _cpuId[i][0].Affinity))
                    result[i] = (eax >> 16) & 0xFF;
                else
                    result[i] = 100;
            }

            return result;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            double coreMax = float.MinValue;
            double coreAvg = 0;
            uint eax = 0;

            for (int i = 0; i < _coreTemperatures.Length; i++)
            {
                // if reading is valid
                if (Ring0.ReadMsr(IA32_THERM_STATUS_MSR, out eax, out uint _, _cpuId[i][0].Affinity) && (eax & 0x80000000) != 0)
                {
                    // get the dist from tjMax from bits 22:16
                    double deltaT = (eax & 0x007F0000) >> 16;
                    double tjMax = _coreTemperatures[i].TjMax;
                    double tSlope = _coreTemperatures[i].TSlope;
                    _coreTemperatures[i].Value = tjMax - tSlope * deltaT;

                    coreAvg += (float)_coreTemperatures[i].Value;
                    if (coreMax < _coreTemperatures[i].Value)
                        coreMax = (float)_coreTemperatures[i].Value;

                    _distToTjMaxTemperatures[i] = deltaT;
                }
                else
                {
                    _coreTemperatures[i].Value = null;
                    _distToTjMaxTemperatures[i] = null;
                }
            }

            //calculate average cpu temperature over all cores
            if (coreMax != double.MinValue)
            {
                _coreMax = coreMax;
                coreAvg /= _coreTemperatures.Length;
                _coreAvgTemp = coreAvg;
            }

            if (_packageTemperature != null)
            {
                // if reading is valid
                if (Ring0.ReadMsr(IA32_PACKAGE_THERM_STATUS, out eax, out uint _, _cpuId[0][0].Affinity) && (eax & 0x80000000) != 0)
                {
                    // get the dist from tjMax from bits 22:16
                    double deltaT = (eax & 0x007F0000) >> 16;
                    double tjMax = _packageTemperature.GetValueOrDefault().TjMax;
                    double tSlope = _packageTemperature.GetValueOrDefault().TSlope;
                    _packageTemperature = new CoreTemperature { TjMax = tjMax, TSlope = tSlope, Value = tjMax - tSlope * deltaT };
                }
                else
                {
                    _packageTemperature = null;
                }
            }

            if (HasTimeStampCounter && _timeStampCounterMultiplier > 0)
            {
                double newBusClock = 0;
                for (int i = 0; i < _coreClocks.Length; i++)
                {
                    System.Threading.Thread.Sleep(1);
                    if (Ring0.ReadMsr(IA32_PERF_STATUS, out eax, out uint _, _cpuId[i][0].Affinity))
                    {
                        newBusClock = TimeStampCounterFrequency / _timeStampCounterMultiplier;
                        switch (_microarchitecture)
                        {
                            case IntelMicroarchitecture.Nehalem:
                            {
                                uint multiplier = eax & 0xff;
                                _coreClocks[i] = multiplier * newBusClock;
                                break;
                            }
                            case IntelMicroarchitecture.Airmont:
                            case IntelMicroarchitecture.AlderLake:
                            case IntelMicroarchitecture.Broadwell:
                            case IntelMicroarchitecture.CannonLake:
                            case IntelMicroarchitecture.CometLake:
                            case IntelMicroarchitecture.Goldmont:
                            case IntelMicroarchitecture.GoldmontPlus:
                            case IntelMicroarchitecture.Haswell:
                            case IntelMicroarchitecture.IceLake:
                            case IntelMicroarchitecture.IvyBridge:
                            case IntelMicroarchitecture.JasperLake:
                            case IntelMicroarchitecture.KabyLake:
                            case IntelMicroarchitecture.RocketLake:
                            case IntelMicroarchitecture.SandyBridge:
                            case IntelMicroarchitecture.Silvermont:
                            case IntelMicroarchitecture.Skylake:
                            case IntelMicroarchitecture.TigerLake:
                            case IntelMicroarchitecture.Tremont:
                            {
                                uint multiplier = (eax >> 8) & 0xff;
                                _coreClocks[i] = multiplier * newBusClock;
                                break;
                            }
                            default:
                            {
                                double multiplier = ((eax >> 8) & 0x1f) + 0.5 * ((eax >> 14) & 1);
                                _coreClocks[i] = multiplier * newBusClock;
                                break;
                            }
                        }
                    }
                    else
                    {
                        // if IA32_PERF_STATUS is not available, assume TSC frequency
                        _coreClocks[i] = TimeStampCounterFrequency;
                    }
                }

                if (newBusClock > 0)
                {
                    _busClock = newBusClock;
                }
            }

            if (_powerSensors != null)
            {
                uint index = 0;
                foreach (IntelPowerSensor? sensor in _powerSensors)
                {
                    if (sensor == null)
                        continue;

                    if (!Ring0.ReadMsr(_energyStatusMsrs[index], out eax, out uint _))
                        continue;


                    DateTime time = DateTime.UtcNow;
                    uint energyConsumed = eax;
                    float deltaTime = (float)(time - _lastEnergyTime[index]).TotalSeconds;
                    if (deltaTime < 0.01)
                        continue;


                    _powerSensors[index] = new IntelPowerSensor { Value = _energyUnitMultiplier * unchecked(energyConsumed - _lastEnergyConsumed[index]) / deltaTime, Type = sensor.GetValueOrDefault().Type };
                    _lastEnergyTime[index] = time;
                    _lastEnergyConsumed[index] = energyConsumed;
                    index++;
                }
            }

            if (Ring0.ReadMsr(IA32_PERF_STATUS, out eax, out uint _))
            {
                _coreVoltage = ((eax >> 32) & 0xFFFF) / (double)(1 << 13);
            }
        }

        private const uint IA32_PACKAGE_THERM_STATUS = 0x1B1;
        private const uint IA32_PERF_STATUS = 0x0198;
        private const uint IA32_TEMPERATURE_TARGET = 0x01A2;
        private const uint IA32_THERM_STATUS_MSR = 0x019C;

        private const uint MSR_DRAM_ENERGY_STATUS = 0x619;
        private const uint MSR_PKG_ENERGY_STATUS = 0x611;
        private const uint MSR_PLATFORM_INFO = 0xCE;
        private const uint MSR_PP0_ENERY_STATUS = 0x639;
        private const uint MSR_PP1_ENERY_STATUS = 0x641;

        private const uint MSR_RAPL_POWER_UNIT = 0x606;
    }
}
