﻿// ported from: https://gitlab.com/leogx9r/ryzen_smu
// and: https://github.com/irusanov/SMUDebugTool

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

// ReSharper disable InconsistentNaming

namespace EMNSystemInfo.HardwareAPI
{
    internal struct SMUSensorInfo
    {
        public string Name;
        public SMUSensorType Type;
        public float Scale;
    }

    /// <summary>
    /// SMU sensor type
    /// </summary>
    public enum SMUSensorType
    {
        /// <summary>
        /// Voltage sensor, value is in volts (V).
        /// </summary>
        Voltage, // V

        /// <summary>
        /// Current sensor, value is in amps (A).
        /// </summary>
        Current, // A

        /// <summary>
        /// Power sensor, value is in watts (W).
        /// </summary>
        Power, // W

        /// <summary>
        /// Clock speed sensor, value is in megahertz (MHz).
        /// </summary>
        Clock, // MHz

        /// <summary>
        /// Temperature sensor, value is in degrees Celsius (°C).
        /// </summary>
        Temperature, // °C

        /// <summary>
        /// Load percentage sensor.
        /// </summary>
        Load, // %

        /// <summary>
        /// Factor sensor.
        /// </summary>
        Factor, // 1
    }

    public enum AMDCPUCodeName
    {
        Undefined,
        Colfax,
        Renoir,
        Picasso,
        Matisse,
        Threadripper,
        CastlePeak,
        RavenRidge,
        RavenRidge2,
        SummitRidge,
        PinnacleRidge,
        Rembrandt,
        Vermeer,
        VanGogh,
        Cezanne,
        Milan,
        Dali
    }

    internal class RyzenSMU
    {
        private const byte SMU_PCI_ADDR_REG = 0xC4;
        private const byte SMU_PCI_DATA_REG = 0xC8;
        private const uint SMU_REQ_MAX_ARGS = 6;
        private const uint SMU_RETRIES_MAX = 8096;

        public readonly AMDCPUCodeName _cpuCodeName;
        private readonly Mutex _mutex = new();
        private readonly bool _supportedCPU;

