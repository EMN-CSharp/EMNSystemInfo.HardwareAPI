// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;
using static EMNSystemInfo.HardwareAPI.NativeInterop.SetupAPI;

namespace EMNSystemInfo.HardwareAPI.Battery
{
    /// <summary>
    /// Battery power status
    /// </summary>
    public enum BatteryPowerState
    {
        /// <summary>
        /// Power status is unknown
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Battery is charging
        /// </summary>
        Charging = 0x00000004,

        /// <summary>
        /// Battery is in critical state
        /// </summary>
        Critical = 0x00000008,

        /// <summary>
        /// Battery is discharging
        /// </summary>
        Discharging = 0x00000002,

        /// <summary>
        /// THe system is connected to AC power, thus, no batteries are discharging.
        /// </summary>
        OnAC = 0x00000001
    }

    /// <summary>
    /// Battery chemistry
    /// </summary>
    public enum BatteryChemistry
    {
        /// <summary>
        /// Battery chemistry is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// Lead-Acid (Pb-Ac)
        /// </summary>
        LeadAcid,

        /// <summary>
        /// Nickel-Cadmium (Ni-Cd)
        /// </summary>
        NickelCadmium,

        /// <summary>
        /// Nickel-Metal Hydride (Ni-MH)
        /// </summary>
        NickelMetalHydride,

        /// <summary>
        /// Lithium Ion (Li-Ion)
        /// </summary>
        LithiumIon,

        /// <summary>
        /// Nickel-Zinc (Ni-Zn)
        /// </summary>
        NickelZinc,

        /// <summary>
        /// Rechargeable alkaline-manganese
        /// </summary>
        AlkalineManganese
    }

    /// <summary>
    /// Batteries information. It is not required to initialize the library to use this class, nor administrator rights are required.
    /// </summary>
    public static partial class Batteries
    {
        /// <summary>
        /// Gets if there is a battery or multiple batteries installed.
        /// </summary>
        public static bool Exist
        {
            get
            {
                SYSTEM_POWER_STATUS sps = default;
                GetSystemPowerStatus(ref sps);

                if (sps.BatteryFlag == 0x80 /*NoSystemBattery*/)
                {
                    return false;
                }
                return true;
            }
        }

