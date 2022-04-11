// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI.NativeInterop
{
    internal static class InpOut
    {
        public delegate IntPtr MapPhysToLinDelegate(IntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pPhysicalMemoryHandle);

        public delegate bool UnmapPhysicalMemoryDelegate(IntPtr PhysicalMemoryHandle, IntPtr pbLinAddr);

        [DllImport("inpout.dll", EntryPoint = "MapPhysToLin", CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr MapPhysToLin(IntPtr pbPhysAddr, uint dwPhysSize, out IntPtr pPhysicalMemoryHandle);

        [DllImport("inpout.dll", EntryPoint = "UnmapPhysicalMemory", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnmapPhysicalMemory(IntPtr PhysicalMemoryHandle, IntPtr pbLinAddr);
    }
}
