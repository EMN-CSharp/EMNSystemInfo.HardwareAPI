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
        /// <summary>CPU Core voltage [mV]</summary>
        VoltageCPU,
        /// <summary>CPU_Opt fan [RPM]</summary>
        FanCPUOpt,
        /// <summary>VRM heat sink fan [RPM]</summary>
        FanVrmHS,
        /// <summary>Chipset fan [RPM]</summary>
        FanChipset,
        /// <summary>Water Pump [RPM]</summary>
        FanWaterPump,
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
        private enum BoardFamily
        {
            Amd400,
            Amd500,
            Intel100,
            Intel600,
        }

        private struct BoardInfo
        {
            public BoardInfo(MotherboardModel[] models, BoardFamily family, params ECSensorType[] sensors)
            {
                Models = models;
                Family = family;
                Sensors = sensors;
            }

            public BoardInfo(MotherboardModel model, BoardFamily family, params ECSensorType[] sensors)
            {
                Models = new MotherboardModel[] { model };
                Family = family;
                Sensors = sensors;
            }

            public MotherboardModel[] Models { get; }
            public BoardFamily Family { get; }
            public ECSensorType[] Sensors { get; }
        };

        private static readonly Dictionary<BoardFamily, Dictionary<ECSensorType, EmbeddedControllerSource>> _knownSensors = new()
        {
            {
                BoardFamily.Amd400,
                new()  // no chipset fans in this generation
                {
                    { ECSensorType.TempChipset, new EmbeddedControllerSource(0x003a) },
                    { ECSensorType.TempCPU, new EmbeddedControllerSource(0x003b) },
                    { ECSensorType.TempMB, new EmbeddedControllerSource(0x003c) },
                    { ECSensorType.TempTSensor, new EmbeddedControllerSource(0x003d, blank: -40) },
                    { ECSensorType.TempVrm, new EmbeddedControllerSource(0x003e) },
                    { ECSensorType.VoltageCPU, new EmbeddedControllerSource(0x00a2, 2, factor: 1e-3f) },
                    { ECSensorType.FanCPUOpt, new EmbeddedControllerSource(0x00bc, 2) },
                    { ECSensorType.FanVrmHS, new EmbeddedControllerSource(0x00b2, 2) },
                    { ECSensorType.FanWaterFlow, new EmbeddedControllerSource(0x00b4, 2, factor: 1.0f / 42f * 60f) },
                    { ECSensorType.CurrCPU, new EmbeddedControllerSource(0x00f4) },
                    { ECSensorType.TempWaterIn, new EmbeddedControllerSource(0x010d, blank: -40) },
                    { ECSensorType.TempWaterOut, new EmbeddedControllerSource(0x010b, blank: -40) },
                }
            },
            {
                BoardFamily.Amd500,
                new()
                {
                    { ECSensorType.TempChipset, new EmbeddedControllerSource(0x003a) },
                    { ECSensorType.TempCPU, new EmbeddedControllerSource(0x003b) },
                    { ECSensorType.TempMB, new EmbeddedControllerSource(0x003c) },
                    { ECSensorType.TempTSensor, new EmbeddedControllerSource(0x003d, blank: -40) },
                    { ECSensorType.TempVrm, new EmbeddedControllerSource(0x003e) },
                    { ECSensorType.VoltageCPU, new EmbeddedControllerSource(0x00a2, 2, factor: 1e-3f) },
                    { ECSensorType.FanCPUOpt, new EmbeddedControllerSource(0x00b0, 2) },
                    { ECSensorType.FanVrmHS, new EmbeddedControllerSource(0x00b2, 2) },
                    { ECSensorType.FanChipset, new EmbeddedControllerSource(0x00b4, 2) },
                    // TODO: "why 42?" is a silly question, I know, but still, why? On the serious side, it might be 41.6(6)
                    { ECSensorType.FanWaterFlow, new EmbeddedControllerSource(0x00bc, 2, factor: 1.0f / 42f * 60f) },
                    { ECSensorType.CurrCPU, new EmbeddedControllerSource(0x00f4) },
                    { ECSensorType.TempWaterIn, new EmbeddedControllerSource(0x0100, blank: -40) },
                    { ECSensorType.TempWaterOut, new EmbeddedControllerSource(0x0101, blank: -40) },
                }
            },
            {
                BoardFamily.Intel100,
                new()
                {
                    { ECSensorType.TempChipset, new EmbeddedControllerSource(0x003a) },
                    { ECSensorType.TempTSensor, new EmbeddedControllerSource(0x003d, blank: -40) },
                    { ECSensorType.FanWaterPump, new EmbeddedControllerSource(0x00bc, 2) },
                    { ECSensorType.CurrCPU, new EmbeddedControllerSource(0x00f4) },
                    { ECSensorType.VoltageCPU, new EmbeddedControllerSource(0x00a2, 2, factor: 1e-3f) },
                }
            },
            {
                BoardFamily.Intel600,
                new()
                {
                    { ECSensorType.TempTSensor, new EmbeddedControllerSource(0x003d, blank: -40) },
                    { ECSensorType.TempVrm, new EmbeddedControllerSource(0x003e) },
                }
            },
        };

        private static readonly BoardInfo[] _boards = new BoardInfo[]
        {
            new(MotherboardModel.PRIME_X470_PRO, BoardFamily.Amd400,
               ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
               ECSensorType.TempVrm, ECSensorType.TempVrm, ECSensorType.FanCPUOpt,
               ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new (MotherboardModel.PRIME_X570_PRO, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempVrm, ECSensorType.TempTSensor, ECSensorType.FanChipset
            ),
            new(MotherboardModel.PRO_WS_X570_ACE, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempVrm, ECSensorType.FanChipset, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(new MotherboardModel[] {MotherboardModel.ROG_CROSSHAIR_VIII_HERO, MotherboardModel.ROG_CROSSHAIR_VIII_HERO_WIFI }, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.TempWaterIn, ECSensorType.TempWaterOut,
                ECSensorType.FanCPUOpt, ECSensorType.FanChipset, ECSensorType.FanWaterFlow,
                ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_CROSSHAIR_VIII_DARK_HERO, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.TempWaterIn, ECSensorType.TempWaterOut,
                ECSensorType.FanCPUOpt, ECSensorType.FanWaterFlow, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.CROSSHAIR_III_FORMULA, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm,
                ECSensorType.FanCPUOpt, ECSensorType.FanChipset, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_CROSSHAIR_VIII_IMPACT, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm,
                ECSensorType.FanChipset, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_STRIX_B550_E_GAMING, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm, ECSensorType.FanCPUOpt
            ),
            new(MotherboardModel.ROG_STRIX_B550_I_GAMING, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm,
                ECSensorType.FanVrmHS, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_STRIX_X570_E_GAMING, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.TempVrm,
                ECSensorType.FanChipset, ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_STRIX_X570_F_GAMING, BoardFamily.Amd500,
                ECSensorType.TempChipset, ECSensorType.TempCPU, ECSensorType.TempMB,
                ECSensorType.TempTSensor, ECSensorType.FanChipset
            ),
            new(MotherboardModel.ROG_STRIX_X570_I_GAMING, BoardFamily.Amd500,
                ECSensorType.TempTSensor, ECSensorType.FanVrmHS, ECSensorType.FanChipset,
                ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.ROG_STRIX_Z690_A_GAMING_WIFI_D4, BoardFamily.Intel600,
                ECSensorType.TempTSensor, ECSensorType.TempVrm
            ),
            new(MotherboardModel.Z170_A, BoardFamily.Intel100,
                ECSensorType.TempTSensor, ECSensorType.TempChipset, ECSensorType.FanWaterPump,
                ECSensorType.CurrCPU, ECSensorType.VoltageCPU
            ),
            new(MotherboardModel.SLEEPY_IL, BoardFamily.Intel600,
                ECSensorType.FanCPUOpt
            )
        };

        private readonly IReadOnlyList<(ECSensorType SensorType, EmbeddedControllerSource Source)> _boardSensors;
        private readonly List<ECSensor> _ecSensors;
        private readonly ushort[] _registers;
        private readonly byte[] _data;

        /// <summary>
        /// Gets the Embedded Controller sensors
        /// </summary>
        public ECSensor[] Sensors => _ecSensors.ToArray();

        internal EmbeddedController(IEnumerable<(ECSensorType SensorType, EmbeddedControllerSource Source)> boardSensors)
        {
            Type = LPCType.EmbeddedController;
            ChipName = "Embedded Controller";

            // sorting by address, which implies sorting by bank, for optimized EC access
            var boardSensorsList = boardSensors.ToList();
            boardSensorsList.Sort((left, right) =>
            {
                return left.Source.Register.CompareTo(right.Source.Register);
            });
            _boardSensors = boardSensorsList;

            _ecSensors = new List<ECSensor>();
            List<ushort> registers = new();
            foreach (var s in _boardSensors)
            {
                _ecSensors.Add(new() { Type = s.SensorType });
                for (int i = 0; i < s.Source.Size; ++i)
                {
                    registers.Add((ushort)(s.Source.Register + i));
                }
            }

            _registers = registers.ToArray();
            _data = new byte[_registers.Length];
        }

        internal static EmbeddedController Create(MotherboardModel model)
        {
            var boards = _boards.Where(b => b.Models.Contains(model)).ToList();
            if (boards.Count == 0)
                return null;
            if (boards.Count > 1)
                throw new MultipleBoardRecordsFoundException(model.ToString());
            BoardInfo board = boards[0];
            var boardSensors = board.Sensors.Select(ecs => (SensorType: ecs, Source: _knownSensors[board.Family][ecs]));

            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => new WindowsEmbeddedController(boardSensors),
                _ => null
            };
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
            for (int si = 0; si < _ecSensors.Count; ++si)
            {
                int val = _boardSensors[si].Source.Size switch
                {
                    1 => unchecked((sbyte)_data[readRegister]),
                    2 => unchecked((short)((_data[readRegister] << 8) + _data[readRegister + 1])),
                    _ => 0,
                };
                readRegister += _boardSensors[si].Source.Size;

                _ecSensors[si].Value = val != _boardSensors[si].Source.Blank ? val * _boardSensors[si].Source.Factor : null;
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

        public class IOException: System.IO.IOException
        {
            public IOException(string message): base($"ACPI embedded controller I/O error: {message}") { }
        }

        public class BadConfigurationException : System.Exception
        {
            public BadConfigurationException(string message) : base(message) { }
        }

        public class MultipleBoardRecordsFoundException : BadConfigurationException
        {
            public MultipleBoardRecordsFoundException(string model) : base($"Multiple board records refer to the same model '{model}'") { }
        }
    }
}
