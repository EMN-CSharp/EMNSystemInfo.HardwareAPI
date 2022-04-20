// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI.LPC.EC;
using System;
using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.LPC
{
    /// <summary>
    /// LPC chip type
    /// </summary>
    public enum LPCType
    {
        /// <summary>
        /// Embedded Controller. Convert your <see cref="LPC"/> instance into <see cref="EC.EmbeddedController"/>.
        /// </summary>
        EmbeddedController,

        /// <summary>
        /// Super I/O. Convert your <see cref="LPC"/> instance into <see cref="SuperIOHardware"/>.
        /// </summary>
        SuperIO
    }

    /// <summary>
    /// Abstract class that represents a base LPC chip
    /// </summary>
    public abstract class LPC
    {
        /// <summary>
        /// Gets the chip name
        /// </summary>
        public string ChipName { get; internal set; }

        /// <summary>
        /// Gets the LPC chip type
        /// </summary>
        public LPCType Type { get; internal set; }

        /// <summary>
        /// Frees the resources used in this class. It is not necessary to call this method, <see cref="LPCChips.DisposeLPCChips"/> does all the work.
        /// </summary>
        public virtual void Close() { }
    }

    /// <summary>
    /// LPC chips information. It is required to initialize the library to use this class.
    /// </summary>
    public static class LPCChips
    {
        /// <summary>
        /// Gets a value that represents if the LPC chips are loaded on the <see cref="List"/> property. Returns <see langword="true"/> if LPCs are loaded, otherwise, <see langword="false"/>.
        /// </summary>
        public static bool LPCsAreLoaded { get; private set; } = false;

        /// <summary>
        /// LPC chips list
        /// </summary>
        public static LPC[] List { get; internal set; } = Array.Empty<LPC>();

        /// <summary>
        /// Loads all the motherboard LPC chips into the <see cref="List"/> property.
        /// </summary>
        /// <returns><see langword="false"/> if the library is not initialized, the user is not an administrator, or LPC chips were loaded before. Otherwise, <see langword="true"/>.</returns>
        public static bool LoadLPCChips()
        {
            if (!LibrarySettings.IsInitialized || !LibrarySettings.UserIsAdmin || LPCsAreLoaded)
            {
                return false;
            }

            IReadOnlyList<ISuperIO> superIO;
            MotherboardManufacturer manufacturer = SMBIOS.Board == null ? MotherboardManufacturer.Unknown : Identification.GetManufacturer(SMBIOS.Board.ManufacturerName);
            MotherboardModel model = SMBIOS.Board == null ? MotherboardModel.Unknown : Identification.GetModel(SMBIOS.Board.ProductName);

            LPCIO lpcIO = new LPCIO();
            superIO = lpcIO.SuperIO;

            EmbeddedController ec = EmbeddedController.Create(model);

            List = new LPC[superIO.Count + (ec != null ? 1 : 0)];
            for (int i = 0; i < superIO.Count; i++)
                List[i] = new SuperIOHardware(superIO[i], manufacturer, model);

            if (ec != null)
                List[superIO.Count] = ec;

            LPCsAreLoaded = true;
            return true;
        }

        /// <summary>
        /// Frees the resources used by <see cref="LPC"/> instances.
        /// </summary>
        public static void DisposeLPCChips()
        {
            foreach (LPC lpc in List)
            {
                lpc.Close();
            }
            LPCsAreLoaded = false;
            List = Array.Empty<LPC>();
        }
    }
}
