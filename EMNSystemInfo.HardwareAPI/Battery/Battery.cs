// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;

namespace EMNSystemInfo.HardwareAPI.Battery
{
    /// <summary>
    /// Class that represents an individual battery.
    /// </summary>
    public class Battery : IDisposable
    {
        private readonly SafeFileHandle _batteryHandle;
        private readonly uint _batteryTag;

        internal Battery(string name,
                         string manufacturer,
                         uint batteryTag,
                         SafeFileHandle batteryHandle,
                         BatteryChemistry chemistry,
                         uint designedCapacity,
                         uint fullChargedCapacity)
        {
            Name = name;
            Manufacturer = manufacturer;
            _batteryTag = batteryTag;
            _batteryHandle = batteryHandle;
            Chemistry = chemistry;
            DesignedCapacity = designedCapacity;
            FullChargedCapacity = fullChargedCapacity;
        }

        /// <summary>
        /// Updates all the battery's properties
        /// </summary>
        public void Update()
        {
            BATTERY_WAIT_STATUS bws = default;
            bws.BatteryTag = _batteryTag;
            BATTERY_STATUS batteryStatus = default;
            if (DeviceIoControl(_batteryHandle,
                                IOCTL_BATTERY.QUERY_STATUS,
                                ref bws,
                                Marshal.SizeOf(bws),
                                ref batteryStatus,
                                Marshal.SizeOf(batteryStatus),
                                out _,
                                IntPtr.Zero))
            {
                PowerState = (BatteryPowerState)batteryStatus.PowerState;

                if (batteryStatus.Capacity != BatteryUnknownValue && FullChargedCapacity != BatteryUnknownValue)
                    ChargeLevel = batteryStatus.Capacity * 100d / FullChargedCapacity;
                else
                    ChargeLevel = null;

                if (FullChargedCapacity != BatteryUnknownValue && DesignedCapacity != BatteryUnknownValue)
                    DegradationLevel = 100d - (FullChargedCapacity * 100d / DesignedCapacity);
                else
                    DegradationLevel = null;

                if (batteryStatus.Capacity != BatteryUnknownValue)
                    RemainingCapacity = batteryStatus.Capacity;
                else
                    RemainingCapacity = null;

                if (batteryStatus.Voltage != BatteryUnknownValue)
                    Voltage = batteryStatus.Voltage / 1000d;
                else
                    Voltage = null;

                if (batteryStatus.Rate != BATTERY_UNKNOWN_RATE)
                    ChargeDischargeRate = batteryStatus.Rate / 1000d;
                else
                    ChargeDischargeRate = null;

                if (batteryStatus.Rate == BATTERY_UNKNOWN_RATE || batteryStatus.Voltage == BatteryUnknownValue)
                    ChargeDischargeCurrent = null;
                else
                    ChargeDischargeCurrent = (double)batteryStatus.Rate / (double)batteryStatus.Voltage;

                uint estimatedRunTime = BatteryUnknownValue;
                BATTERY_QUERY_INFORMATION bqi = default;
                bqi.BatteryTag = _batteryTag;
                bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryEstimatedTime;
                if (DeviceIoControl(_batteryHandle,
                                    IOCTL_BATTERY.QUERY_INFORMATION,
                                    ref bqi,
                                    Marshal.SizeOf(bqi),
                                    ref estimatedRunTime,
                                    Marshal.SizeOf<uint>(),
                                    out _,
                                    IntPtr.Zero) && estimatedRunTime != BatteryUnknownValue)
                {
                    EstimatedRemainingTime = TimeSpan.FromSeconds(estimatedRunTime);
                }
                else
                {
                    EstimatedRemainingTime = null;
                }
            }
        }

        /// <summary>
        /// Gets the battery name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the battery manufacturer
        /// </summary>
        public string Manufacturer { get; }

        /// <summary>
        /// Gets the battery chemistry
        /// </summary>
        public BatteryChemistry Chemistry { get; }

        /// <summary>
        /// Gets the battery's power state
        /// </summary>
        public BatteryPowerState PowerState { get; internal set; } = BatteryPowerState.Unknown;

        /// <summary>
        /// Gets the battery charge level. This property is nullable.
        /// </summary>
        public double? ChargeLevel { get; internal set; }

        /// <summary>
        /// Gets the battery degradation level. This property is nullable.
        /// </summary>
        public double? DegradationLevel { get; internal set; }

        /// <summary>
        /// Gets the battery designed capacity, in milliwatt-hours (mWh).
        /// </summary>
        public uint DesignedCapacity { get; }

        /// <summary>
        /// Gets the battery full-charged capacity, in milliwatt-hours (mWh).
        /// </summary>
        public uint FullChargedCapacity { get; }

        /// <summary>
        /// Gets the battery remaining capacity, in milliwatt-hours (mWh). This property is nullable.
        /// </summary>
        public uint? RemainingCapacity { get; internal set; }

        /// <summary>
        /// Gets the battery voltage, in volts (V). This property is nullable.
        /// </summary>
        public double? Voltage { get; internal set; }

        /// <summary>
        /// Gets the battery charge/discharge rate, in watts (W). This property is nullable.
        /// </summary>
        /// <remarks>If the returned value is greater than 0, the battery is charging; if it's less than 0, the battery is discharging.</remarks>
        public double? ChargeDischargeRate { get; internal set; }

        /// <summary>
        /// Gets the battery charge/discharge current, in amperes (A). This property is nullable.
        /// </summary>
        /// <remarks>If the returned value is greater than 0, the battery is charging; if it's less than 0, the battery is discharging.</remarks>
        public double? ChargeDischargeCurrent { get; internal set; }

        /// <summary>
        /// Gets the battery estimated remaining time. This property is nullable.
        /// </summary>
        public TimeSpan? EstimatedRemainingTime { get; internal set; }

        /// <summary>
        /// Frees the resources used in this class. It is not necessary to call this method, <see cref="Batteries.Dispose"/> does all the work.
        /// </summary>
        public void Dispose()
        {
            _batteryHandle.Close();
        }
    }
}
