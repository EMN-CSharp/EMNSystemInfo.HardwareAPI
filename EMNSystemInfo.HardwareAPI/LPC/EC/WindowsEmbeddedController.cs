// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// Partial Copyright (C) LibreHardwareMonitor and Contributors.
// All Rights Reserved.

using System.Collections.Generic;

namespace EMNSystemInfo.HardwareAPI.LPC.EC
{
    internal class WindowsEmbeddedController : EmbeddedController
    {
        public WindowsEmbeddedController(IEnumerable<(ECSensorType SensorType, EmbeddedControllerSource Source)> boardSensors) : base(boardSensors)
        { }

        internal override IEmbeddedControllerIO AcquireIOInterface()
        {
            return new WindowsEmbeddedControllerIO();
        }
    }
}
