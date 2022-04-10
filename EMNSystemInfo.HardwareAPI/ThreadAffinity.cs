// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;
using System;

namespace EMNSystemInfo.HardwareAPI
{
    internal static class ThreadAffinity
    {
        /// <summary>
        /// Initializes static members of the <see cref="ThreadAffinity" /> class.
        /// </summary>
        static ThreadAffinity()
        {
            ProcessorGroupCount = GetActiveProcessorGroupCount();

            if (ProcessorGroupCount < 1)
                ProcessorGroupCount = 1;
        }

        /// <summary>
        /// Gets the processor group count.
        /// </summary>
        public static int ProcessorGroupCount { get; }

        /// <summary>
        /// Returns true if the <paramref name="affinity"/> is valid.
        /// </summary>
        /// <param name="affinity">The affinity.</param>
        /// <returns><c>true</c> if the specified affinity is valid; otherwise, <c>false</c>.</returns>
        public static bool IsValid(GroupAffinity affinity)
        {
            try
            {
                GroupAffinity previousAffinity = Set(affinity);
                if (previousAffinity == GroupAffinity.Undefined)
                    return false;


                Set(previousAffinity);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Sets the processor group affinity for the current thread.
        /// </summary>
        /// <param name="affinity">The processor group affinity.</param>
        /// <returns>The previous processor group affinity.</returns>
        public static GroupAffinity Set(GroupAffinity affinity)
        {
            if (affinity == GroupAffinity.Undefined)
                return GroupAffinity.Undefined;

            UIntPtr uIntPtrMask;
            try
            {
                uIntPtrMask = (UIntPtr)affinity.Mask;
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(nameof(affinity));
            }

            var groupAffinity = new GROUP_AFFINITY { Group = affinity.Group, Mask = uIntPtrMask };

            IntPtr currentThread = GetCurrentThread();

            try
            {
                if (SetThreadGroupAffinity(currentThread,
                                           ref groupAffinity,
                                           out GROUP_AFFINITY previousGroupAffinity))
                {
                    return new GroupAffinity(previousGroupAffinity.Group, (ulong)previousGroupAffinity.Mask);
                }

                return GroupAffinity.Undefined;
            }
            catch (EntryPointNotFoundException)
            {
                if (affinity.Group > 0)
                    throw new ArgumentOutOfRangeException(nameof(affinity));


                ulong previous = (ulong)SetThreadAffinityMask(currentThread, uIntPtrMask);

                return new GroupAffinity(0, previous);
            }
        }
    }
}
