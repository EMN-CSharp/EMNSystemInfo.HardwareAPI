// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

namespace EMNSystemInfo.HardwareAPI
{
    public class ControlSensor : IControlSensor
    {
        public Control Control { get; internal set; }

        public double? Value { get; internal set; }
    }
}
