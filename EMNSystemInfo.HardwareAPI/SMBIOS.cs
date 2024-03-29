// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;
using System;
using System.Collections.Generic;
using System.Text;

namespace EMNSystemInfo.HardwareAPI
{
    /// <summary>
    /// Chassis security status based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.3</see>.
    /// </summary>
    public enum ChassisSecurityStatus
    {
        Other = 1,
        Unknown,
        None,
        ExternalInterfaceLockedOut,
        ExternalInterfaceEnabled
    }

    /// <summary>
    /// Chassis state based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.2</see>.
    /// </summary>
    public enum ChassisStates
    {
        Other = 1,
        Unknown,
        Safe,
        Warning,
        Critical,
        NonRecoverable
    }

    /// <summary>
    /// Chassis type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.4.1</see>.
    /// </summary>
    public enum ChassisType
    {
        Other = 1,
        Unknown,
        Desktop,
        LowProfileDesktop,
        PizzaBox,
        MiniTower,
        Tower,
        Portable,
        Laptop,
        Notebook,
        HandHeld,
        DockingStation,
        AllInOne,
        SubNotebook,
        SpaceSaving,
        LunchBox,
        MainServerChassis,
        ExpansionChassis,
        SubChassis,
        BusExpansionChassis,
        PeripheralChassis,
        RaidChassis,
        RackMountChassis,
        SealedCasePc,
        MultiSystemChassis,
        CompactPci,
        AdvancedTca,
        Blade,
        BladeEnclosure,
        Tablet,
        Convertible,
        Detachable,
        IoTGateway,
        EmbeddedPc,
        MiniPc,
        StickPc
    }

