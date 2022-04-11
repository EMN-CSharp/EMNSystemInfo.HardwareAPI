// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    internal static class WindowsStorage
    {
        internal static StorageInfo GetStorageInfo(string deviceId, uint driveIndex)
        {
            using (SafeHandle handle = Kernel32.OpenDevice(deviceId))
            {
                if (handle == null || handle.IsInvalid)
                    return null;


                var query = new Kernel32.STORAGE_PROPERTY_QUERY { PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageDeviceProperty, QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery };

                if (!Kernel32.DeviceIoControl(handle,
                                              Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY,
                                              ref query,
                                              Marshal.SizeOf(query),
                                              out Kernel32.STORAGE_DEVICE_DESCRIPTOR_HEADER header,
                                              Marshal.SizeOf<Kernel32.STORAGE_DEVICE_DESCRIPTOR_HEADER>(),
                                              out _,
                                              IntPtr.Zero))
                {
                    return null;
                }

                IntPtr descriptorPtr = Marshal.AllocHGlobal((int)header.Size);
                try
                {
                    if (!Kernel32.DeviceIoControl(handle, Kernel32.IOCTL.IOCTL_STORAGE_QUERY_PROPERTY, ref query, Marshal.SizeOf(query), descriptorPtr, header.Size, out _, IntPtr.Zero))
                        return null;

                    Kernel32.DeviceIoControl(handle,
                                             Kernel32.IOCTL.IOCTL_DISK_GET_DRIVE_GEOMETRY,
                                             IntPtr.Zero,
                                             0,
                                             out Kernel32.DISK_GEOMETRY geometry,
                                             Marshal.SizeOf<Kernel32.DISK_GEOMETRY>(),
                                             out _,
                                             IntPtr.Zero);

                    return new StorageInfo((int)driveIndex, descriptorPtr, geometry);
                }
                finally
                {
                    Marshal.FreeHGlobal(descriptorPtr);
                }
            }
        }

        public static string[] GetLogicalDrives(int driveIndex)
        {
            var list = new List<string>();

            try
            {
                using (var s = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_DiskPartition " + "WHERE DiskIndex = " + driveIndex))
                {
                    using (ManagementObjectCollection dpc = s.Get())
                    {
                        foreach (ManagementBaseObject o in dpc)
                        {
                            if (o is ManagementObject dp)
                            {
                                using (ManagementObjectCollection ldc = dp.GetRelated("Win32_LogicalDisk"))
                                {
                                    foreach (ManagementBaseObject ld in ldc)
                                    {
                                        list.Add(((string)ld["Name"]).TrimEnd(':'));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Ignored.
            }

            return list.ToArray();
        }

        internal class StorageInfo : PhysicalStorage.StorageInfo
        {
            public StorageInfo(int index, IntPtr descriptorPtr, Kernel32.DISK_GEOMETRY driveGeometry)
            {
                Kernel32.STORAGE_DEVICE_DESCRIPTOR descriptor = Marshal.PtrToStructure<Kernel32.STORAGE_DEVICE_DESCRIPTOR>(descriptorPtr);
                Index = index;
                Vendor = GetString(descriptorPtr, descriptor.VendorIdOffset);
                Product = GetString(descriptorPtr, descriptor.ProductIdOffset);
                Revision = GetString(descriptorPtr, descriptor.ProductRevisionOffset);
                Serial = GetString(descriptorPtr, descriptor.SerialNumberOffset);
                BusType = descriptor.BusType;
                Removable = descriptor.RemovableMedia;
                RawData = new byte[descriptor.Size];
                Marshal.Copy(descriptorPtr, RawData, 0, RawData.Length);

                DriveGeometry.Cylinders = driveGeometry.Cylinders;
                DriveGeometry.TracksPerCylinder = driveGeometry.TracksPerCylinder;
                DriveGeometry.SectorsPerTrack = driveGeometry.SectorsPerTrack;
                DriveGeometry.BytesPerSector = driveGeometry.BytesPerSector;
            }

            private static string GetString(IntPtr descriptorPtr, uint offset)
            {
                return offset > 0 ? Marshal.PtrToStringAnsi(new IntPtr(descriptorPtr.ToInt64() + offset))?.Trim() : string.Empty;
            }
        }
    }
}
