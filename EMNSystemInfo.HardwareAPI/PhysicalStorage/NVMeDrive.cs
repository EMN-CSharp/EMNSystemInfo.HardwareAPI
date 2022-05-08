// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    public enum NVMeCriticalWarning
    {
        /// <summary>
        /// Available spare space has fallen below the threshold.
        /// </summary>
        AvailableSpaceLow,

        /// <summary>
        /// A temperature sensor is above an over-temperature threshold or an under-temperature threshold.
        /// </summary>
        TemperatureThreshold,

        /// <summary>
        /// Device reliability has been degraded due to significant media-related errors or any internal error that degrades device reliability.
        /// </summary>
        ReliabilityDegraded,

        /// <summary>
        /// Device has entered in read-only mode
        /// </summary>
        ReadOnly,

        /// <summary>
        /// Volatile memory backup device has failed
        /// </summary>
        VolatileMemoryBackupDeviceFailed,
    }

    /// <summary>
    /// Class that represents an individual NVMe drive.
    /// </summary>
    public sealed class NVMeDrive : Drive
    {
        private const ulong Units = 512000;
        private readonly NVMeInfo _info;

        public ushort PCIVendorID => _info.VID;

        public ushort PCISubsystemVendorID => _info.SSVID;

        public string IEEEOuiIdentifier => _info.IEEE[2].ToString("X2") + _info.IEEE[1].ToString("X2") + _info.IEEE[0].ToString("X2");

        public ulong TotalNVMCapacity => _info.TotalCapacity;

        public ulong UnallocatedNVMCapacity => _info.UnallocatedCapacity;

        public ushort ControllerID => _info.ControllerId;

        public ulong NumberOfNamespaces => _info.NumberNamespaces;

        public NVMeCriticalWarning[] CriticalWarnings
        {
            get
            {
                NVMeHealthInfo healthInfo = Smart.GetHealthInfo();
                List<NVMeCriticalWarning> warnings = new();
                if (healthInfo.CriticalWarning != NVME_CRITICAL_WARNING.None)
                {
                    if ((healthInfo.CriticalWarning & NVME_CRITICAL_WARNING.AvailableSpaceLow) != 0)
                        warnings.Add(NVMeCriticalWarning.AvailableSpaceLow);
                    if ((healthInfo.CriticalWarning & NVME_CRITICAL_WARNING.TemperatureThreshold) != 0)
                        warnings.Add(NVMeCriticalWarning.TemperatureThreshold);
                    if ((healthInfo.CriticalWarning & NVME_CRITICAL_WARNING.ReliabilityDegraded) != 0)
                        warnings.Add(NVMeCriticalWarning.ReliabilityDegraded);
                    if ((healthInfo.CriticalWarning & NVME_CRITICAL_WARNING.ReadOnly) != 0)
                        warnings.Add(NVMeCriticalWarning.ReadOnly);
                    if ((healthInfo.CriticalWarning & NVME_CRITICAL_WARNING.VolatileMemoryBackupDeviceFailed) != 0)
                        warnings.Add(NVMeCriticalWarning.VolatileMemoryBackupDeviceFailed);
                }
                return warnings.ToArray();
            }
        }

        public double? Temperature { get; private set; }

        public double[] TemperatureSensors { get; private set; }

        public double? AvailableSpare { get; private set; }

        public double? AvailableSpareThreshold { get; private set; }

        public double? PercentageUsed { get; private set; }

        public ulong? DataRead { get; private set; }

        public ulong? DataWritten { get; private set; }

        public ulong? HostReadCommands { get; private set; }

        public ulong? HostWriteCommands { get; private set; }

        public ulong? ControllerBusyTime { get; private set; }

        public ulong? PowerCycles { get; private set; }

        public TimeSpan? PowerOnTime { get; private set; }

        public ulong? UnsafeShutdowns { get; private set; }

        public ulong? MediaErrors { get; private set; }

        public ulong? ErrorInfoLogCount { get; private set; }

        public uint? WarningCompositeTemperatureTime { get; private set; }

        public uint? CriticalCompositeTemperatureTime { get; private set; }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            NVMeHealthInfo healthInfo = Smart.GetHealthInfo();

            if (ValueIsInRange(healthInfo.Temperature))
                Temperature = healthInfo.Temperature;

            List<double> tempSensors = new();
            foreach (short tempSensor in healthInfo.TemperatureSensors)
            {
                if (ValueIsInRange(tempSensor))
                    tempSensors.Add(tempSensor);
            }
            TemperatureSensors = tempSensors.ToArray();

            AvailableSpare = healthInfo.AvailableSpare;

            AvailableSpareThreshold = healthInfo.AvailableSpareThreshold;

            PercentageUsed = healthInfo.PercentageUsed;

            DataRead = UnitsToData(healthInfo.DataUnitRead);

            DataWritten = UnitsToData(healthInfo.DataUnitWritten);

            HostReadCommands = healthInfo.HostReadCommands;

            HostWriteCommands = healthInfo.HostWriteCommands;

            ControllerBusyTime = healthInfo.ControllerBusyTime;

            PowerCycles = healthInfo.PowerCycle;

            PowerOnTime = TimeSpan.FromHours(healthInfo.PowerOnHours);

            UnsafeShutdowns = healthInfo.UnsafeShutdowns;

            MediaErrors = healthInfo.MediaErrors;

            ErrorInfoLogCount = healthInfo.ErrorInfoLogEntryCount;

            WarningCompositeTemperatureTime = healthInfo.WarningCompositeTemperatureTime;

            CriticalCompositeTemperatureTime = healthInfo.CriticalCompositeTemperatureTime;
        }

        private bool ValueIsInRange(double value) => value >= -1000 && value <= 1000;

        /// <summary>
        /// Gets the SMART data.
        /// </summary>
        internal NVMeSmart Smart { get; private set; }

        internal NVMeDrive(StorageInfo storageInfo, NVMeInfo info)
            : base(storageInfo)
        {
            Type = PhysicalDriveType.NVMe;

            Smart = new NVMeSmart(storageInfo);
            _info = info;
        }

        private static NVMeInfo GetDeviceInfo(StorageInfo storageInfo)
        {
            var smart = new NVMeSmart(storageInfo);
            return smart.GetInfo();
        }

        internal static Drive CreateInstance(StorageInfo storageInfo)
        {
            NVMeInfo nvmeInfo = GetDeviceInfo(storageInfo);
            return nvmeInfo == null ? null : new NVMeDrive(storageInfo, nvmeInfo);
        }

        private static ulong UnitsToData(ulong u)
        {
            // One unit is 512000 bytes
            return Units * u;
        }

        /// <inheritdoc/>
        public override void Close()
        {
            Smart?.Close();

            base.Close();
        }

        private delegate float GetSensorValue(NVMeHealthInfo health);
    }
}
