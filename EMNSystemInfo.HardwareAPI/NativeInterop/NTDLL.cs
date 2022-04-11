// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System;
using System.Runtime.InteropServices;

namespace EMNSystemInfo.HardwareAPI.NativeInterop
{
    internal static class NTDLL
    {
        private const string LibName = nameof(NTDLL);

        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION
        {
            public long IdleTime;
            public long KernelTime;
            public long UserTime;
            public long Reserved0;
            public long Reserved1;
            public ulong Reserved2;
        }

        public enum SYSTEM_INFORMATION_CLASS
        {
            SystemBasicInformation,
            SystemProcessorInformation,
            SystemPerformanceInformation,
            SystemTimeOfDayInformation,
            SystemPathInformation,
            SystemProcessInformation,
            SystemCallCountInformation,
            SystemDeviceInformation,
            SystemProcessorPerformanceInformation,
            SystemFlagsInformation,
            SystemCallTimeInformation,
            SystemModuleInformation,
            SystemLocksInformation,
            SystemStackTraceInformation,
            SystemPagedPoolInformation,
            SystemNonPagedPoolInformation,
            SystemHandleInformation,
            SystemObjectInformation,
            SystemPageFileInformation,
            SystemVdmInstemulInformation,
            SystemVdmBopInformation,
            SystemFileCacheInformation,
            SystemPoolTagInformation,
            SystemInterruptInformation,
            SystemDpcBehaviorInformation,
            SystemFullMemoryInformation,
            SystemLoadGdiDriverInformation,
            SystemUnloadGdiDriverInformation,
            SystemTimeAdjustmentInformation,
            SystemSummaryMemoryInformation,
            SystemNextEventIdInformation,
            SystemEventIdsInformation,
            SystemCrashDumpInformation,
            SystemExceptionInformation,
            SystemCrashDumpStateInformation,
            SystemKernelDebuggerInformation,
            SystemContextSwitchInformation,
            SystemRegistryQuotaInformation,
            SystemExtendServiceTableInformation,
            SystemPrioritySeperation,
            SystemPlugPlayBusInformation,
            SystemDockInformation,
            SystemPowerInformation,
            SystemProcessorSpeedInformation,
            SystemCurrentTimeZoneInformation,
            SystemLookasideInformation
        }

        [DllImport(LibName)]
        public static extern int NtQuerySystemInformation(SYSTEM_INFORMATION_CLASS SystemInformationClass, [Out] SYSTEM_PROCESSOR_PERFORMANCE_INFORMATION[] SystemInformation, int SystemInformationLength, out IntPtr ReturnLength);
    }
}
