// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.Battery;
using EMNSystemInfo.HardwareAPI.Cooler;
using EMNSystemInfo.HardwareAPI.CPU;
using EMNSystemInfo.HardwareAPI.GPU;
using EMNSystemInfo.HardwareAPI.LPC;
using EMNSystemInfo.HardwareAPI.PhysicalStorage;
using System;
using System.Security.Principal;

namespace EMNSystemInfo.HardwareAPI
{
    /// <summary>
    /// Class that provides library settings.
    /// </summary>
    public static class LibrarySettings
    {
        private static string _krnldrvName;

        /// <summary>
        /// Gets the value that represents if the library is initialized.
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Gets the value that represents if the current user is an administrator.
        /// </summary>
        public static bool UserIsAdmin
        {
            get
            {
                WindowsPrincipal adminCheck = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return adminCheck.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Gets or sets the kernel driver name. When necessary, the set value will be validated.
        /// </summary>
        public static string KernelDriverName
        {
            get => _krnldrvName;
            set
            {
                _krnldrvName = value.Replace(" ", string.Empty).Replace(".", "_");
            }
        }

        /// <summary>
        /// Gets or sets the kernel driver display name.
        /// </summary>
        public static string KernelDriverDisplayName { get; set; }

        /// <summary>
        /// Initializes the library. If the current user is an administrator, installs the kernel driver used to get hardware info, with <see cref="KernelDriverName"/> and <see cref="KernelDriverDisplayName"/> as the driver name and display name respectively.
        /// </summary>
        /// <exception cref="FormatException"/>
        public static void Initialize()
        {
            if (!IsInitialized)
            {
                OpCode.Open();
                
                if (UserIsAdmin)
                {
                    if (string.IsNullOrWhiteSpace(KernelDriverName))
                    {
                        throw new FormatException($"{nameof(KernelDriverName)} cannot be null, empty, or just white spaces");
                    }
                    else
                    {
                        Ring0.Open();
                    }
                }

                IsInitialized = true;
            }
        }

        /// <summary>
        /// Gets the kernel driver report. Used for debug purposes.
        /// </summary>
        /// <returns>A string that represents the kernel driver report</returns>
        public static string GetKernelDriverReport()
        {
            return Ring0.GetReport();
        }

        /// <summary>
        /// Frees the resources used in the library. Uninstalls the kernel driver.
        /// </summary>
        public static void Close()
        {
            if (IsInitialized)
            {
                Batteries.DisposeAllBatteries();
                Coolers.DisposeCoolers();
                Processors.DisposeAllProcessors();
                GPUs.DisposeGPUs();
                LPCChips.DisposeLPCChips();
                StorageDrives.DisposeDrives();

                if (Ring0.IsOpen)
                    Ring0.Close();

                OpCode.Close();

                IsInitialized = false;
            }
        }
    }
}
