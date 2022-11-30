// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Linq;

namespace EMNSystemInfo.HardwareAPI.LPC.EC
{
    /// <summary>
    /// Class that represents an Embedded Controller sensor
    /// </summary>
    public class ECSensor
    {
        public ECSensorType Type { get; internal set; }
        public double? Value { get; internal set; }
    }

    /// <summary>
    /// Embedded Controller sensor types.
    /// </summary>
    public enum ECSensorType
    {
        /// <summary>Chipset temperature [°C]</summary>
        TempChipset,
        /// <summary>CPU temperature [°C]</summary>
        TempCPU,
        /// <summary>motherboard temperature [°C]</summary>
        TempMB,
        /// <summary>"T_Sensor" temperature sensor reading [°C]</summary>
        TempTSensor,
        /// <summary>VRM temperature [°C]</summary>
        TempVrm,
        /// <summary>CPU_Opt fan [RPM]</summary>
        FanCPUOpt,
        /// <summary>VRM heat sink fan [RPM]</summary>
        FanVrmHS,
        /// <summary>Chipset fan [RPM]</summary>
        FanChipset,
        /// <summary>Water flow sensor reading [L/h]</summary>
        FanWaterFlow,
        /// <summary>CPU current [A]</summary>
        CurrCPU,
        /// <summary>"Water_In" temperature sensor reading [°C]</summary>
        TempWaterIn,
        /// <summary>"Water_Out" temperature sensor reading [°C]</summary>
        TempWaterOut,
        Max
    };

    /// <summary>
    /// Class that represents an individual Embedded Controller LPC chip
    /// </summary>
    public abstract class EmbeddedController : LPC
    {
        private static readonly Dictionary<ECSensorType, EmbeddedControllerSource> _knownSensors = new()
        {
            { ECSensorType.TempChipset, new EmbeddedControllerSource(ECSensorType.TempChipset, 0x003a) },
            { ECSensorType.TempCPU, new EmbeddedControllerSource(ECSensorType.TempCPU, 0x003b) },
            { ECSensorType.TempMB, new EmbeddedControllerSource(ECSensorType.TempMB, 0x003c) },
            { ECSensorType.TempTSensor, new EmbeddedControllerSource(ECSensorType.TempTSensor, 0x003d, blank: -40) },
            { ECSensorType.TempVrm, new EmbeddedControllerSource(ECSensorType.TempVrm, 0x003e) },
            { ECSensorType.FanCPUOpt, new EmbeddedControllerSource(ECSensorType.FanCPUOpt, 0x00b0, 2) },
            { ECSensorType.FanVrmHS, new EmbeddedControllerSource(ECSensorType.FanVrmHS, 0x00b2, 2) },
            { ECSensorType.FanChipset, new EmbeddedControllerSource(ECSensorType.FanChipset, 0x00b4, 2) },
            // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
            { ECSensorType.FanWaterFlow, new EmbeddedControllerSource(ECSensorType.FanWaterFlow, 0x00bc, 2, factor: 1.0f / 42f * 60f) },
            { ECSensorType.CurrCPU, new EmbeddedControllerSource(ECSensorType.CurrCPU, 0x00f4) },
            { ECSensorType.TempWaterIn, new EmbeddedControllerSource(ECSensorType.TempWaterIn, 0x0100, blank: -40) },
            { ECSensorType.TempWaterOut, new EmbeddedControllerSource(ECSensorType.TempWaterOut, 0x0101, blank: -40) },
        };