        private readonly Dictionary<uint, Dictionary<uint, SMUSensorInfo>> _supportedPmTableVersions = new()
        {
            {
                // Zen Raven Ridge APU.
                0x001E0004, new Dictionary<uint, SMUSensorInfo>
                {
                    { 7, new SMUSensorInfo { Name = "TDC", Type = SMUSensorType.Current, Scale = 1 } },
                    { 11, new SMUSensorInfo { Name = "EDC", Type = SMUSensorType.Current, Scale = 1 } },
                    //{ 61, new SmuSensorType { Name = "Core", Type = SensorType.Voltage } },
                    //{ 62, new SmuSensorType { Name = "Core", Type = SensorType.Current, Scale = 1} },
                    //{ 63, new SmuSensorType { Name = "Core", Type = SensorType.Power, Scale = 1 } },
                    //{ 65, new SmuSensorType { Name = "SoC", Type = SensorType.Voltage } },
                    { 66, new SMUSensorInfo { Name = "SoC", Type = SMUSensorType.Current, Scale = 1 } },
                    { 67, new SMUSensorInfo { Name = "SoC", Type = SMUSensorType.Power, Scale = 1 } },
                    //{ 96, new SmuSensorType { Name = "Core #1", Type = SensorType.Power } },
                    //{ 97, new SmuSensorType { Name = "Core #2", Type = SensorType.Power } },
                    //{ 98, new SmuSensorType { Name = "Core #3", Type = SensorType.Power } },
                    //{ 99, new SmuSensorType { Name = "Core #4", Type = SensorType.Power } },
                    { 108, new SMUSensorInfo { Name = "Core #1", Type = SMUSensorType.Temperature, Scale = 1 } },
                    { 109, new SMUSensorInfo { Name = "Core #2", Type = SMUSensorType.Temperature, Scale = 1 } },
                    { 110, new SMUSensorInfo { Name = "Core #3", Type = SMUSensorType.Temperature, Scale = 1 } },
                    { 111, new SMUSensorInfo { Name = "Core #4", Type = SMUSensorType.Temperature, Scale = 1 } },
                    { 150, new SMUSensorInfo { Name = "GFX", Type = SMUSensorType.Voltage, Scale = 1 } },
                    { 151, new SMUSensorInfo { Name = "GFX", Type = SMUSensorType.Temperature, Scale = 1 } },
                    { 154, new SMUSensorInfo { Name = "GFX", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 156, new SMUSensorInfo { Name = "GFX", Type = SMUSensorType.Load, Scale = 1 } },
                    { 166, new SMUSensorInfo { Name = "Fabric", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 177, new SMUSensorInfo { Name = "Uncore", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 178, new SMUSensorInfo { Name = "Memory", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 342, new SMUSensorInfo { Name = "Displays", Type = SMUSensorType.Factor, Scale = 1 } },
                }
            },
            {
                // Zen 2.
                0x00240903, new Dictionary<uint, SMUSensorInfo>
                {
                    { 15, new SMUSensorInfo { Name = "TDC", Type = SMUSensorType.Current, Scale = 1 } },
                    { 21, new SMUSensorInfo { Name = "EDC", Type = SMUSensorType.Current, Scale = 1 } },
                    { 48, new SMUSensorInfo { Name = "Fabric", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 50, new SMUSensorInfo { Name = "Uncore", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 51, new SMUSensorInfo { Name = "Memory", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 115, new SMUSensorInfo { Name = "SoC", Type = SMUSensorType.Temperature, Scale = 1 } },
                    //{ 66, new SmuSensorType { Name = "Bus Speed", Type = SensorType.Clock, Scale = 1 } },
                    //{ 188, new SmuSensorType { Name = "Core #1", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 189, new SmuSensorType { Name = "Core #2", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 190, new SmuSensorType { Name = "Core #3", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 191, new SmuSensorType { Name = "Core #4", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 192, new SmuSensorType { Name = "Core #5", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 193, new SmuSensorType { Name = "Core #6", Type = SensorType.Clock, Scale = 1000 } },
                }
            },
            {
                // Zen 3.
                0x00380805, new Dictionary<uint, SMUSensorInfo>
                {
                    // TDC and EDC don't match the HWiNFO values
                    //{ 15, new SmuSensorType { Name = "TDC", Type = SensorType.Current, Scale = 1 } },
                    //{ 21, new SmuSensorType { Name = "EDC", Type = SensorType.Current, Scale = 1 } },
                    { 48, new SMUSensorInfo { Name = "Fabric", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 50, new SMUSensorInfo { Name = "Uncore", Type = SMUSensorType.Clock, Scale = 1 } },
                    { 51, new SMUSensorInfo { Name = "Memory", Type = SMUSensorType.Clock, Scale = 1 } },
                    //{ 115, new SmuSensorType { Name = "SoC", Type = SensorType.Temperature, Scale = 1 } },
                    //{ 66, new SmuSensorType { Name = "Bus Speed", Type = SensorType.Clock, Scale = 1 } },
                    //{ 188, new SmuSensorType { Name = "Core #1", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 189, new SmuSensorType { Name = "Core #2", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 190, new SmuSensorType { Name = "Core #3", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 191, new SmuSensorType { Name = "Core #4", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 192, new SmuSensorType { Name = "Core #5", Type = SensorType.Clock, Scale = 1000 } },
                    //{ 193, new SmuSensorType { Name = "Core #6", Type = SensorType.Clock, Scale = 1000 } },
                }
            }
        };

        private uint _argsAddr;
        private uint _cmdAddr;
        private uint _dramBaseAddr;
        private uint _pmTableSize;
        private uint _pmTableSizeAlt;
        private uint _pmTableVersion;
        private uint _rspAddr;

        public RyzenSMU(uint family, uint model, uint packageType)
        {
            _cpuCodeName = GetCpuCodeName(family, model, packageType);

            _supportedCPU = Environment.Is64BitOperatingSystem == Environment.Is64BitProcess && SetAddresses(_cpuCodeName);

            if (_supportedCPU)
            {
                InpOut.Open();

                SetupPmTableAddrAndSize();
            }
        }

        private static AMDCPUCodeName GetCpuCodeName(uint family, uint model, uint packageType)
        {
            if (family == 0x17)
            {
                switch (model)
                {
                    case 0x01:
                    {
                        return packageType == 7 ? AMDCPUCodeName.Threadripper : AMDCPUCodeName.SummitRidge;
                    }
                    case 0x08:
                    {
                        return packageType == 7 ? AMDCPUCodeName.Colfax : AMDCPUCodeName.PinnacleRidge;
                    }
                    case 0x11:
                    {
                        return AMDCPUCodeName.RavenRidge;
                    }
                    case 0x18:
                    {
                        return packageType == 2 ? AMDCPUCodeName.RavenRidge2 : AMDCPUCodeName.Picasso;
                    }
                    case 0x20:
                    {
                        return AMDCPUCodeName.Dali;
                    }
                    case 0x31:
                    {
                        return AMDCPUCodeName.CastlePeak;
                    }
                    case 0x60:
                    {
                        return AMDCPUCodeName.Renoir;
                    }
                    case 0x71:
                    {
                        return AMDCPUCodeName.Matisse;
                    }
                    case 0x90:
                    {
                        return AMDCPUCodeName.VanGogh;
                    }
                    default:
                    {
                        return AMDCPUCodeName.Undefined;
                    }
                }
            }

            if (family == 0x19)
            {
                switch (model)
                {
                    case 0x00:
                    {
                        return AMDCPUCodeName.Milan;
                    }
                    case 0x20:
                    case 0x21:
                    {
                        return AMDCPUCodeName.Vermeer;
                    }
                    case 0x40:
                    {
                        return AMDCPUCodeName.Rembrandt;
                    }
                    case 0x50:
                    {
                        return AMDCPUCodeName.Cezanne;
                    }
                    default:
                    {
                        return AMDCPUCodeName.Undefined;
                    }
                }
            }

            return AMDCPUCodeName.Undefined;
        }

        private bool SetAddresses(AMDCPUCodeName codeName)
        {
            switch (codeName)
            {
                case AMDCPUCodeName.CastlePeak:
                case AMDCPUCodeName.Matisse:
                case AMDCPUCodeName.Vermeer:
                {
                    _cmdAddr = 0x3B10524;
                    _rspAddr = 0x3B10570;
                    _argsAddr = 0x3B10A40;

                    return true;
                }
                case AMDCPUCodeName.Colfax:
                case AMDCPUCodeName.SummitRidge:
                case AMDCPUCodeName.Threadripper:
                case AMDCPUCodeName.PinnacleRidge:
                {
                    _cmdAddr = 0x3B1051C;
                    _rspAddr = 0x3B10568;
                    _argsAddr = 0x3B10590;

                    return true;
                }
                case AMDCPUCodeName.Renoir:
                case AMDCPUCodeName.Picasso:
                case AMDCPUCodeName.RavenRidge:
                case AMDCPUCodeName.RavenRidge2:
                case AMDCPUCodeName.Dali:
                {
                    _cmdAddr = 0x3B10A20;
                    _rspAddr = 0x3B10A80;
                    _argsAddr = 0x3B10A88;

                    return true;
                }
                default:
                {
                    return false;
                }
            }
        }

        public uint GetSmuVersion()
        {
            uint[] args = { 1 };

            if (SendCommand(0x02, ref args))
                return args[0];

            return 0;
        }

        public Dictionary<uint, SMUSensorInfo> GetPmTableStructure()
        {
            if (!IsPmTableLayoutDefined())
                return new Dictionary<uint, SMUSensorInfo>();

            return _supportedPmTableVersions[_pmTableVersion];
        }

        public bool IsPmTableLayoutDefined()
        {
            return _supportedPmTableVersions.ContainsKey(_pmTableVersion);
        }

        public float[] GetPmTable()
        {
            if (!_supportedCPU || !TransferTableToDram())
                return new float[] { 0 };

            float[] table = ReadDramToArray();

            // Fix for Zen+ empty values on first call.
            if (table.Length == 0 || table[0] == 0)
            {
                Thread.Sleep(10);
                TransferTableToDram();
                table = ReadDramToArray();
            }

            return table;
        }

        private float[] ReadDramToArray()
        {
            float[] table = new float[_pmTableSize / 4];

            byte[] bytes = InpOut.ReadMemory(new IntPtr(_dramBaseAddr), _pmTableSize);
            if (bytes != null)
                Buffer.BlockCopy(bytes, 0, table, 0, bytes.Length);

            return table;
        }

        private bool SetupPmTableAddrAndSize()
        {
            if (_pmTableSize == 0)
                SetupPmTableSize();

            if (_dramBaseAddr == 0)
                SetupDramBaseAddr();

            return _dramBaseAddr != 0 && _pmTableSize != 0;
        }

        private void SetupPmTableSize()
        {
            if (!GetPmTableVersion(ref _pmTableVersion))
                return;

            switch (_cpuCodeName)
            {
                case AMDCPUCodeName.Matisse:
                {
                    switch (_pmTableVersion)
                    {
                        case 0x240902:
                        {
                            _pmTableSize = 0x514;
                            break;
                        }
                        case 0x240903:
                        {
                            _pmTableSize = 0x518;
                            break;
                        }
                        case 0x240802:
                        {
                            _pmTableSize = 0x7E0;
                            break;
                        }
                        case 0x240803:
                        {
                            _pmTableSize = 0x7E4;
                            break;
                        }
                        default:
                        {
                            return;
                        }
                    }

                    break;
                }
                case AMDCPUCodeName.Vermeer:
                {
                    switch (_pmTableVersion)
                    {
                        case 0x2D0903:
                        {
                            _pmTableSize = 0x594;
                            break;
                        }
                        case 0x380904:
                        {
                            _pmTableSize = 0x5A4;
                            break;
                        }
                        case 0x380905:
                        {
                            _pmTableSize = 0x5D0;
                            break;
                        }
                        case 0x2D0803:
                        {
                            _pmTableSize = 0x894;
                            break;
                        }
                        case 0x380804:
                        {
                            _pmTableSize = 0x8A4;
                            break;
                        }
                        case 0x380805:
                        {
                            _pmTableSize = 0x8F0;
                            break;
                        }
                        default:
                        {
                            return;
                        }
                    }

                    break;
                }
                case AMDCPUCodeName.Renoir:
                {
                    switch (_pmTableVersion)
                    {
                        case 0x370000:
                        {
                            _pmTableSize = 0x794;
                            break;
                        }
                        case 0x370001:
                        {
                            _pmTableSize = 0x884;
                            break;
                        }
                        case 0x370002:
                        case 0x370003:
                        {
                            _pmTableSize = 0x88C;
                            break;
                        }
                        case 0x370004:
                        {
                            _pmTableSize = 0x8AC;
                            break;
                        }
                        case 0x370005:
                        {
                            _pmTableSize = 0x8C8;
                            break;
                        }
                        default:
                        {
                            return;
                        }
                    }

                    break;
                }
                case AMDCPUCodeName.Cezanne:
                {
                    switch (_pmTableVersion)
                    {
                        case 0x400005:
                        {
                            _pmTableSize = 0x944;
                            break;
                        }
                        default:
                        {
                            return;
                        }
                    }

                    break;
                }
                case AMDCPUCodeName.Picasso:
                case AMDCPUCodeName.RavenRidge:
                case AMDCPUCodeName.RavenRidge2:
                {
                    _pmTableSizeAlt = 0xA4;
                    _pmTableSize = 0x608 + _pmTableSizeAlt;
                    break;
                }
                default:
                {
                    return;
                }
            }
        }

        private bool GetPmTableVersion(ref uint version)
        {
            uint[] args = { 0 };
            uint fn;

            switch (_cpuCodeName)
            {
                case AMDCPUCodeName.RavenRidge:
                case AMDCPUCodeName.Picasso:
                {
                    fn = 0x0c;
                    break;
                }
                case AMDCPUCodeName.Matisse:
                case AMDCPUCodeName.Vermeer:
                {
                    fn = 0x08;
                    break;
                }
                case AMDCPUCodeName.Renoir:
                {
                    fn = 0x06;
                    break;
                }
                default:
                {
                    return false;
                }
            }

            bool ret = SendCommand(fn, ref args);
            version = args[0];

            return ret;
        }

        private void SetupAddrClass1(uint[] fn)
        {
            uint[] args = { 1, 1 };

            bool command = SendCommand(fn[0], ref args);
            if (!command)
                return;

            _dramBaseAddr = args[0] | (args[1] << 32);
        }

        private void SetupAddrClass2(uint[] fn)
        {
            uint[] args = { 0, 0, 0, 0, 0, 0 };

            bool command = SendCommand(fn[0], ref args);
            if (!command)
                return;

            args = new uint[] { 0 };
            command = SendCommand(fn[1], ref args);
            if (!command)
                return;

            _dramBaseAddr = args[0];
        }

        private void SetupAddrClass3(uint[] fn)
        {
            uint[] parts = { 0, 0 };

            // == Part 1 ==
            uint[] args = { 3 };
            bool command = SendCommand(fn[0], ref args);
            if (!command)
                return;

            args = new uint[] { 3 };
            command = SendCommand(fn[2], ref args);
            if (!command)
                return;

            // 1st Base.
            parts[0] = args[0];
            // == Part 1 End ==

            // == Part 2 ==
            args = new uint[] { 3 };
            command = SendCommand(fn[1], ref args);
            if (!command)
                return;

            args = new uint[] { 5 };
            command = SendCommand(fn[0], ref args);
            if (!command)
                return;

            args = new uint[] { 5 };
            command = SendCommand(fn[2], ref args);
            if (!command)
                return;

            // 2nd base.
            parts[1] = args[0];
            // == Part 2 End ==

            _dramBaseAddr = parts[0] & 0xFFFFFFFF;
        }

        private void SetupDramBaseAddr()
        {
            uint[] fn = { 0, 0, 0 };

            switch (_cpuCodeName)
            {
                case AMDCPUCodeName.Vermeer:
                case AMDCPUCodeName.Matisse:
                case AMDCPUCodeName.CastlePeak:
                {
                    fn[0] = 0x06;
                    SetupAddrClass1(fn);

                    return;
                }
                case AMDCPUCodeName.Renoir:
                {
                    fn[0] = 0x66;
                    SetupAddrClass1(fn);

                    return;
                }
                case AMDCPUCodeName.Colfax:
                case AMDCPUCodeName.PinnacleRidge:
                {
                    fn[0] = 0x0b;
                    fn[1] = 0x0c;
                    SetupAddrClass2(fn);

                    return;
                }
                case AMDCPUCodeName.Dali:
                case AMDCPUCodeName.Picasso:
                case AMDCPUCodeName.RavenRidge:
                case AMDCPUCodeName.RavenRidge2:
                {
                    fn[0] = 0x0a;
                    fn[1] = 0x3d;
                    fn[2] = 0x0b;
                    SetupAddrClass3(fn);

                    return;
                }
                default:
                {
                    return;
                }
            }
        }

        public bool TransferTableToDram()
        {
            uint[] args = { 0 };
            uint fn;

            switch (_cpuCodeName)
            {
                case AMDCPUCodeName.Matisse:
                case AMDCPUCodeName.Vermeer:
                {
                    fn = 0x05;
                    break;
                }
                case AMDCPUCodeName.Renoir:
                {
                    args[0] = 3;
                    fn = 0x65;
                    break;
                }
                case AMDCPUCodeName.Picasso:
                case AMDCPUCodeName.RavenRidge:
                case AMDCPUCodeName.RavenRidge2:
                {
                    args[0] = 3;
                    fn = 0x3d;
                    break;
                }
                default:
                {
                    return false;
                }
            }

            return SendCommand(fn, ref args);
        }

        private bool SendCommand(uint msg, ref uint[] args)
        {
            uint[] cmdArgs = new uint[SMU_REQ_MAX_ARGS];
            int argsLength = Math.Min(args.Length, cmdArgs.Length);

            for (int i = 0; i < argsLength; ++i)
                cmdArgs[i] = args[i];

            uint tmp = 0;
            if (_mutex.WaitOne(5000))
            {
                // Step 1: Wait until the RSP register is non-zero.

                tmp = 0;
                uint retries = SMU_RETRIES_MAX;
                do
                {
                    if (!ReadReg(_rspAddr, ref tmp))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                }
                while (tmp == 0 && 0 != retries--);

                // Step 1.b: A command is still being processed meaning a new command cannot be issued.

                if (retries == 0 && tmp == 0)
                {
                    _mutex.ReleaseMutex();
                    return false;
                }

                // Step 2: Write zero (0) to the RSP register
                WriteReg(_rspAddr, 0);

                // Step 3: Write the argument(s) into the argument register(s)
                for (int i = 0; i < cmdArgs.Length; ++i)
                    WriteReg(_argsAddr + (uint)(i * 4), cmdArgs[i]);

                // Step 4: Write the message Id into the Message ID register
                WriteReg(_cmdAddr, msg);

                // Step 5: Wait until the Response register is non-zero.
                tmp = 0;
                retries = SMU_RETRIES_MAX;
                do
                {
                    if (!ReadReg(_rspAddr, ref tmp))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                }
                while (tmp == 0 && retries-- != 0);

                if (retries == 0 && tmp != (uint)Status.OK)
                {
                    _mutex.ReleaseMutex();
                    return false;
                }

                // Step 6: If the Response register contains OK, then SMU has finished processing  the message.

                args = new uint[SMU_REQ_MAX_ARGS];
                for (byte i = 0; i < SMU_REQ_MAX_ARGS; i++)
                {
                    if (!ReadReg(_argsAddr + (uint)(i * 4), ref args[i]))
                    {
                        _mutex.ReleaseMutex();
                        return false;
                    }
                }

                ReadReg(_rspAddr, ref tmp);
                _mutex.ReleaseMutex();
            }

            return tmp == (uint)Status.OK;
        }

        private static void WriteReg(uint addr, uint data)
        {
            if (Ring0.WaitPciBusMutex(10))
            {
                if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr))
                {
                    Ring0.WritePciConfig(0x00, SMU_PCI_DATA_REG, data);
                }

                Ring0.ReleasePciBusMutex();
            }
        }

        private static bool ReadReg(uint addr, ref uint data)
        {
            bool read = false;

            if (Ring0.WaitPciBusMutex(10))
            {
                if (Ring0.WritePciConfig(0x00, SMU_PCI_ADDR_REG, addr))
                {
                    read = Ring0.ReadPciConfig(0x00, SMU_PCI_DATA_REG, out data);
                }

                Ring0.ReleasePciBusMutex();
            }

            return read;
        }

        private enum Status : uint
        {
            OK = 0x01,
            Failed = 0xFF,
            UnknownCmd = 0xFE,
            CmdRejectedPrereq = 0xFD,
            CmdRejectedBusy = 0xFC
        }
    }
}
