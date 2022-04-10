// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using EMNSystemInfo.HardwareAPI.NativeInterop;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    [NamePrefix("")]
    public class GenericHardDisk : ATADrive
    {
        private const byte TEMPERATURE1_ATTRIBUTE = 0xC2;
        private const byte TEMPERATURE2_ATTRIBUTE = 0xE7;
        private const byte TEMPERATUREDIFFERENCEFROM100_ATTRIBUTE = 0xBE;

        private static readonly List<SMARTAttribute> _smartAttributes = new List<SMARTAttribute>
        {
            new SMARTAttribute(0x01, SMARTNames.ReadErrorRate),
            new SMARTAttribute(0x02, SMARTNames.ThroughputPerformance),
            new SMARTAttribute(0x03, SMARTNames.SpinUpTime),
            new SMARTAttribute(0x04, SMARTNames.StartStopCount, RawToInt),
            new SMARTAttribute(0x05, SMARTNames.ReallocatedSectorsCount),
            new SMARTAttribute(0x06, SMARTNames.ReadChannelMargin),
            new SMARTAttribute(0x07, SMARTNames.SeekErrorRate),
            new SMARTAttribute(0x08, SMARTNames.SeekTimePerformance),
            new SMARTAttribute(POWERONHOURS_ATTRIBUTE, SMARTNames.PowerOnHours, RawToInt),
            new SMARTAttribute(0x0A, SMARTNames.SpinRetryCount),
            new SMARTAttribute(0x0B, SMARTNames.RecalibrationRetries),
            new SMARTAttribute(POWERCYCLECOUNT_ATTRIBUTE, SMARTNames.PowerCycleCount, RawToInt),
            new SMARTAttribute(0x0D, SMARTNames.SoftReadErrorRate),
            new SMARTAttribute(0xAA, SMARTNames.Unknown),
            new SMARTAttribute(0xAB, SMARTNames.Unknown),
            new SMARTAttribute(0xAC, SMARTNames.Unknown),
            new SMARTAttribute(0xB7, SMARTNames.SataDownshiftErrorCount, RawToInt),
            new SMARTAttribute(0xB8, SMARTNames.EndToEndError),
            new SMARTAttribute(0xB9, SMARTNames.HeadStability),
            new SMARTAttribute(0xBA, SMARTNames.InducedOpVibrationDetection),
            new SMARTAttribute(0xBB, SMARTNames.ReportedUncorrectableErrors, RawToInt),
            new SMARTAttribute(0xBC, SMARTNames.CommandTimeout, RawToInt),
            new SMARTAttribute(0xBD, SMARTNames.HighFlyWrites),
            new SMARTAttribute(0xBF, SMARTNames.GSenseErrorRate),
            new SMARTAttribute(0xC0, SMARTNames.EmergencyRetractCycleCount),
            new SMARTAttribute(0xC1, SMARTNames.LoadCycleCount),
            new SMARTAttribute(0xC3, SMARTNames.HardwareEccRecovered),
            new SMARTAttribute(0xC4, SMARTNames.ReallocationEventCount),
            new SMARTAttribute(0xC5, SMARTNames.CurrentPendingSectorCount),
            new SMARTAttribute(0xC6, SMARTNames.UncorrectableSectorCount),
            new SMARTAttribute(0xC7, SMARTNames.UltraDmaCrcErrorCount),
            new SMARTAttribute(0xC8, SMARTNames.WriteErrorRate),
            new SMARTAttribute(0xCA, SMARTNames.DataAddressMarkErrors),
            new SMARTAttribute(0xCB, SMARTNames.RunOutCancel),
            new SMARTAttribute(0xCC, SMARTNames.SoftEccCorrection),
            new SMARTAttribute(0xCD, SMARTNames.ThermalAsperityRate),
            new SMARTAttribute(0xCE, SMARTNames.FlyingHeight),
            new SMARTAttribute(0xCF, SMARTNames.SpinHighCurrent),
            new SMARTAttribute(0xD0, SMARTNames.SpinBuzz),
            new SMARTAttribute(0xD1, SMARTNames.OfflineSeekPerformance),
            new SMARTAttribute(0xD3, SMARTNames.VibrationDuringWrite),
            new SMARTAttribute(0xD4, SMARTNames.ShockDuringWrite),
            new SMARTAttribute(0xDC, SMARTNames.DiskShift),
            new SMARTAttribute(0xDD, SMARTNames.AlternativeGSenseErrorRate),
            new SMARTAttribute(0xDE, SMARTNames.LoadedHours),
            new SMARTAttribute(0xDF, SMARTNames.LoadUnloadRetryCount),
            new SMARTAttribute(0xE0, SMARTNames.LoadFriction),
            new SMARTAttribute(0xE1, SMARTNames.LoadUnloadCycleCount),
            new SMARTAttribute(0xE2, SMARTNames.LoadInTime),
            new SMARTAttribute(0xE3, SMARTNames.TorqueAmplificationCount),
            new SMARTAttribute(0xE4, SMARTNames.PowerOffRetractCycle),
            new SMARTAttribute(0xE6, SMARTNames.GmrHeadAmplitude),
            new SMARTAttribute(0xE8, SMARTNames.EnduranceRemaining),
            new SMARTAttribute(0xE9, SMARTNames.PowerOnHours),
            new SMARTAttribute(0xF0, SMARTNames.HeadFlyingHours),
            new SMARTAttribute(0xF1, SMARTNames.TotalLbasWritten),
            new SMARTAttribute(0xF2, SMARTNames.TotalLbasRead),
            new SMARTAttribute(0xFA, SMARTNames.ReadErrorRetryRate),
            new SMARTAttribute(0xFE, SMARTNames.FreeFallProtection),
            new SMARTAttribute(TEMPERATURE1_ATTRIBUTE, SMARTNames.Temperature, (r, v, p) => r[0] + (p ?? 0), 0),
            new SMARTAttribute(TEMPERATURE2_ATTRIBUTE, SMARTNames.Temperature, (r, v, p) => r[0] + (p ?? 0), 0),
            new SMARTAttribute(TEMPERATUREDIFFERENCEFROM100_ATTRIBUTE, SMARTNames.TemperatureDifferenceFrom100, (r, v, p) => r[0] + (p ?? 0), 0)
        };

        internal GenericHardDisk(StorageInfo storageInfo, ISmart smart)
            : base(storageInfo, smart, _smartAttributes)
        {
            Type = PhysicalDriveType.HDD;
        }

        internal override void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values)
        {
            foreach (Kernel32.SMART_ATTRIBUTE attr in values)
            {
                if (attr.Id == POWERONHOURS_ATTRIBUTE)
                {
                    PowerOnTime = TimeSpan.FromHours(RawToInt(attr.RawValue, 0, null));
                }
                if (attr.Id == POWERCYCLECOUNT_ATTRIBUTE)
                {
                    PowerCycleCount = (ulong)RawToInt(attr.RawValue, 0, null);
                }

                if (attr.Id == TEMPERATURE1_ATTRIBUTE || attr.Id == TEMPERATURE2_ATTRIBUTE)
                {
                    Temperature = attr.RawValue[0];
                }
                else if (attr.Id == TEMPERATUREDIFFERENCEFROM100_ATTRIBUTE)
                {
                    Temperature = 100 - attr.RawValue[0];
                }
            }
        }
    }
}