        /// <summary>
        /// Loads all the installed batteries into the <see cref="List"/> property.
        /// </summary>
        public unsafe static void LoadInstalledBatteries()
        {
            List<Battery> batteries = new();

            IntPtr hDevice = SetupDiGetClassDevs(ref GUID_DEVCLASS_BATTERY,
                                                 IntPtr.Zero,
                                                 IntPtr.Zero,
                                                 DIGCF_PRESENT | DIGCF_DEVICEINTERFACE);
            if (hDevice != INVALID_HANDLE_VALUE)
            {
                for (uint i = 0; ; i++)
                {
                    SP_DEVICE_INTERFACE_DATA did = default;
                    did.cbSize = (uint)Marshal.SizeOf<SP_DEVICE_INTERFACE_DATA>();

                    if (!SetupDiEnumDeviceInterfaces(hDevice,
                                                     IntPtr.Zero,
                                                     ref GUID_DEVCLASS_BATTERY,
                                                     i,
                                                     ref did))
                    {
                        if (Marshal.GetLastWin32Error() == ERROR_NO_MORE_ITEMS)
                            break;
                    }
                    else
                    {
                        SetupDiGetDeviceInterfaceDetail(hDevice,
                                                        did,
                                                        IntPtr.Zero,
                                                        0,
                                                        out uint cbRequired,
                                                        IntPtr.Zero);

                        if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER)
                        {
                            IntPtr pdidd = LocalAlloc(LPTR, cbRequired);
                            Marshal.WriteInt32(pdidd, Environment.Is64BitOperatingSystem ? 8 : 4); // cbSize.

                            if (SetupDiGetDeviceInterfaceDetail(hDevice,
                                                                did,
                                                                pdidd,
                                                                cbRequired,
                                                                out _,
                                                                IntPtr.Zero))
                            {
                                string devicePath = new string((char*)(pdidd + 4));

                                SafeFileHandle battery = CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
                                if (!battery.IsInvalid)
                                {
                                    BATTERY_QUERY_INFORMATION bqi = default;

                                    uint dwWait = 0;
                                    if (DeviceIoControl(battery,
                                                        IOCTL_BATTERY.QUERY_TAG,
                                                        ref dwWait,
                                                        Marshal.SizeOf(dwWait),
                                                        ref bqi.BatteryTag,
                                                        Marshal.SizeOf(bqi.BatteryTag),
                                                        out _,
                                                        IntPtr.Zero))
                                    {
                                        BATTERY_INFORMATION bi = default;
                                        bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryInformation;

                                        if (DeviceIoControl(battery,
                                                            IOCTL_BATTERY.QUERY_INFORMATION,
                                                            ref bqi,
                                                            Marshal.SizeOf(bqi),
                                                            ref bi,
                                                            Marshal.SizeOf(bi),
                                                            out _,
                                                            IntPtr.Zero))
                                        {
                                            if (bi.Capabilities == BatteryCapabilities.BATTERY_SYSTEM_BATTERY)
                                            {
                                                const int MAX_LOADSTRING = 100;

                                                IntPtr ptrDeviceName = Marshal.AllocHGlobal(MAX_LOADSTRING);
                                                bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryDeviceName;

                                                if (DeviceIoControl(battery,
                                                                    IOCTL_BATTERY.QUERY_INFORMATION,
                                                                    ref bqi,
                                                                    Marshal.SizeOf(bqi),
                                                                    ptrDeviceName,
                                                                    MAX_LOADSTRING,
                                                                    out _,
                                                                    IntPtr.Zero))
                                                {
                                                    IntPtr ptrManufactureName = Marshal.AllocHGlobal(MAX_LOADSTRING);
                                                    bqi.InformationLevel = BATTERY_QUERY_INFORMATION_LEVEL.BatteryManufactureName;

                                                    if (DeviceIoControl(battery,
                                                                        IOCTL_BATTERY.QUERY_INFORMATION,
                                                                        ref bqi,
                                                                        Marshal.SizeOf(bqi),
                                                                        ptrManufactureName,
                                                                        MAX_LOADSTRING,
                                                                        out _,
                                                                        IntPtr.Zero))
                                                    {
                                                        string name = Marshal.PtrToStringUni(ptrDeviceName);
                                                        string manufacturer = Marshal.PtrToStringUni(ptrManufactureName);

                                                        BatteryChemistry chemistry = BatteryChemistry.Unknown;
                                                        if (bi.Chemistry.SequenceEqual(new[] { 'P', 'b', 'A', 'c' }))
                                                        {
                                                            chemistry = BatteryChemistry.LeadAcid;
                                                        }
                                                        else if (bi.Chemistry.SequenceEqual(new[] { 'L', 'I', 'O', 'N' }) || bi.Chemistry.SequenceEqual(new[] { 'L', 'i', '-', 'I' }))
                                                        {
                                                            chemistry = BatteryChemistry.LithiumIon;
                                                        }
                                                        else if (bi.Chemistry.SequenceEqual(new[] { 'N', 'i', 'C', 'd' }))
                                                        {
                                                            chemistry = BatteryChemistry.NickelCadmium;
                                                        }
                                                        else if (bi.Chemistry.SequenceEqual(new[] { 'N', 'i', 'M', 'H' }))
                                                        {
                                                            chemistry = BatteryChemistry.NickelMetalHydride;
                                                        }
                                                        else if (bi.Chemistry.SequenceEqual(new[] { 'N', 'i', 'Z', 'n' }))
                                                        {
                                                            chemistry = BatteryChemistry.NickelZinc;
                                                        }
                                                        else if (bi.Chemistry.SequenceEqual(new[] { 'R', 'A', 'M', '\x00' }))
                                                        {
                                                            chemistry = BatteryChemistry.AlkalineManganese;
                                                        }

                                                        batteries.Add(new Battery(name,
                                                                             manufacturer,
                                                                             bqi.BatteryTag,
                                                                             battery,
                                                                             chemistry,
                                                                             bi.DesignedCapacity,
                                                                             bi.FullChargedCapacity));
                                                    }

                                                    Marshal.FreeHGlobal(ptrManufactureName);
                                                }

                                                Marshal.FreeHGlobal(ptrDeviceName);
                                            }
                                        }
                                    }
                                }
                            }

                            LocalFree(pdidd);
                        }
                    }
                }

                SetupDiDestroyDeviceInfoList(hDevice);
            }

            List = batteries.ToArray();
        }

        /// <summary>
        /// Frees the resources used by <see cref="Battery"/> instances.
        /// </summary>
        public static void DisposeAllBatteries()
        {
            foreach (Battery bat in List)
            {
                bat.Dispose();
            }
            List = Array.Empty<Battery>();
        }

        /// <summary>
        /// Batteries list.
        /// </summary>
        public static Battery[] List { get; private set; } = Array.Empty<Battery>();
    }
}
