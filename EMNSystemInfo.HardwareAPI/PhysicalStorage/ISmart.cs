// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using EMNSystemInfo.HardwareAPI.NativeInterop;

namespace EMNSystemInfo.HardwareAPI.PhysicalStorage
{
    internal interface ISmart : IDisposable
    {
        bool IsValid { get; }

        void Close();

        bool EnableSmart();

        Kernel32.SMART_ATTRIBUTE[] ReadSmartData();

        Kernel32.SMART_THRESHOLD[] ReadSmartThresholds();

        bool ReadNameAndFirmwareRevision(out string name, out string firmwareRevision);
    }
}