    /// <summary>
    /// Processor family based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.2</see>.
    /// </summary>
    public enum ProcessorFamily
    {
        Other = 1,
        Intel8086 = 3,
        Intel80286 = 4,
        Intel386,
        Intel486,
        Intel8087,
        Intel80287,
        Intel80387,
        Intel80487,
        IntelPentium,
        IntelPentiumPro,
        IntelPentiumII,
        IntelPentiumMMX,
        IntelCeleron,
        IntelPentiumIIXeon,
        IntelPentiumIII,
        M1,
        M2,
        IntelCeleronM,
        IntelPentium4HT,
        AmdDuron = 24,
        AmdK5,
        AmdK6,
        AmdK62,
        AmdK63,
        AmdAthlon,
        Amd2900,
        AmdK62Plus,
        PowerPc,
        PowerPc601,
        PowerPc603,
        PowerPc603Plus,
        PowerPc604,
        PowerPc620,
        PowerPcx704,
        PowerPc750,
        IntelCoreDuo,
        IntelCoreDuoMobile,
        IntelCoreSoloMobile,
        IntelAtom,
        IntelCoreM,
        IntelCoreM3,
        IntelCoreM5,
        IntelCoreM7,
        Alpha,
        Alpha21064,
        Alpha21066,
        Alpha21164,
        Alpha21164Pc,
        Alpha21164a,
        Alpha21264,
        Alpha21364,
        AmdTurionIIUltraDualCoreMobileM,
        AmdTurionDualCoreMobileM,
        AmdAthlonIIDualCoreM,
        AmdOpteron6100Series,
        AmdOpteron4100Series,
        AmdOpteron6200Series,
        AmdOpteron4200Series,
        AmdFxSeries,
        Mips,
        MipsR4000,
        MipsR4200,
        MipsR4400,
        MipsR4600,
        MipsR10000,
        AmdCSeries,
        AmdESeries,
        AmdASeries,
        AmdGSeries,
        AmdZSeries,
        AmdRSeries,
        AmdOpteron4300Series,
        AmdOpteron6300Series,
        AmdOpteron3300Series,
        AmdFireProSeries,
        Sparc,
        SuperSparc,
        MicroSparcII,
        MicroSparcIIep,
        UltraSparc,
        UltraSparcII,
        UltraSparcIIi,
        UltraSparcIII,
        UltraSparcIIIi,
        Motorola68040 = 96,
        Motorola68xxx,
        Motorola68000,
        Motorola68010,
        Motorola68020,
        Motorola68030,
        AmdAthlonX4QuadCore,
        AmdOpteronX1000Series,
        AmdOpteronX2000Series,
        AmdOpteronASeries,
        AmdOpteronX3000Series,
        AmdZen,
        Hobbit = 112,
        CrusoeTm5000 = 120,
        CrusoeTm3000,
        EfficeonTm8000,
        Weitek = 128,
        IntelItanium = 130,
        AmdAthlon64,
        AmdOpteron,
        AmdSempron,
        AmdTurio64Mobile,
        AmdOpteronDualCore,
        AmdAthlon64X2DualCore,
        AmdTurion64X2Mobile,
        AmdOpteronQuadCore,
        AmdOpteronThirdGen,
        AmdPhenomFXQuadCore,
        AmdPhenomX4QuadCore,
        AmdPhenomX2DualCore,
        AmdAthlonX2DualCore,
        PaRisc,
        PaRisc8500,
        PaRisc8000,
        PaRisc7300LC,
        PaRisc7200,
        PaRisc7100LC,
        PaRisc7100,
        V30 = 160,
        IntelXeon3200QuadCoreSeries,
        IntelXeon3000DualCoreSeries,
        IntelXeon5300QuadCoreSeries,
        IntelXeon5100DualCoreSeries,
        IntelXeon5000DualCoreSeries,
        IntelXeonLVDualCore,
        IntelXeonULVDualCore,
        IntelXeon7100Series,
        IntelXeon5400Series,
        IntelXeonQuadCore,
        IntelXeon5200DualCoreSeries,
        IntelXeon7200DualCoreSeries,
        IntelXeon7300QuadCoreSeries,
        IntelXeon7400QuadCoreSeries,
        IntelXeon7400MultiCoreSeries,
        IntelPentiumIIIXeon,
        IntelPentiumIIISpeedStep,
        IntelPentium4,
        IntelXeon,
        As400,
        IntelXeonMP,
        AmdAthlonXP,
        AmdAthlonMP,
        IntelItanium2,
        IntelPentiumM,
        IntelCeleronD,
        IntelPentiumD,
        IntelPentiumExtreme,
        IntelCoreSolo,
        IntelCore2Duo = 191,
        IntelCore2Solo,
        IntelCore2Extreme,
        IntelCore2Quad,
        IntelCore2ExtremeMobile,
        IntelCore2DuoMobile,
        IntelCore2SoloMobile,
        IntelCoreI7,
        IntelCeleronDualCore,
        Ibm390,
        PowerPcG4,
        PowerPcG5,
        Esa390G6,
        ZArchitecture,
        IntelCoreI5,
        IntelCoreI3,
        IntelCoreI9,
        ViaC7M = 210,
        ViaC7D,
        ViaC7,
        ViaEden,
        IntelXeonMultiCore,
        IntelXeon3xxxDualCoreSeries,
        IntelXeon3xxxQuadCoreSeries,
        ViaNano,
        IntelXeon5xxxDualCoreSeries,
        IntelXeon5xxxQuadCoreSeries,
        IntelXeon7xxxDualCoreSeries = 221,
        IntelXeon7xxxQuadCoreSeries,
        IntelXeon7xxxMultiCoreSeries,
        IntelXeon3400MultiCoreSeries,
        AmdOpteron3000Series = 228,
        AmdSempronII,
        AmdOpteronQuadCoreEmbedded,
        AmdPhenomTripleCore,
        AmdTurionUltraDualCoreMobile,
        AmdTurionDualCoreMobile,
        AmdTurionDualCore,
        AmdAthlonDualCore,
        AmdSempronSI,
        AmdPhenomII,
        AmdAthlonII,
        AmdOpteronSixCore,
        AmdSempronM,
        IntelI860 = 250,
        IntelI960,
        ArmV7 = 256,
        ArmV8,
        HitachiSh3,
        HitachiSh4,
        Arm,
        StrongArm,
        _686,
        MediaGX,
        MII,
        WinChip,
        Dsp,
        VideoProcessor
    }

