// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming

using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI.NativeInterop
{
    internal static class GDI32
    {
        internal const string DllName = nameof(GDI32);
        
        [DllImport(DllName, ExactSpelling = true)]
        internal static extern uint D3DKMTCloseAdapter(ref D3DKMTH.D3DKMT_CLOSEADAPTER closeAdapter);

        [DllImport(DllName, ExactSpelling = true)]
        internal static extern uint D3DKMTOpenAdapterFromDeviceName(ref D3DKMTH.D3DKMT_OPENADAPTERFROMDEVICENAME openAdapterFromDeviceName);

        [DllImport(DllName, ExactSpelling = true)]
        internal static extern uint D3DKMTQueryAdapterInfo(ref D3DKMTH.D3DKMT_QUERYADAPTERINFO queryAdapterInfo);

        [DllImport(DllName, ExactSpelling = true)]
        internal static extern uint D3DKMTQueryStatistics(ref D3DKMTH.D3DKMT_QUERYSTATISTICS queryStatistics);
    }
}
