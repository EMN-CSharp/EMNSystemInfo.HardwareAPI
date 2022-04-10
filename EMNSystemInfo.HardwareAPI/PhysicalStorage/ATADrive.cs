// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    public class SMARTSensor
    {
        public SMARTAttribute Attribute { get; internal set; }

        public byte NormalizedValue { get; internal set; }

        public byte WorstValue { get; internal set; }

        public byte Threshold { get; internal set; }

        public byte[] RawValue { get; internal set; }

        public double Value { get; internal set; }
    }

    public abstract class ATADrive : Drive
    {
        protected const byte POWERONHOURS_ATTRIBUTE = 0x09;
        protected const byte POWERCYCLECOUNT_ATTRIBUTE = 0x0C;

        // array of all hard drive types, matching type is searched in this order
        private static readonly Type[] _hddTypes = { typeof(SSDPlextor), typeof(SSDIntel), typeof(SSDSandforce), typeof(SSDIndilinx), typeof(SSDSamsung), typeof(SSDMicron), typeof(GenericHardDisk) };

        private readonly List<SMARTSensor> _sensors = new();

        public SMARTSensor[] SMARTSensors
        {
            get
            {
                if (_sensors.Count == 0)
                {
                    if (Smart.IsValid)
                    {
                        byte[] smartIds = Smart.ReadSmartData().Select(x => x.Id).ToArray();

                        IEnumerable<SMARTAttribute> smartAttrs = SmartAttributes
                                              .Where(x => smartIds.Contains(x.Id));

                        foreach (SMARTAttribute attr in smartAttrs)
                        {
                            _sensors.Add(new() { Attribute = attr });
                        }
                    }
                }

                if (_sensors.Count > 0)
                {
                    Kernel32.SMART_ATTRIBUTE[] smartAttributes = Smart.ReadSmartData();
                    Kernel32.SMART_THRESHOLD[] smartThresholds = Smart.ReadSmartThresholds();
                    int count = 0;
                    var smartAttrsThrs = smartThresholds.ToDictionary((thr) =>
                    {
                        count++;
                        return smartAttributes[count - 1];
                    });

                    foreach (SMARTSensor smartSensor in _sensors)
                    {
                        foreach (var smartAttrThr in smartAttrsThrs)
                        {
                            if (smartAttrThr.Key.Id == smartSensor.Attribute.Id)
                            {
                                smartSensor.NormalizedValue = smartAttrThr.Key.CurrentValue;
                                smartSensor.WorstValue = smartAttrThr.Key.WorstValue;
                                smartSensor.Threshold = smartAttrThr.Value.Threshold;
                                smartSensor.RawValue = smartAttrThr.Key.RawValue;
                                smartSensor.Value = smartSensor.Attribute.ConvertValue(smartAttrThr.Key, smartSensor.Attribute.Parameter);
                            }
                        }
                    }
                }

                return _sensors.ToArray();
            }
        }

        public DriveHealth DriveHealth { get; }

        public double? Temperature { get; protected set; }

        public TimeSpan? PowerOnTime { get; protected set; }

        public ulong? PowerCycleCount { get; protected set; }

        /// <summary>
        /// Gets the SMART data.
        /// </summary>
        internal ISmart Smart { get; }

        /// <summary>
        /// Gets the SMART attributes.
        /// </summary>
        internal IReadOnlyList<SMARTAttribute> SmartAttributes { get; }

        internal ATADrive(StorageInfo storageInfo, ISmart smart, IReadOnlyList<SMARTAttribute> smartAttributes)
          : base(storageInfo)
        {
            IsATADrive = true;
            Smart = smart;
            if (smart.IsValid)
            {
                smart.EnableSmart();

                if (smart.GetType() == typeof(WindowsSMART))
                {
                    WindowsSMART winSMART = (WindowsSMART)smart;
                    DriveHealth = winSMART.ReadSmartHealth();
                    winSMART.ReadNameAndFirmwareRevision(out string name, out string firmRev);
                    Name = name;
                    FirmwareRevision = firmRev;
                }
            }
            
            SmartAttributes = smartAttributes;
            CreateSensors();
        }

        internal static Drive CreateInstance(StorageInfo storageInfo)
        {
            WindowsSMART smart = new(storageInfo.Index);
            string name = null;
            Kernel32.SMART_ATTRIBUTE[] smartAttributes = { };

            if (smart.IsValid)
            {
                bool nameValid = smart.ReadNameAndFirmwareRevision(out name, out _);
                bool smartEnabled = smart.EnableSmart();

                if (smartEnabled)
                    smartAttributes = smart.ReadSmartData();
                
                if (!nameValid)
                {
                    name = null;
                }
            }
            else
            {
                string[] logicalDrives = WindowsStorage.GetLogicalDrives(storageInfo.Index);
                if (logicalDrives == null || logicalDrives.Length == 0)
                {
                    smart.Close();
                    return null;
                }

                bool hasNonZeroSizeDrive = false;
                foreach (string logicalDrive in logicalDrives)
                {
                    try
                    {
                        var driveInfo = new DriveInfo(logicalDrive);
                        if (driveInfo.TotalSize > 0)
                        {
                            hasNonZeroSizeDrive = true;
                            break;
                        }
                    }
                    catch (ArgumentException) { }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }

                if (!hasNonZeroSizeDrive)
                {
                    smart.Close();
                    return null;
                }
            }

            foreach (Type type in _hddTypes)
            {
                // get the array of the required SMART attributes for the current type

                // check if all required attributes are present
                bool allAttributesFound = true;

                if (type.GetCustomAttributes(typeof(RequireSmartAttribute), true) is RequireSmartAttribute[] requiredAttributes)
                {
                    foreach (RequireSmartAttribute requireAttribute in requiredAttributes)
                    {
                        bool attributeFound = false;

                        foreach (Kernel32.SMART_ATTRIBUTE value in smartAttributes)
                        {
                            if (value.Id == requireAttribute.AttributeId)
                            {
                                attributeFound = true;
                                break;
                            }
                        }

                        if (!attributeFound)
                        {
                            allAttributesFound = false;
                            break;
                        }
                    }
                }

                // if an attribute is missing, then try the next type
                if (!allAttributesFound)
                    continue;


                // check if there is a matching name prefix for this type
                if (type.GetCustomAttributes(typeof(NamePrefixAttribute), true) is NamePrefixAttribute[] namePrefixes)
                {
                    foreach (NamePrefixAttribute prefix in namePrefixes)
                    {
                        if (name.StartsWith(prefix.Prefix, StringComparison.InvariantCulture))
                        {
                            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

                            return Activator.CreateInstance(type, flags, null, new object[] { storageInfo, smart }, null) as ATADrive;
                        }
                    }
                }
            }

            // no matching type has been found
            smart.Close();
            return null;
        }

        private void CreateSensors()
        {
        }

        internal virtual void UpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values) { }

        internal void DefaultUpdateAdditionalSensors(Kernel32.SMART_ATTRIBUTE[] values, byte? powOnHrsAttr, byte? powCycleCntAttr, byte? tempAttr)
        {
            foreach (Kernel32.SMART_ATTRIBUTE attr in values)
            {
                if (attr.Id == powOnHrsAttr)
                {
                    PowerOnTime = TimeSpan.FromHours(RawToInt(attr.RawValue, 0, null));
                }
                if (attr.Id == powCycleCntAttr)
                {
                    PowerCycleCount = (ulong)RawToInt(attr.RawValue, 0, null);
                }
                if (attr.Id == tempAttr)
                {
                    Temperature = attr.RawValue[0];
                }
            }
        }

        public override void Update()
        {
            base.Update();

            UpdateSensors();
        }

        protected override void UpdateSensors()
        {
            if (Smart.IsValid)
            {
                Kernel32.SMART_ATTRIBUTE[] smartAttributes = Smart.ReadSmartData();
                UpdateAdditionalSensors(smartAttributes);
            }
        }

        internal static double RawToInt(byte[] raw, byte value, double? parameter)
        {
            return (raw[3] << 24) | (raw[2] << 16) | (raw[1] << 8) | raw[0];
        }

        public override void Close()
        {
            Smart.Close();
            base.Close();
        }
    }
}
