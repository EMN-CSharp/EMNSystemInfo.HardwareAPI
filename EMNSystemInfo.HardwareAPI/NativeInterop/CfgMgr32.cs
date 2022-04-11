// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI.NativeInterop
{
    internal static class CfgMgr32
    {

        internal const uint CM_GET_DEVICE_INTERFACE_LIST_PRESENT = 0;
        internal const int CR_SUCCESS = 0;
        internal static Guid GUID_DISPLAY_DEVICE_ARRIVAL = new("1CA05180-A699-450A-9A0C-DE4FBE3DDD89");
        
        internal const string DllName = nameof(CfgMgr32);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern uint CM_Get_Device_Interface_List_Size(out uint size, ref Guid interfaceClassGuid, string deviceID, uint flags);

        [DllImport(DllName, CharSet = CharSet.Unicode)]
        internal static extern uint CM_Get_Device_Interface_List(ref Guid interfaceClassGuid, string deviceID, char[] buffer, uint bufferLength, uint flags);
    }
}
