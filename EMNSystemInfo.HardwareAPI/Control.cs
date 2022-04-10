// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI
{
    public delegate void ControlEventHandler(Control control);

    public class Control : IControl
    {
        private ControlMode _mode;
        private double _softwareValue;

        public Control
        (
            IControlSensor sensor,
            double minSoftwareValue,
            double maxSoftwareValue)
        {
            Sensor = sensor;
            MinSoftwareValue = minSoftwareValue;
            MaxSoftwareValue = maxSoftwareValue;
        }

        internal event ControlEventHandler ControlModeChanged;

        internal event ControlEventHandler SoftwareControlValueChanged;

        public ControlMode ControlMode
        {
            get { return _mode; }
            private set
            {
                if (_mode != value)
                {
                    _mode = value;
                    ControlModeChanged?.Invoke(this);
                }
            }
        }

        public double MaxSoftwareValue { get; }

        public double MinSoftwareValue { get; }

        public IControlSensor Sensor { get; }

        public double SoftwareValue
        {
            get { return _softwareValue; }
            private set
            {
                if (_softwareValue != value)
                {
                    _softwareValue = value;
                    SoftwareControlValueChanged?.Invoke(this);
                }
            }
        }

        public void SetDefault()
        {
            ControlMode = ControlMode.Default;
        }

        public void SetSoftware(double value)
        {
            ControlMode = ControlMode.Software;
            SoftwareValue = value;
        }

        double IControl.SetDefault()
        {
            throw new System.NotImplementedException();
        }

        double IControl.SetSoftware(double value)
        {
            throw new System.NotImplementedException();
        }
    }
}