        private static readonly Dictionary<MotherboardModel, ECSensorType[]> _boardSensors = new()
        {
            {
                MotherboardModel.PRIME_X570_PRO,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempVrm, ECSensorType.TempTSensor, ECSensorType.FanChipset }
            },
            {
                MotherboardModel.SLEEPY_IL,
                new ECSensorType[] { ECSensorType.FanCPUOpt }
            },
            {
                MotherboardModel.PRO_WS_X570_ACE,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB ,
                    ECSensorType.TempVrm, ECSensorType.FanChipset, ECSensorType.CurrCPU}
            },
            {
                MotherboardModel.ROG_CROSSHAIR_VIII_HERO,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.TempWaterIn, ECSensorType.TempWaterOut,
                    ECSensorType.FanCPUOpt, ECSensorType.FanChipset, ECSensorType.FanWaterFlow, ECSensorType.CurrCPU}
            },
            {
                MotherboardModel.ROG_CROSSHAIR_VIII_HERO_WIFI,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.TempWaterIn, ECSensorType.TempWaterOut,
                    ECSensorType.FanCPUOpt, ECSensorType.FanChipset, ECSensorType.FanWaterFlow, ECSensorType.CurrCPU}
            },
            {
                MotherboardModel.ROG_CROSSHAIR_VIII_DARK_HERO,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.TempWaterIn, ECSensorType.TempWaterOut,
                    ECSensorType.FanCPUOpt, ECSensorType.FanWaterFlow, ECSensorType.CurrCPU
                }
            },
            {
                MotherboardModel.CROSSHAIR_III_FORMULA,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm,
                    ECSensorType.FanCPUOpt, ECSensorType.FanChipset, ECSensorType.CurrCPU }
            },
            {
                MotherboardModel.ROG_CROSSHAIR_VIII_IMPACT,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm,
                    ECSensorType.FanChipset, ECSensorType.CurrCPU }
            },
            {
                MotherboardModel.ROG_STRIX_B550_E_GAMING,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.FanCPUOpt }
            },
            {
                MotherboardModel.ROG_STRIX_B550_I_GAMING,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm,
                    ECSensorType.FanVrmHS, ECSensorType.CurrCPU }
            },
            {
                MotherboardModel.ROG_STRIX_X570_E_GAMING,
                new ECSensorType[] { ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.TempVrm,
                    ECSensorType.FanChipset, ECSensorType.CurrCPU }
            },
            {
                MotherboardModel.ROG_STRIX_X570_F_GAMING,
                new ECSensorType[]{ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                    ECSensorType.TempTSensor, ECSensorType.FanChipset}
            },
            {
                MotherboardModel.ROG_STRIX_X570_I_GAMING,
                new ECSensorType[] {
                    ECSensorType.TempTSensor, ECSensorType.FanVrmHS, ECSensorType.FanChipset, ECSensorType.CurrCPU }
            },
            {
                MotherboardModel.ROG_STRIX_Z690_A_GAMING_WIFI_D4,
                new ECSensorType[] {
                    ECSensorType.TempTSensor, ECSensorType.TempVrm }
            }
        };

        static EmbeddedController()
        {
            System.Diagnostics.Debug.Assert(_knownSensors.Count == ((int)ECSensorType.Max));
        }

        private readonly IReadOnlyList<EmbeddedControllerSource> _sources;
        private readonly List<ECSensor> _sensors;
        private readonly ushort[] _registers;
        private readonly byte[] _data;

        /// <summary>
        /// Gets the Embedded Controller sensors
        /// </summary>
        public ECSensor[] Sensors => _sensors.ToArray();

        internal EmbeddedController(IEnumerable<EmbeddedControllerSource> sources)
        {
            Type = LPCType.EmbeddedController;
            ChipName = "Embedded Controller";

            // sorting by address, which implies sorting by bank, for optimized EC access
            var sourcesList = sources.ToList();
            sourcesList.Sort((left, right) =>
            {
                return left.Register.CompareTo(right.Register);
            });
            _sources = sourcesList;

            _sensors = new List<ECSensor>();
            List<ushort> registers = new();
            foreach (EmbeddedControllerSource s in _sources)
            {
                _sensors.Add(new() { Type = s.Type });
                for (int i = 0; i < s.Size; ++i)
                {
                    registers.Add((ushort)(s.Register + i));
                }
            }

            _registers = registers.ToArray();
            _data = new byte[_registers.Length];
        }

        internal static EmbeddedController Create(MotherboardModel model)
        {
            if (_boardSensors.TryGetValue(model, out ECSensorType[] sensors))
            {
                var sources = sensors.Select(ecs => _knownSensors[ecs]);

                return Environment.OSVersion.Platform switch
                {
                    PlatformID.Win32NT => new WindowsEmbeddedController(sources),
                    _ => null
                };
            }

            return null;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            if (!TryUpdateData())
            {
                // just skip this update cycle?
                return;
            }

            int readRegister = 0;
            for (int si = 0; si < _sensors.Count; ++si)
            {
                int val = _sources[si].Size switch
                {
                    1 => unchecked((sbyte)_data[readRegister]),
                    2 => unchecked((short)((_data[readRegister] << 8) + _data[readRegister + 1])),
                    _ => 0,
                };
                readRegister += _sources[si].Size;

                _sensors[si].Value = val != _sources[si].Blank ? val * _sources[si].Factor : null;
            }
        }

        internal abstract IEmbeddedControllerIO AcquireIOInterface();

        private bool TryUpdateData()
        {
            try
            {
                using IEmbeddedControllerIO embeddedControllerIO = AcquireIOInterface();
                embeddedControllerIO.Read(_registers, _data);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }

        public class IOException: System.IO.IOException {
            public IOException(string message): base($"ACPI embedded controller I/O error: {message}") { }
        }
    }
}
