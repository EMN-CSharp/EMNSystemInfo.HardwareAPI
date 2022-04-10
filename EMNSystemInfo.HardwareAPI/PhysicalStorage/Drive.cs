// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    public enum PhysicalDriveType
    {
        Generic,
        HDD,
        SSD,
        NVMe
    }

    public class Drive
    {
        private readonly DrivePerformanceCounters _drivePCs;
        private readonly StorageInfo _storageInfo;
        private readonly TimeSpan _updateInterval = TimeSpan.FromSeconds(60);

        private DateTime _lastUpdate = DateTime.MinValue;
        private double _usageSensor;

        public string Name { get; protected set; }

        public string SerialNumber { get; protected set; }

        public ulong Capacity => _storageInfo.DiskSize;

        public string FirmwareRevision { get; protected set; }

        public DriveGeometry Geometry { get; protected set; }

        public bool IsATADrive { get; protected set; }

        public bool IsRemovable => _storageInfo.Removable;

        public DriveInfo[] LogicalDrives { get; }

        public string DeviceId => _storageInfo.DeviceId;

        public int Index => _storageInfo.Index;

        public PhysicalDriveType Type { get; internal set; } = PhysicalDriveType.Generic;

        public double UsedCapacityPercentage => _usageSensor;

        public double? TotalActivityPercentage { get; internal set; }

        public double? TotalReadActivityPercentage { get; internal set; }

        public double? TotalWriteActivityPercentage { get; internal set; }

        public double? ReadSpeed { get; internal set; }

        public double? WriteSpeed { get; internal set; }

        public double? AverageResponseTimePerTransfer { get; internal set; }

        public double? AverageResponseTimePerRead { get; internal set; }

        public double? AverageResponseTimePerWrite { get; internal set; }

        internal Drive(StorageInfo storageInfo)
        {
            Geometry = storageInfo.DriveGeometry;
            _storageInfo = storageInfo;
            Name = storageInfo.Name;
            SerialNumber = storageInfo.Serial;
            FirmwareRevision = storageInfo.Revision;

            try
            {
                _drivePCs = new(storageInfo.Index);
            }
            catch (Exception)
            { }

            string[] logicalDrives = WindowsStorage.GetLogicalDrives(storageInfo.Index);
            var driveInfoList = new List<DriveInfo>(logicalDrives.Length);

            foreach (string logicalDrive in logicalDrives)
            {
                try
                {
                    var di = new DriveInfo(logicalDrive);
                    if (di.TotalSize > 0)
                        driveInfoList.Add(new DriveInfo(logicalDrive));
                }
                catch (ArgumentException)
                { }
                catch (IOException)
                { }
                catch (UnauthorizedAccessException)
                { }
            }

            LogicalDrives = driveInfoList.ToArray();
        }

        internal static Drive CreateInstance(string deviceId, uint driveNumber, ulong diskSize, int scsiPort, uint heads, ulong tracks, ulong sectors)
        {
            StorageInfo info = WindowsStorage.GetStorageInfo(deviceId, driveNumber);
            if (info == null)
                return null;

            info.DiskSize = diskSize;
            info.DeviceId = deviceId;
            info.Scsi = $@"\\.\SCSI{scsiPort}:";

            info.DriveGeometry.Heads = heads;
            info.DriveGeometry.Tracks = tracks;
            info.DriveGeometry.Sectors = sectors;

            if (info.BusType is Kernel32.STORAGE_BUS_TYPE.BusTypeVirtual or Kernel32.STORAGE_BUS_TYPE.BusTypeFileBackedVirtual)
                return null;

            if (info.Removable)
            {
                Drive drive = new(info);
                if (drive != null)
                    return drive;
            }

            // Fallback, when it is not possible to read out with the NVMe implementation,
            // Try it with the SATA S.M.A.R.T. implementation
            if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme)
            {
                Drive x = NVMeDrive.CreateInstance(info);
                if (x != null)
                    return x;
            }

            if (info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeAta ||
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeSata ||
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeNvme ||
                info.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeRAID)
            {
                return ATADrive.CreateInstance(info);
            }

            return new Drive(info);
        }

        protected virtual void UpdateSensors()
        { }

        public virtual void Update()
        {
            //read out with updateInterval
            TimeSpan tDiff = DateTime.UtcNow - _lastUpdate;
            if (tDiff > _updateInterval)
            {
                TotalActivityPercentage = _drivePCs?.DriveTime;
                TotalReadActivityPercentage = _drivePCs?.ReadTime;
                TotalWriteActivityPercentage = _drivePCs?.WriteTime;
                ReadSpeed = _drivePCs?.ReadSpeed;
                WriteSpeed = _drivePCs?.WriteSpeed;
                AverageResponseTimePerTransfer = _drivePCs?.AverageResponseTimePerTransfer;
                AverageResponseTimePerRead = _drivePCs?.AverageResponseTimePerRead;
                AverageResponseTimePerWrite = _drivePCs?.AverageResponseTimePerWrite;

                _lastUpdate = DateTime.UtcNow;

                UpdateSensors();

                long totalSize = 0;
                long totalFreeSpace = 0;

                for (int i = 0; i < LogicalDrives.Length; i++)
                {
                    if (!LogicalDrives[i].IsReady)
                        continue;

                    try
                    {
                        totalSize += LogicalDrives[i].TotalSize;
                        totalFreeSpace += LogicalDrives[i].TotalFreeSpace;
                    }
                    catch (IOException)
                    { }
                    catch (UnauthorizedAccessException)
                    { }
                }

                if (totalSize > 0)
                    _usageSensor = 100.0f - (100.0f * totalFreeSpace) / totalSize;
            }
        }

        public virtual void Close() { }
    }
}