    /// <summary>
    /// Processor type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.1</see>.
    /// </summary>
    public enum SMBIOSProcessorType
    {
        Other = 1,
        Unknown,
        CentralProcessor,
        MathProcessor,
        DspProcessor,
        VideoProcessor
    }

    /// <summary>
    /// Processor socket based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.5.5</see>.
    /// </summary>
    public enum ProcessorSocket
    {
        Other = 1,
        Unknown,
        DaughterBoard,
        ZifSocket,
        PiggyBack,
        None,
        LifSocket,
        Zif423 = 13,
        A,
        Zif478,
        Zif754,
        Zif940,
        Zif939,
        MPga604,
        Lga771,
        Lga775,
        S1,
        AM2,
        F,
        Lga1366,
        G34,
        AM3,
        C32,
        Lga1156,
        Lga1567,
        Pga988A,
        Bga1288,
        RPga088B,
        Bga1023,
        Bga1224,
        Lga1155,
        Lga1356,
        Lga2011,
        FS1,
        FS2,
        FM1,
        FM2,
        Lga20113,
        Lga13563,
        Lga1150,
        Bga1168,
        Bga1234,
        Bga1364,
        AM4,
        Lga1151,
        Bga1356,
        Bga1440,
        Bga1515,
        Lga36471,
        SP3,
        SP3R2,
        Lga2066,
        Bga1510,
        Bga1528,
        Lga4189
    }

    /// <summary>
    /// System wake-up type based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.2.2</see>.
    /// </summary>
    public enum SystemWakeUp
    {
        Reserved,
        Other,
        Unknown,
        ApmTimer,
        ModemRing,
        LanRemote,
        PowerSwitch,
        PciPme,
        AcPowerRestored
    }

    /// <summary>
    /// Cache associativity based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.3.0, Chapter 7.8.5</see>.
    /// </summary>
    public enum CacheAssociativity
    {
        Other = 1,
        Unknown,
        DirectMapped,
        _2Way,
        _4Way,
        FullyAssociative,
        _8Way,
        _16Way,
        _12Way,
        _24Way,
        _32Way,
        _48Way,
        _64Way,
        _20Way,
    }

    /// <summary>
    /// Processor cache level.
    /// </summary>
    public enum CacheDesignation
    {
        Other,
        L1,
        L2,
        L3
    }

    /// <summary>
    /// RAM module form factor, based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.5.0, Chapter 7.18.1</see>.
    /// </summary>
    public enum RAMFormFactor : byte
    {
        Unknown = 1,
        Other = 2,
        SIMM = 3,
        SIP = 4,
        DIP = 5,
        ZIP = 6,
        SOJ = 7,
        Proprietary = 8,
        DIMM = 9,
        TSOP = 10,
        RowOfChips = 11,
        RIMM = 12,
        SODIMM = 13,
        SRIMM = 14,
        FBDIMM = 15,
        Die = 16
    }

    /// <summary>
    /// RAM module type, based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.5.0, Chapter 7.18.2</see>.
    /// </summary>
    public enum RAMType : byte
    {
        Unknown = 0,
        Other = 1,
        DRAM = 2,
        SyncDRAM = 3,
        CacheDRAM = 4,
        EDO = 5,
        EDRAM = 6,
        VRAM = 7,
        SRAM = 8,
        RAM = 9,
        ROM = 10,
        Flash = 11,
        EEPROM = 12,
        FEPROM = 13,
        EPROM = 14,
        CDRAM = 15,
        ThreeDRAM = 16,
        SDRAM = 17,
        SGRAM = 18,
        RDRAM = 19,
        DDR = 20,
        DDR2 = 21,
        DDR2_FBDIMM = 22,
        DDR3 = 24,
        FBD2 = 25,
        DDR4 = 26,
        LPDDR = 0x1B,
        LPDDR2 = 0x1C,
        LPDDR3 = 0x1D,
        LPDDR4 = 0x1E,
        LogicalNonVolatileDevice = 0x1F,
        HBM = 0x20,
        HBM2 = 0x21,
        DDR5 = 0x22,
        LPDDR5 = 0x23,
    }

