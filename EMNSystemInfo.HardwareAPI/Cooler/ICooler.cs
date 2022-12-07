// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI.Cooler
{
    public enum CoolerType
    {
        Unknown,
        AeroCoolP7H1,
        AquaComputerAquastreamXT,
        AquaComputerD5Next,
        AquaComputerMPS,
        Heatmaster,
        NZXTKrakenX3,
        TBalancer,
        AquaComputerOcto
    }

    public interface ICooler
    {
        string Name { get; }

        bool IsWaterCoolingSystem { get; }

        CoolerType Type { get; }

        void Close();

        void Update();
    }
}
