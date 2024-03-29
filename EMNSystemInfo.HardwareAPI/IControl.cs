﻿// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI
{
    public enum ControlMode
    {
        Undefined,
        Software,
        Default
    }

    public interface IControl
    {
        ControlMode ControlMode { get; }

        double MaxSoftwareValue { get; }

        double MinSoftwareValue { get; }

        IControlSensor Sensor { get; }

        double SoftwareValue { get; }

        void SetDefault();

        void SetSoftware(double value);
    }
}