    /// <summary>
    /// Additional detail on the RAM module type, based on <see href="https://www.dmtf.org/dsp/DSP0134">DMTF SMBIOS Reference Specification v.3.5.0, Chapter 7.18.3</see>.
    /// </summary>
    public enum RAMTypeDetail : ushort
    {
        Reserved = 1,
        Other = 2,
        Unknown = 4,
        FastPaged = 8,
        StaticColumn = 16,
        PseudoStatic = 32,
        Rambus = 64,
        Synchronous = 128,
        CMOS = 256,
        EDO = 512,
        WindowDRAM = 1024,
        CacheDRAM = 2048,
        NVRAM = 4096,
        RegisteredBuffered = 8192,
        UnbufferedRegistered = 16384,
        LRDIMM = 32768
    }

    public class InformationBase
    {
        private readonly byte[] _data;
        private readonly IList<string> _strings;

        /// <summary>
        /// Initializes a new instance of the <see cref="InformationBase" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="strings">The strings.</param>
        protected InformationBase(byte[] data, IList<string> strings)
        {
            _data = data;
            _strings = strings;
        }

        /// <summary>
        /// Gets the byte.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="int" />.</returns>
        protected int GetByte(int offset)
        {
            if (offset < _data.Length && offset >= 0)
                return _data[offset];


            return 0;
        }

        /// <summary>
        /// Gets the word.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="int" />.</returns>
        protected int GetWord(int offset)
        {
            if (offset + 1 < _data.Length && offset >= 0)
                return (_data[offset + 1] << 8) | _data[offset];


            return 0;
        }

        /// <summary>
        /// Gets the string.
        /// </summary>
        /// <param name="offset">The offset.</param>
        /// <returns><see cref="string" />.</returns>
        protected string GetString(int offset)
        {
            if (offset < _data.Length && _data[offset] > 0 && _data[offset] <= _strings.Count)
                return _strings[_data[offset] - 1];


            return string.Empty;
        }
    }

    /// <summary>
    /// Motherboard BIOS information obtained from the SMBIOS table.
    /// </summary>
    public class BiosInformation : InformationBase
    {
        internal BiosInformation(string vendor, string version, string date = null, ulong? size = null) : base(null, null)
        {
            Vendor = vendor;
            Version = version;
            Date = GetDate(date);
            Size = size;
        }

        internal BiosInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            Vendor = GetString(0x04);
            Version = GetString(0x05);
            Date = GetDate(GetString(0x08));
            Size = GetSize();
        }

        /// <summary>
        /// Gets the BIOS release date.
        /// </summary>
        public DateTime? Date { get; }

        /// <summary>
        /// Gets the size of the physical device containing the BIOS.
        /// </summary>
        public ulong? Size { get; }

        /// <summary>
        /// Gets the string number of the BIOS Vendor’s Name.
        /// </summary>
        public string Vendor { get; }

        /// <summary>
        /// Gets the string number of the BIOS Version. This value is a free-form string that may contain Core and OEM version information.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <returns><see cref="Nullable{Int64}" />.</returns>
        private ulong? GetSize()
        {
            int biosRomSize = GetByte(0x09);
            int extendedBiosRomSize = GetWord(0x18);

            bool isExtendedBiosRomSize = biosRomSize == 0xFF && extendedBiosRomSize != 0;
            if (!isExtendedBiosRomSize)
                return 65536 * (ulong)(biosRomSize + 1);


            int unit = (extendedBiosRomSize & 0xC000) >> 14;
            ulong extendedSize = (ulong)(extendedBiosRomSize & ~0xC000) * 1024 * 1024;

            switch (unit)
            {
                case 0x00: return extendedSize; // Megabytes
                case 0x01: return extendedSize * 1024; // Gigabytes - might overflow in the future
            }

            return null; // Other patterns not defined in DMI 3.2.0
        }

