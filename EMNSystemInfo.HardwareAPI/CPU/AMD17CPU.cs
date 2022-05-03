// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.CPU
{
    /// <summary>
    /// Class that represents an individual AMD CPU family 17h-19h
    /// </summary>
    public sealed class AMD17CPU : AMDCPU
    {
        private readonly Processor _processor;
        private readonly RyzenSMU _smu;

        /// <summary>
        /// Gets the bus clock speed, in megahertz (MHz).
        /// </summary>
        public double BusClock => _processor.BusClock;

        /// <summary>
        /// Gets the CPU code name
        /// </summary>
        public AMDCPUCodeName CodeName => _smu._cpuCodeName;

        /// <summary>
        /// Gets the CPU core voltage (VID), in volts (V).
        /// </summary>
        public double CoreVoltage => _processor.CoreVoltage;

        /// <summary>
        /// Gets the power consumed by the entire package, in watts (W)
        /// </summary>
        public double PackagePower => _processor.PackagePower;

        /// <summary>
        /// Gets the SoC voltage (VID), in volts (V). This property is nullable.
        /// </summary>
        public double? SoCVoltage => _processor.SoCVoltage;

        /// <summary>
        /// Gets an array of <see cref="CoreTemperature"/>s for each CCD. Each element is nullable.
        /// </summary>
        public CoreTemperature?[] CCDTemperatures => _processor.CCDTemperatures;

        /// <summary>
        /// Gets the core temperature (Tctl), in degrees Celsius (°C).
        /// </summary>
        public CoreTemperature CoreTemperatureTctl => _processor.CoreTemperatureTctl;

        /// <summary>
        /// Gets the core temperature (Tctl/Tdie), in degrees Celsius (°C).
        /// </summary>
        public CoreTemperature CoreTemperatureTctlTdie => _processor.CoreTemperatureTctlTdie;

        /// <summary>
        /// Gets the core temperature (Tdie), in degrees Celsius (°C).
        /// </summary>
        public CoreTemperature CoreTemperatureTdie => _processor.CoreTemperatureTdie;

        /// <summary>
        /// Gets the dictionary of sensors from the SMU table. The sensor value is nullable.
        /// </summary>
        public Dictionary<SMUSensor, double?> SMUSensors => _processor.SMUSensors;

        internal AMD17CPU(int processorIndex, CPUID[][] cpuId) : base(processorIndex, cpuId)
        {
            Type = ProcessorType.AMD17CPU;
            
            _smu = new RyzenSMU(_family, _model, _packageType);

            // Add all numa nodes.
            // Register ..1E_2, [10:8] + 1
            _processor = new Processor(this);

            // Add all numa nodes.
            int coreId = 0;
            int lastCoreId = -1; // Invalid id.

            // Ryzen 3000's skip some core ids.
            // So start at 1 and count upwards when the read core changes.
            foreach (CPUID[] cpu in cpuId.OrderBy(x => x[0].ExtData[0x1e, 1] & 0xFF))
            {
                CPUID thread = cpu[0];

                // CPUID_Fn8000001E_EBX, Register ..1E_1, [7:0]
                // threads per core =  CPUID_Fn8000001E_EBX[15:8] + 1
                // CoreId: core ID =  CPUID_Fn8000001E_EBX[7:0]
                int coreIdRead = (int)(thread.ExtData[0x1e, 1] & 0xff);

                // CPUID_Fn8000001E_ECX, Node Identifiers, Register ..1E_2
                // NodesPerProcessor =  CPUID_Fn8000001E_ECX[10:8]
                // nodeID =  CPUID_Fn8000001E_ECX[7:0]
                int nodeId = (int)(thread.ExtData[0x1e, 2] & 0xff);

                if (coreIdRead != lastCoreId)
                {
                    coreId++;
                }

                lastCoreId = coreIdRead;

                _processor.AppendThread(thread, nodeId, coreId);
            }

            Update();
        }
        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            _processor.UpdateSensors();

            foreach (NumaNode node in _processor.Nodes)
            {
                NumaNode.UpdateSensors();

                foreach (Core c in node.Cores)
                {
                    c.UpdateSensors();
                }
            }
        }

        private class Processor
        {
            private double _busClock;
            private CoreTemperature?[] _ccdTemperatures;
            private CoreTemperature _coreTemperatureTctl;
            private CoreTemperature _coreTemperatureTctlTdie;
            private CoreTemperature _coreTemperatureTdie;
            private double _coreVoltage;
            private readonly AMD17CPU _cpu;
            private double _packagePower;
            private readonly Dictionary<KeyValuePair<uint, SMUSensor>, double?> _smuSensors = new();
            private double? _socVoltage;
            
            private CoreTemperature? _ccdsAverageTemperature;
            private CoreTemperature? _ccdsMaxTemperature;
            private DateTime _lastPwrTime = new(0);
            private uint _lastPwrValue;

            public double BusClock => _busClock;

            public double CoreVoltage => _coreVoltage;

            public double PackagePower => _packagePower;

            public double? SoCVoltage => _socVoltage;

            public CoreTemperature?[] CCDTemperatures => _ccdTemperatures;

            public CoreTemperature CoreTemperatureTctl => _coreTemperatureTctl;

            public CoreTemperature CoreTemperatureTctlTdie => _coreTemperatureTctlTdie;

            public CoreTemperature CoreTemperatureTdie => _coreTemperatureTdie;

            public Dictionary<SMUSensor, double?> SMUSensors
            {
                get
                {
                    Dictionary<SMUSensor, double?> retval = new();
                    foreach (var sensor in _smuSensors)
                    {
                        retval.Add(sensor.Key.Value, sensor.Value);
                    }

                    return retval;
                }
            }

            public Processor(AMD17CPU hardware)
            {
                _cpu = hardware;

                _ccdTemperatures = new CoreTemperature?[8]; // Hardcoded until there's a way to get max CCDs.

                foreach (KeyValuePair<uint, SMUSensor> sensor in _cpu._smu.GetPmTableStructure())
                {
                    _smuSensors.Add(sensor, 0);
                }
            }

            public List<NumaNode> Nodes { get; } = new();

            public void UpdateSensors()
            {
                NumaNode node = Nodes[0];
                Core core = node?.Cores[0];
                CPUID cpuId = core?.Threads[0];

                if (cpuId == null)
                    return;


                GroupAffinity previousAffinity = ThreadAffinity.Set(cpuId.Affinity);

                // MSRC001_0299
                // TU [19:16]
                // ESU [12:8] -> Unit 15.3 micro Joule per increment
                // PU [3:0]
                Ring0.ReadMsr(MSR_PWR_UNIT, out uint _, out uint _);

                // MSRC001_029B
                // total_energy [31:0]
                DateTime sampleTime = DateTime.Now;
                Ring0.ReadMsr(MSR_PKG_ENERGY_STAT, out uint eax, out _);

                uint totalEnergy = eax;

                uint smuSvi0Tfn = 0;
                uint smuSvi0TelPlane0 = 0;
                uint smuSvi0TelPlane1 = 0;

                if (Ring0.WaitPciBusMutex(10))
                {
                    // THM_TCON_CUR_TMP
                    // CUR_TEMP [31:21]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_THM_TCON_CUR_TMP);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint temperature);

                    // SVI0_TFN_PLANE0 [0]
                    // SVI0_TFN_PLANE1 [1]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M01H_SVI + 0x8);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0Tfn);

                    bool supportsPerCcdTemperatures = false;

                    // TODO: find a better way because these will probably keep changing in the future.

                    uint sviPlane0Offset;
                    uint sviPlane1Offset;
                    switch (cpuId.Model)
                    {
                        case 0x31: // Threadripper 3000.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0x14;
                            sviPlane1Offset = F17H_M01H_SVI + 0x10;
                            supportsPerCcdTemperatures = true;
                            break;
                        }
                        case 0x71: // Zen 2.
                        case 0x21: // Zen 3.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0x10;
                            sviPlane1Offset = F17H_M01H_SVI + 0xC;
                            supportsPerCcdTemperatures = true;
                            break;
                        }
                        default: // Zen and Zen+.
                        {
                            sviPlane0Offset = F17H_M01H_SVI + 0xC;
                            sviPlane1Offset = F17H_M01H_SVI + 0x10;
                            break;
                        }
                    }

                    // SVI0_PLANE0_VDDCOR [24:16]
                    // SVI0_PLANE0_IDDCOR [7:0]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane0Offset);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane0);

                    // SVI0_PLANE1_VDDCOR [24:16]
                    // SVI0_PLANE1_IDDCOR [7:0]
                    Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, sviPlane1Offset);
                    Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out smuSvi0TelPlane1);

                    ThreadAffinity.Set(previousAffinity);

                    // power consumption
                    // power.Value = (float) ((double)pu * 0.125);
                    // esu = 15.3 micro Joule per increment
                    if (_lastPwrTime.Ticks == 0)
                    {
                        _lastPwrTime = sampleTime;
                        _lastPwrValue = totalEnergy;
                    }

                    // ticks diff
                    TimeSpan time = sampleTime - _lastPwrTime;
                    long pwr;
                    if (_lastPwrValue <= totalEnergy)
                        pwr = totalEnergy - _lastPwrValue;
                    else
                        pwr = (0xffffffff - _lastPwrValue) + totalEnergy;

                    // update for next sample
                    _lastPwrTime = sampleTime;
                    _lastPwrValue = totalEnergy;

                    double energy = 15.3e-6 * pwr;
                    energy /= time.TotalSeconds;

                    if (!double.IsNaN(energy))
                        _packagePower = (float)energy;

                    // current temp Bit [31:21]
                    // If bit 19 of the Temperature Control register is set, there is an additional offset of 49 degrees C.
                    bool tempOffsetFlag = (temperature & F17H_TEMP_OFFSET_FLAG) != 0;
                    temperature = (temperature >> 21) * 125;

                    float offset = 0.0f;

                    // Offset table: https://github.com/torvalds/linux/blob/master/drivers/hwmon/k10temp.c#L78
                    if (string.IsNullOrWhiteSpace(cpuId.Name))
                        offset = 0;
                    else if (cpuId.Name.Contains("1600X") || cpuId.Name.Contains("1700X") || cpuId.Name.Contains("1800X"))
                        offset = -20.0f;
                    else if (cpuId.Name.Contains("Threadripper 19") || cpuId.Name.Contains("Threadripper 29"))
                        offset = -27.0f;
                    else if (cpuId.Name.Contains("2700X"))
                        offset = -10.0f;

                    float t = temperature * 0.001f;
                    if (tempOffsetFlag)
                        t += -49.0f;

                    if (offset < 0)
                    {
                        _coreTemperatureTctl.Value = t;
                        _coreTemperatureTdie.Value = t + offset;
                    }
                    else
                    {
                        // Zen 2 doesn't have an offset so Tdie and Tctl are the same.
                        _coreTemperatureTctlTdie.Value = t;
                    }

                    // Tested only on R5 3600 & Threadripper 3960X.
                    if (supportsPerCcdTemperatures)
                    {
                        for (uint i = 0; i < _ccdTemperatures.Length; i++)
                        {
                            Ring0.WritePciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER, F17H_M70H_CCD1_TEMP + (i * 0x4));
                            Ring0.ReadPciConfig(0x00, FAMILY_17H_PCI_CONTROL_REGISTER + 4, out uint ccdRawTemp);

                            ccdRawTemp &= 0xFFF;
                            float ccdTemp = ((ccdRawTemp * 125) - 305000) * 0.001f;
                            if (ccdRawTemp > 0 && ccdTemp < 125) // Zen 2 reports 95 degrees C max, but it might exceed that.
                            {
                                if (_ccdTemperatures[i] == null)
                                {
                                    _ccdTemperatures[i] = new();
                                }

                                _ccdTemperatures[i] = new() { Value = ccdTemp };
                            }
                        }

                        CoreTemperature?[] activeCcds = _ccdTemperatures.Where(x => x != null).ToArray();
                        if (activeCcds.Length > 1)
                        {
                            // No need to get the max / average ccds temp if there is only one CCD.

                            if (_ccdsMaxTemperature == null)
                            {
                                _ccdsMaxTemperature = new();
                            }

                            if (_ccdsAverageTemperature == null)
                            {
                                _ccdsMaxTemperature = new();
                            }

                            _ccdsMaxTemperature = activeCcds.Max(x => x.Value);
                            _ccdsAverageTemperature = new() { Value = activeCcds.Average(x => x.Value.Value) };
                        }
                    }

                    Ring0.ReleasePciBusMutex();
                }

                // voltage
                const double vidStep = 0.00625;
                double vcc;
                uint svi0PlaneXVddCor;

                // Core (0x01).
                if ((smuSvi0Tfn & 0x01) == 0)
                {
                    svi0PlaneXVddCor = (smuSvi0TelPlane0 >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _coreVoltage = vcc;
                }

                // SoC (0x02), not every Zen cpu has this voltage.
                if (cpuId.Model == 0x11 || cpuId.Model == 0x21 || cpuId.Model == 0x71 || cpuId.Model == 0x31 || (smuSvi0Tfn & 0x02) == 0)
                {
                    svi0PlaneXVddCor = (smuSvi0TelPlane1 >> 16) & 0xff;
                    vcc = 1.550 - vidStep * svi0PlaneXVddCor;
                    _socVoltage = vcc;
                }

                double timeStampCounterMultiplier = GetTimeStampCounterMultiplier();
                if (timeStampCounterMultiplier > 0)
                {
                    _busClock = _cpu.TimeStampCounterFrequency / timeStampCounterMultiplier;
                }

                if (_cpu._smu.IsPmTableLayoutDefined())
                {
                    float[] smuData = _cpu._smu.GetPmTable();

                    for (int i = 0; i < _smuSensors.Count; i++)
                    {
                        var sensor = _smuSensors.ElementAt(i);
                        if (smuData.Length > sensor.Key.Key)
                        {
                            _smuSensors[sensor.Key] = smuData[sensor.Key.Key] * sensor.Key.Value.Scale;
                        }
                    }
                }
            }

            private double GetTimeStampCounterMultiplier()
            {
                Ring0.ReadMsr(MSR_PSTATE_0, out uint eax, out _);
                uint cpuDfsId = (eax >> 8) & 0x3f;
                uint cpuFid = eax & 0xff;
                return 2.0 * cpuFid / cpuDfsId;
            }

            public void AppendThread(CPUID thread, int numaId, int coreId)
            {
                NumaNode node = null;
                foreach (NumaNode n in Nodes)
                {
                    if (n.NodeId == numaId)
                    {
                        node = n;
                        break;
                    }
                }

                if (node == null)
                {
                    node = new NumaNode(_cpu, numaId);
                    Nodes.Add(node);
                }

                if (thread != null)
                    node.AppendThread(thread, coreId);
            }
        }

        private class NumaNode
        {
            private readonly AMD17CPU _cpu;

            public NumaNode(AMD17CPU cpu, int id)
            {
                Cores = new List<Core>();
                NodeId = id;
                _cpu = cpu;
            }

            public List<Core> Cores { get; }

            public int NodeId { get; }

            public void AppendThread(CPUID thread, int coreId)
            {
                Core core = null;
                foreach (Core c in Cores)
                {
                    if (c.CoreId == coreId)
                        core = c;
                }

                if (core == null)
                {
                    core = new Core(_cpu, coreId);
                    Cores.Add(core);
                }

                if (thread != null)
                    core.Threads.Add(thread);
            }

            public static void UpdateSensors()
            { }
        }

        private class Core
        {
            private double _clock;
            private readonly AMD17CPU _cpu;
            private double _multiplier;
            private double _power;
            private double _vcore;
            private double _busSpeed;
            private DateTime _lastPwrTime = new(0);
            private uint _lastPwrValue;

            public Core(AMD17CPU cpu, int id)
            {
                _cpu = cpu;
                Threads = new List<CPUID>();
                CoreId = id;
            }

            public int CoreId { get; }

            public List<CPUID> Threads { get; }

            public void UpdateSensors()
            {
                // CPUID cpu = threads.FirstOrDefault();
                CPUID cpu = Threads[0];
                if (cpu == null)
                    return;


                var previousAffinity = ThreadAffinity.Set(cpu.Affinity);

                // MSRC001_0299
                // TU [19:16]
                // ESU [12:8] -> Unit 15.3 micro Joule per increment
                // PU [3:0]
                Ring0.ReadMsr(MSR_PWR_UNIT, out _, out _);

                // MSRC001_029A
                // total_energy [31:0]
                DateTime sampleTime = DateTime.Now;
                uint eax;
                Ring0.ReadMsr(MSR_CORE_ENERGY_STAT, out eax, out _);
                uint totalEnergy = eax;

                // MSRC001_0293
                // CurHwPstate [24:22]
                // CurCpuVid [21:14]
                // CurCpuDfsId [13:8]
                // CurCpuFid [7:0]
                Ring0.ReadMsr(MSR_HARDWARE_PSTATE_STATUS, out eax, out _);
                int curCpuVid = (int)((eax >> 14) & 0xff);
                int curCpuDfsId = (int)((eax >> 8) & 0x3f);
                int curCpuFid = (int)(eax & 0xff);

                // MSRC001_0064 + x
                // IddDiv [31:30]
                // IddValue [29:22]
                // CpuVid [21:14]
                // CpuDfsId [13:8]
                // CpuFid [7:0]
                // Ring0.ReadMsr(MSR_PSTATE_0 + (uint)CurHwPstate, out eax, out edx);
                // int IddDiv = (int)((eax >> 30) & 0x03);
                // int IddValue = (int)((eax >> 22) & 0xff);
                // int CpuVid = (int)((eax >> 14) & 0xff);
                ThreadAffinity.Set(previousAffinity);

                // clock
                // CoreCOF is (Core::X86::Msr::PStateDef[CpuFid[7:0]] / Core::X86::Msr::PStateDef[CpuDfsId]) * 200
                double clock = 200.0;
                _busSpeed = _cpu.BusClock;
                if (_busSpeed > 0)
                    clock = _busSpeed * 2;

                _clock = curCpuFid / (double)curCpuDfsId * clock;

                // multiplier
                _multiplier = curCpuFid / (double)curCpuDfsId * 2.0;

                // Voltage
                const double vidStep = 0.00625;
                double vcc = 1.550 - vidStep * curCpuVid;
                _vcore = vcc;

                // power consumption
                // power.Value = (float) ((double)pu * 0.125);
                // esu = 15.3 micro Joule per increment
                if (_lastPwrTime.Ticks == 0)
                {
                    _lastPwrTime = sampleTime;
                    _lastPwrValue = totalEnergy;
                }

                // ticks diff
                TimeSpan time = sampleTime - _lastPwrTime;
                long pwr;
                if (_lastPwrValue <= totalEnergy)
                    pwr = totalEnergy - _lastPwrValue;
                else
                    pwr = (0xffffffff - _lastPwrValue) + totalEnergy;

                // update for next sample
                _lastPwrTime = sampleTime;
                _lastPwrValue = totalEnergy;

                double energy = 15.3e-6 * pwr;
                energy /= time.TotalSeconds;

                if (!double.IsNaN(energy))
                    _power = energy;
            }
        }

        // ReSharper disable InconsistentNaming
        private const uint COFVID_STATUS = 0xC0010071;
        private const uint F17H_M01H_SVI = 0x0005A000;
        private const uint F17H_M01H_THM_TCON_CUR_TMP = 0x00059800;
        private const uint F17H_M70H_CCD1_TEMP = 0x00059954;
        private const uint F17H_TEMP_OFFSET_FLAG = 0x80000;
        private const uint FAMILY_17H_PCI_CONTROL_REGISTER = 0x60;
        private const uint HWCR = 0xC0010015;
        private const uint MSR_CORE_ENERGY_STAT = 0xC001029A;
        private const uint MSR_HARDWARE_PSTATE_STATUS = 0xC0010293;
        private const uint MSR_PKG_ENERGY_STAT = 0xC001029B;
        private const uint MSR_PSTATE_0 = 0xC0010064;
        private const uint MSR_PWR_UNIT = 0xC0010299;
        private const uint PERF_CTL_0 = 0xC0010000;

        private const uint PERF_CTR_0 = 0xC0010004;
        // ReSharper restore InconsistentNaming
    }
}
