// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using static EMNSystemInfo.HardwareAPI.NativeInterop.Kernel32;
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace EMNSystemInfo.HardwareAPI
{
    internal static class FirmwareTable
    {
        public static byte[] GetTable(FirmwareProvider provider, string table)
        {
            int id = table[3] << 24 | table[2] << 16 | table[1] << 8 | table[0];
            return GetTable(provider, id);
        }

        public static byte[] GetTable(FirmwareProvider provider, int table)
        {
            int size;

            try
            {
                size = GetSystemFirmwareTable(provider, table, IntPtr.Zero, 0);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                return null;
            }

            if (size <= 0)
                return null;


            IntPtr allocatedBuffer = IntPtr.Zero;

            try
            {
                allocatedBuffer = Marshal.AllocHGlobal(size);

                GetSystemFirmwareTable(provider, table, allocatedBuffer, size);
                if (Marshal.GetLastWin32Error() != 0)
                    return null;


                byte[] buffer = new byte[size];
                Marshal.Copy(allocatedBuffer, buffer, 0, size);
                return buffer;
            }
            finally
            {
                if (allocatedBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(allocatedBuffer);
            }
        }

        public static string[] EnumerateTables(FirmwareProvider provider)
        {
            int size;

            try
            {
                size = EnumSystemFirmwareTables(provider, IntPtr.Zero, 0);
            }
            catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
            {
                return null;
            }

            IntPtr allocatedBuffer = IntPtr.Zero;

            try
            {
                allocatedBuffer = Marshal.AllocHGlobal(size);

                EnumSystemFirmwareTables(provider, allocatedBuffer, size);

                byte[] buffer = new byte[size];
                Marshal.Copy(allocatedBuffer, buffer, 0, size);

                string[] result = new string[size / 4];

                for (int i = 0; i < result.Length; i++)
                    result[i] = Encoding.ASCII.GetString(buffer, 4 * i, 4);

                return result;
            }
            finally
            {
                if (allocatedBuffer != IntPtr.Zero)
                    Marshal.FreeHGlobal(allocatedBuffer);
            }
        }
    }
}