        /// <summary>
        /// Gets the date.
        /// </summary>
        /// <param name="date">The bios date.</param>
        /// <returns><see cref="Nullable{DateTime}" />.</returns>
        private static DateTime? GetDate(string date)
        {
            string[] parts = (date ?? string.Empty).Split('/');

            if (parts.Length == 3 &&
                int.TryParse(parts[0], out int month) &&
                int.TryParse(parts[1], out int day) &&
                int.TryParse(parts[2], out int year))
            {
                if (month > 12)
                {
                    int tmp = month;
                    month = day;
                    day = tmp;
                }

                return new DateTime(year < 100 ? 1900 + year : year, month, day);
            }

            return null;
        }
    }

    /// <summary>
    /// System information obtained from the SMBIOS table.
    /// </summary>
    public class SystemInformation : InformationBase
    {
        internal SystemInformation
        (
            string manufacturerName,
            string productName,
            string version,
            string serialNumber,
            string family,
            SystemWakeUp wakeUp = SystemWakeUp.Unknown) : base(null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
            Family = family;
            WakeUp = wakeUp;
        }

        internal SystemInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            ManufacturerName = GetString(0x04);
            ProductName = GetString(0x05);
            Version = GetString(0x06);
            SerialNumber = GetString(0x07);
            Family = GetString(0x1A);
            WakeUp = (SystemWakeUp)GetByte(0x18);
        }

        /// <summary>
        /// Gets the family associated with system.
        /// <para>
        /// This text string identifies the family to which a particular computer belongs. A family refers to a set of computers that are similar but not identical from a hardware or software point of
        /// view. Typically, a family is composed of different computer models, which have different configurations and pricing points. Computers in the same family often have similar branding and cosmetic
        /// features.
        /// </para>
        /// </summary>
        public string Family { get; }

        /// <summary>
        /// Gets the manufacturer name associated with system.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the product name associated with system.
        /// </summary>
        public string ProductName { get; }

        /// <summary>
        /// Gets the serial number string associated with system.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the version string associated with system.
        /// </summary>
        public string Version { get; }

        /// <summary>
        /// Gets <inheritdoc cref="SystemWakeUp" />
        /// </summary>
        public SystemWakeUp WakeUp { get; }
    }

    /// <summary>
    /// Chassis information obtained from the SMBIOS table.
    /// </summary>
    public class ChassisInformation : InformationBase
    {
        internal ChassisInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            ManufacturerName = GetString(0x04).Trim();
            Version = GetString(0x06).Trim();
            SerialNumber = GetString(0x07).Trim();
            AssetTag = GetString(0x08).Trim();
            RackHeight = GetByte(0x11);
            PowerCords = GetByte(0x12);
            SKU = GetString(0x15).Trim();
            LockDetected = (GetByte(0x05) & 128) == 128;
            ChassisType = (ChassisType)(GetByte(0x05) & 127);
            BootUpState = (ChassisStates)GetByte(0x09);
            PowerSupplyState = (ChassisStates)GetByte(0x0A);
            ThermalState = (ChassisStates)GetByte(0x0B);
            SecurityStatus = (ChassisSecurityStatus)GetByte(0x0C);
        }

        /// <summary>
        /// Gets the asset tag associated with the enclosure or chassis.
        /// </summary>
        public string AssetTag { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ChassisStates" />
        /// </summary>
        public ChassisStates BootUpState { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ChassisType" />
        /// </summary>
        public ChassisType ChassisType { get; }

        /// <summary>
        /// Gets or sets the chassis lock.
        /// </summary>
        /// <returns>Chassis lock is present if <see langword="true" />. Otherwise, either a lock is not present or it is unknown if the enclosure has a lock.</returns>
        public bool LockDetected { get; set; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure manufacturer name.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the number of power cords associated with the enclosure or chassis.
        /// </summary>
        public int PowerCords { get; }

        /// <summary>
        /// Gets the state of the enclosure’s power supply (or supplies) when last booted.
        /// </summary>
        public ChassisStates PowerSupplyState { get; }

        /// <summary>
        /// Gets the height of the enclosure, in 'U's. A U is a standard unit of measure for the height of a rack or rack-mountable component and is equal to 1.75 inches or 4.445 cm. A value of <c>0</c>
        /// indicates that the enclosure height is unspecified.
        /// </summary>
        public int RackHeight { get; }

        /// <summary>
        /// Gets the physical security status of the enclosure when last booted.
        /// </summary>
        public ChassisSecurityStatus SecurityStatus { get; set; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure serial number.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the string describing the chassis or enclosure SKU number.
        /// </summary>
        public string SKU { get; }

        /// <summary>
        /// Gets the thermal state of the enclosure when last booted.
        /// </summary>
        public ChassisStates ThermalState { get; }

        /// <summary>
        /// Gets the number of null-terminated string representing the chassis or enclosure version.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Motherboard information obtained from the SMBIOS table.
    /// </summary>
    public class BaseBoardInformation : InformationBase
    {
        internal BaseBoardInformation(string manufacturerName, string productName, string version, string serialNumber) : base(null, null)
        {
            ManufacturerName = manufacturerName;
            ProductName = productName;
            Version = version;
            SerialNumber = serialNumber;
        }

        internal BaseBoardInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            ManufacturerName = GetString(0x04).Trim();
            ProductName = GetString(0x05).Trim();
            Version = GetString(0x06).Trim();
            SerialNumber = GetString(0x07).Trim();
        }

        /// <summary>
        /// Gets the value that represents the manufacturer's name.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's name.
        /// </summary>
        public string ProductName { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's serial number.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the value that represents the motherboard's revision number.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Processor information obtained from the SMBIOS table.
    /// </summary>
    public class ProcessorInformation : InformationBase
    {
        internal ProcessorInformation(byte[] data, IList<string> strings) : base(data, strings)
        {
            SocketDesignation = GetString(0x04).Trim();
            ManufacturerName = GetString(0x07).Trim();
            Version = GetString(0x10).Trim();
            CoreCount = GetByte(0x23) != 255 ? GetByte(0x23) : GetWord(0x2A);
            CoreEnabled = GetByte(0x24) != 255 ? GetByte(0x24) : GetWord(0x2C);
            ThreadCount = GetByte(0x25) != 255 ? GetByte(0x25) : GetWord(0x2E);
            ExternalClock = GetWord(0x12);
            MaxSpeed = GetWord(0x14);
            CurrentSpeed = GetWord(0x16);
            Serial = GetString(0x20).Trim();

            ProcessorType = (SMBIOSProcessorType)GetByte(0x05);
            Socket = (ProcessorSocket)GetByte(0x19);

            int family = GetByte(0x06);
            Family = (ProcessorFamily)(family == 254 ? GetWord(0x28) : family);
        }

        /// <summary>
        /// Gets the value that represents the number of cores per processor socket.
        /// </summary>
        public int CoreCount { get; }

        /// <summary>
        /// Gets the value that represents the number of enabled cores per processor socket.
        /// </summary>
        public int CoreEnabled { get; }

        /// <summary>
        /// Gets the value that represents the current processor speed (in MHz).
        /// </summary>
        public int CurrentSpeed { get; }

        /// <summary>
        /// Gets the external Clock Frequency, in MHz. If the value is unknown, the field is set to 0.
        /// </summary>
        public int ExternalClock { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorFamily" />
        /// </summary>
        public ProcessorFamily Family { get; }

        /// <summary>
        /// Gets the string number of Processor Manufacturer.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the value that represents the maximum processor speed (in MHz) supported by the system for this processor socket.
        /// </summary>
        public int MaxSpeed { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorType" />
        /// </summary>
        public SMBIOSProcessorType ProcessorType { get; }

        /// <summary>
        /// Gets the value that represents the string number for the serial number of this processor.
        /// <para>This value is set by the manufacturer and normally not changeable.</para>
        /// </summary>
        public string Serial { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorSocket" />
        /// </summary>
        public ProcessorSocket Socket { get; }

        /// <summary>
        /// Gets the string number for Reference Designation.
        /// </summary>
        public string SocketDesignation { get; }

        /// <summary>
        /// Gets the value that represents the number of threads per processor socket.
        /// </summary>
        public int ThreadCount { get; }

        /// <summary>
        /// Gets the value that represents the string number describing the Processor.
        /// </summary>
        public string Version { get; }
    }

    /// <summary>
    /// Processor cache information obtained from the SMBIOS table.
    /// </summary>
    public class ProcessorCache : InformationBase
    {
        internal ProcessorCache(byte[] data, IList<string> strings) : base(data, strings)
        {
            Designation = GetCacheDesignation();
            Associativity = (CacheAssociativity)GetByte(0x12);
            Size = GetWord(0x09);
        }

        /// <summary>
        /// Gets <inheritdoc cref="CacheAssociativity" />
        /// </summary>
        public CacheAssociativity Associativity { get; }

        /// <summary>
        /// Gets <inheritdoc cref="CacheDesignation" />
        /// </summary>
        public CacheDesignation Designation { get; }

        /// <summary>
        /// Gets the value that represents the installed cache size.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the cache designation.
        /// </summary>
        /// <returns><see cref="CacheDesignation" />.</returns>
        private CacheDesignation GetCacheDesignation()
        {
            string rawCacheType = GetString(0x04);

            if (rawCacheType.Contains("L1"))
                return CacheDesignation.L1;

            if (rawCacheType.Contains("L2"))
                return CacheDesignation.L2;

            if (rawCacheType.Contains("L3"))
                return CacheDesignation.L3;


            return CacheDesignation.Other;
        }
    }

    /// <summary>
    /// Memory information obtained from the SMBIOS table.
    /// </summary>
    public class MemoryDevice : InformationBase
    {
        internal MemoryDevice(byte[] data, IList<string> strings) : base(data, strings)
        {
            Size = GetWord(0x0C);
            FormFactor = (RAMFormFactor)GetByte(0x0E);
            DeviceLocator = GetString(0x10).Trim();
            BankLocator = GetString(0x11).Trim();
            Type = (RAMType)GetByte(0x12);
            TypeDetail = (RAMTypeDetail)GetWord(0x13);
            Speed = GetWord(0x15);
            ManufacturerName = GetString(0x17).Trim();
            SerialNumber = GetString(0x18).Trim();
            PartNumber = GetString(0x1A).Trim();
            MaxVoltage = GetWord(0x24);
            ConfiguredVoltage = GetWord(0x26);

            if (GetWord(0x1C) > 0)
                Size += GetWord(0x1C);
        }

        /// <summary>
        /// Gets a <see cref="RAMFormFactor"/> enum that represents the RAM module form factor.
        /// </summary>
        public RAMFormFactor FormFactor { get; }

        /// <summary>
        /// Gets a <see cref="RAMType"/> enum that represents the RAM module type.
        /// </summary>
        public RAMType Type { get; }

        /// <summary>
        /// Gets a <see cref="RAMTypeDetail"/> enum that represents an additional detail on the RAM module type.
        /// </summary>
        public RAMTypeDetail TypeDetail { get; }

        /// <summary>
        /// Gets the value that represents the configured voltage for this RAM module, in millivolts (mV).
        /// </summary>
        public int ConfiguredVoltage { get; }

        /// <summary>
        /// Gets the value that represents the maximum voltage for this RAM module, in millivolts (mV).
        /// </summary>
        public int MaxVoltage { get; }

        /// <summary>
        /// Gets the string number of the string that identifies the physically labeled bank where the memory device is located.
        /// </summary>
        public string BankLocator { get; }

        /// <summary>
        /// Gets the string number of the string that identifies the physically-labeled socket or board position where the memory device is located.
        /// </summary>
        public string DeviceLocator { get; }

        /// <summary>
        /// Gets the string number for the manufacturer of this memory device.
        /// </summary>
        public string ManufacturerName { get; }

        /// <summary>
        /// Gets the string number for the part number of this memory device.
        /// </summary>
        public string PartNumber { get; }

        /// <summary>
        /// Gets the string number for the serial number of this memory device.
        /// </summary>
        public string SerialNumber { get; }

        /// <summary>
        /// Gets the size of the memory device. If the value is 0, no memory device is installed in the socket.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// Gets the value that identifies the maximum capable speed of the device, in mega transfers per second (MT/s).
        /// </summary>
        public int Speed { get; }
    }

    /// <summary>
    /// Reads and processes information encoded in an SMBIOS table.
    /// </summary>
    public static class SMBIOS
    {
        private static readonly byte[] _raw;

        static SMBIOS()
        {
            List<MemoryDevice> memoryDeviceList = new();
            List<ProcessorCache> processorCacheList = new();

            string[] tables = FirmwareTable.EnumerateTables(FirmwareProvider.RSMB);
            if (tables is { Length: > 0 })
            {
                _raw = FirmwareTable.GetTable(FirmwareProvider.RSMB, tables[0]);
                if (_raw == null || _raw.Length == 0)
                    return;


                byte majorVersion = _raw[1];
                byte minorVersion = _raw[2];

                if (majorVersion > 0 || minorVersion > 0)
                    Version = new Version(majorVersion, minorVersion);

                if (_raw is { Length: > 0 })
                {
                    int offset = 8;
                    byte type = _raw[offset];

                    while (offset + 4 < _raw.Length && type != 127)
                    {
                        type = _raw[offset];
                        int length = _raw[offset + 1];

                        if (offset + length > _raw.Length)
                            break;


                        byte[] data = new byte[length];
                        Array.Copy(_raw, offset, data, 0, length);
                        offset += length;

                        List<string> strings = new();
                        if (offset < _raw.Length && _raw[offset] == 0)
                            offset++;

                        while (offset < _raw.Length && _raw[offset] != 0)
                        {
                            StringBuilder stringBuilder = new();

                            while (offset < _raw.Length && _raw[offset] != 0)
                            {
                                stringBuilder.Append((char)_raw[offset]);
                                offset++;
                            }

                            offset++;

                            strings.Add(stringBuilder.ToString());
                        }

                        offset++;
                        switch (type)
                        {
                            case 0x00:
                                {
                                    Bios = new BiosInformation(data, strings);
                                    break;
                                }
                            case 0x01:
                                {
                                    System = new SystemInformation(data, strings);
                                    break;
                                }
                            case 0x02:
                                {
                                    Board = new BaseBoardInformation(data, strings);
                                    break;
                                }
                            case 0x03:
                                {
                                    Chassis = new ChassisInformation(data, strings);
                                    break;
                                }
                            case 0x04:
                                {
                                    Processor = new ProcessorInformation(data, strings);
                                    break;
                                }
                            case 0x07:
                                {
                                    ProcessorCache processorCache = new(data, strings);
                                    processorCacheList.Add(processorCache);
                                    break;
                                }
                            case 0x11:
                                {
                                    MemoryDevice memoryDevice = new(data, strings);
                                    memoryDeviceList.Add(memoryDevice);
                                    break;
                                }
                        }
                    }
                }
            }

            MemoryDevices = memoryDeviceList.ToArray();
            ProcessorCaches = processorCacheList.ToArray();
        }

        /// <summary>
        /// Gets SMBIOS version
        /// </summary>
        public static Version Version { get; }

        /// <summary>
        /// Gets <inheritdoc cref="BiosInformation" />
        /// </summary>
        public static BiosInformation Bios { get; }

        /// <summary>
        /// Gets <inheritdoc cref="BaseBoardInformation" />
        /// </summary>
        public static BaseBoardInformation Board { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ChassisInformation" />
        /// </summary>
        public static ChassisInformation Chassis { get; }

        /// <summary>
        /// Gets <inheritdoc cref="MemoryDevice" />
        /// </summary>
        public static MemoryDevice[] MemoryDevices { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorInformation" />
        /// </summary>
        public static ProcessorInformation Processor { get; }

        /// <summary>
        /// Gets <inheritdoc cref="ProcessorCache" />
        /// </summary>
        public static ProcessorCache[] ProcessorCaches { get; }

        /// <summary>
        /// Gets <inheritdoc cref="SystemInformation" />
        /// </summary>
        public static SystemInformation System { get; }
    }
}
