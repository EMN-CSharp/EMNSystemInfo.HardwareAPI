// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) EMN-CSharp and Contributors.
// All Rights Reserved.

using EMNSystemInfo.HardwareAPI;
using EMNSystemInfo.HardwareAPI.Battery;
using EMNSystemInfo.HardwareAPI.CPU;
using EMNSystemInfo.HardwareAPI.GPU;
using EMNSystemInfo.HardwareAPI.LPC;
using EMNSystemInfo.HardwareAPI.LPC.EC;
using EMNSystemInfo.HardwareAPI.PhysicalStorage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EMNSystemInfo.HardwareAPITest
{
    class Program
    {
        static string ConvertBytesToHexString(byte[] buffer)
        {
            string[] hexStrArr = new string[buffer.Length];

            for (long i = 0; i < buffer.LongLength; i++)
            {
                hexStrArr[i] = buffer[i].ToString("X2");
            }

            return string.Join("", hexStrArr.Reverse());
        }

        static string GetNodeName(NodeEngineType nodeEngineType, string nodeEngTypeStrWhenOther)
        {
            string nodeName = "<Unknown>";
            switch (nodeEngineType)
            {
                case NodeEngineType.Other:
                    if (string.IsNullOrEmpty(nodeEngTypeStrWhenOther))
                    {
                        nodeName = "Other";
                    }
                    else
                    {
                        nodeName = nodeEngTypeStrWhenOther;
                    }
                    break;
                case NodeEngineType._3D:
                    nodeName = "3D";
                    break;
                case NodeEngineType.VideoDecode:
                    nodeName = "Video Decode";
                    break;
                case NodeEngineType.VideoEncode:
                    nodeName = "Video Encode";
                    break;
                case NodeEngineType.VideoProcessing:
                    nodeName = "Video Processing";
                    break;
                case NodeEngineType.SceneAssembly:
                    nodeName = "Scene Assembly";
                    break;
                case NodeEngineType.Copy:
                    nodeName = "Copy";
                    break;
                case NodeEngineType.Overlay:
                    nodeName = "Overlay";
                    break;
                case NodeEngineType.Crypto:
                    nodeName = "Cryptography";
                    break;
            }

            return nodeName;
        }

        static void Main()
        {
            LibrarySettings.KernelDriverName = "EMNSystemInfo.HardwareAPITest";
            LibrarySettings.KernelDriverDisplayName = "EMNSystemInfo.HardwareAPITest Kernel Driver";
            try
            {
                LibrarySettings.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.WriteLine(LibrarySettings.GetRing0Report());
            }

            StringBuilder output = new();

            #region Batteries Information

            uint count = 1;
            Batteries.LoadInstalledBatteries();
            foreach (Battery bat in Batteries.List)
            {
                bat.Update();
                string chemistry = "<Unknown>";
                switch (bat.Chemistry)
                {
                    case BatteryChemistry.LeadAcid:
                        chemistry = "Lead-Acid";
                        break;
                    case BatteryChemistry.NickelCadmium:
                        chemistry = "Nickel-Cadmium";
                        break;
                    case BatteryChemistry.NickelMetalHydride:
                        chemistry = "Nickel-Metal Hydride";
                        break;
                    case BatteryChemistry.LithiumIon:
                        chemistry = "Lithium Ion";
                        break;
                    case BatteryChemistry.NickelZinc:
                        chemistry = "Nickel-Zinc";
                        break;
                    case BatteryChemistry.AlkalineManganese:
                        chemistry = "Rechargeable Alkaline-Manganese";
                        break;
                }

                string powerState = "<Unknown>";
                switch (bat.PowerState)
                {
                    case BatteryPowerState.Charging:
                        powerState = "Charging";
                        break;
                    case BatteryPowerState.Critical:
                        powerState = "Critical";
                        break;
                    case BatteryPowerState.Discharging:
                        powerState = "Discharging";
                        break;
                    case BatteryPowerState.OnAC:
                        powerState = "On AC Power";
                        break;
                }

                output.Append("Battery #").Append(count).AppendLine(":")
                      .Append(" · Name: ").AppendLine(bat.Name)
                      .Append(" · Manufacturer: ").AppendLine(bat.Manufacturer)
                      .Append(" · Status: ").AppendLine(powerState)
                      .Append(" · Chemistry: ").AppendLine(chemistry)
                      .Append(" · Degradation Level: ").AppendFormat("{0:F2}", bat.DegradationLevel).AppendLine(" %")
                      .Append(" · Designed Capacity: ").Append(bat.DesignedCapacity).AppendLine(" mWh")
                      .Append(" · Full Charged Capacity: ").Append(bat.FullChargedCapacity).AppendLine(" mWh");
                if (bat.RemainingCapacity.HasValue)
                {
                    output.Append(" · Remaining Capacity: ").Append(bat.RemainingCapacity.Value).AppendLine(" mWh");
                }
                if (bat.ChargeLevel.HasValue)
                {
                    output.Append(" · Charge Level: ").AppendFormat("{0:F2}", bat.ChargeLevel.Value).AppendLine(" %");
                }
                if (bat.Voltage.HasValue)
                { 
                    output.Append(" · Voltage: ").AppendFormat("{0:F3}", bat.Voltage.Value).AppendLine(" V");
                }
                if (bat.EstimatedRemainingTime.HasValue)
                {
                    output.Append(" · Remaining Time (Estimated): ").AppendFormat("{0:g}", bat.EstimatedRemainingTime.Value).AppendLine();
                }
                if (bat.ChargeDischargeRate.HasValue)
                {
                    switch (bat.ChargeDischargeRate.Value)
                    {
                        case > 0:
                            output.Append(" · Charge Rate: ").AppendFormat("{0:F1}", bat.ChargeDischargeRate).AppendLine(" W")
                                  .Append(" · Charge Current: ").AppendFormat("{0:F3}", bat.ChargeDischargeRate / bat.Voltage).AppendLine(" A");

                            break;
                        case < 0:
                            output.Append(" · Discharge Rate: ").AppendFormat("{0:F1}", Math.Abs(bat.ChargeDischargeRate.Value)).AppendLine(" W")
                                  .Append(" · Discharge Current: ").AppendFormat("{0:F3}", Math.Abs(bat.ChargeDischargeRate.Value) / bat.Voltage).AppendLine(" A");

                            break;
                        default:
                            output.AppendLine(" · Charge/Discharge Rate: 0 W")
                                  .AppendLine(" · Charge/Discharge Current: 0 A");

                            break;
                    }
                }
                count++;
            }
            Batteries.DisposeAllBatteries();

            #endregion

            output.AppendLine();

            #region Processors Information

            Processor[] processors = Processors.List;
            foreach (Processor p in processors)
            {
                output.AppendFormat("Processor #{0}:", p.Index + 1).AppendLine();
                output.AppendFormat(" · Name: {0}", p.BrandString).AppendLine();
                output.AppendFormat(" · Short Name: {0}", p.Name).AppendLine();
                output.AppendFormat(" · Manufacturer: {0}", p.Vendor).AppendLine();
                output.AppendFormat(" · Load: {0:F2} %", p.TotalLoad).AppendLine();

                output.Append(" · Thread Loads:").AppendLine();
                foreach (ThreadLoad threadLoad in p.ThreadLoads)
                {
                    if (threadLoad.Thread != null)
                    {
                        output.AppendFormat("   · Core #{0}, Thread #{1}: {2:F2} %", threadLoad.Core + 1, threadLoad.Thread + 1, threadLoad.Value).AppendLine();
                    }
                    else
                    {
                        output.AppendFormat("   · Core #{0}: {1:F2} %", threadLoad.Core + 1, threadLoad.Value).AppendLine();
                    }
                }

                output.AppendFormat(" · ¿Has TSC?: {0}", p.HasTimeStampCounter).AppendLine();
                if (p.HasTimeStampCounter)
                {
                    output.AppendFormat(" · TSC Frequency: {0:F2} MHz", p.TimeStampCounterFrequency).AppendLine();
                }
                output.AppendFormat(" · ¿Has MSRs?: {0}", p.HasModelSpecificRegisters).AppendLine();

                #region Intel CPU

                if (p.Type == ProcessorType.IntelCPU)
                {
                    IntelCPU intelCPU = (IntelCPU)p;
                    intelCPU.Update();
                    output.Append(" · Intel-Specific Properties:").AppendLine();
                    output.AppendFormat("   · Microarchitecture: {0}", intelCPU.Microarchitecture).AppendLine();
                    output.AppendFormat("   · Bus Clock Speed: {0:F2} MHz", intelCPU.BusClock).AppendLine();
                    output.AppendFormat("   · Core Voltage (VID): {0:F3} V", intelCPU.CoreVoltage).AppendLine();

                    output.Append("   · Core Clock Speeds:").AppendLine();
                    count = 1;
                    foreach (double clock in intelCPU.CoreFrequencyClocks)
                    {
                        output.AppendFormat("     · Core #{0}: {1:F2} MHz (multiplier: × {2:F2})", count, clock, clock / intelCPU.BusClock).AppendLine();
                        count++;
                    }

                    if (intelCPU.PackageTemperature.HasValue)
                    {
                        output.AppendFormat("   · Package Temperature: {0:F2} °C", intelCPU.PackageTemperature.Value.Value).AppendLine();
                        output.AppendFormat("   · Maximum temperature supported by the package (TjMax value): {0:F2} °C", intelCPU.PackageTemperature.Value.TjMax).AppendLine();
                    }
                    output.AppendFormat("   · Average Core Temperature: {0:F2} °C", intelCPU.CoreAverageTemperature).AppendLine();

                    output.Append("   · Core Temperatures:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in intelCPU.CoreTemperatures)
                    {
                        output.AppendFormat("     · Core #{0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Power Sensors:").AppendLine();
                    count = 1;
                    foreach (IntelPowerSensor? ps in intelCPU.PowerSensors)
                    {
                        output.AppendFormat("     · {0}: {1:F2} W", ps?.Type, ps?.Value).AppendLine();
                        count++;
                    }
                }

                #endregion

                #region AMD 0F CPU

                else if (p.Type == ProcessorType.AMD0FCPU)
                {
                    AMD0FCPU amd0FCPU = (AMD0FCPU)p;
                    amd0FCPU.Update();
                    output.Append(" · AMD-Specific Properties:").AppendLine();
                    output.AppendFormat("   · Bus Clock: {0:F2} MHz", amd0FCPU.BusClock).AppendLine();

                    output.Append("   · Core Clock Speeds:").AppendLine();
                    count = 1;
                    foreach (double clock in amd0FCPU.CoreClockSpeed)
                    {
                        output.AppendFormat("     · Core #{0}: {1:F2} MHz (multiplier: × {2:F2})", count, clock, clock / amd0FCPU.BusClock).AppendLine();
                        count++;
                    }

                    output.Append("   · Core Temperatures:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in amd0FCPU.CoreTemperatures)
                    {
                        output.AppendFormat("     · Core #{0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }
                }

                #endregion

                #region AMD 10 CPU

                else if (p.Type == ProcessorType.AMD10CPU)
                {
                    AMD10CPU amd10CPU = (AMD10CPU)p;
                    amd10CPU.Update();
                    output.Append(" · AMD-Specific Properties:").AppendLine();
                    output.AppendFormat("   · Bus Clock Speed: {0:F2} MHz", amd10CPU.BusClock).AppendLine();
                    output.AppendFormat("   · Core Voltage (VID): {0:F3} V", amd10CPU.CoreVoltage).AppendLine();
                    output.AppendFormat("   · Northbridge Voltage: {0:F3} V", amd10CPU.NorthbridgeVoltage).AppendLine();
                    output.AppendFormat("   · C2 State Residency Level: {0:F3} %", amd10CPU.C2StateResidencyLevel).AppendLine();
                    output.AppendFormat("   · C3 State Residency Level: {0:F3} %", amd10CPU.C3StateResidencyLevel).AppendLine();

                    output.Append("   · Core Clock Speeds:").AppendLine();
                    count = 1;
                    foreach (double clock in amd10CPU.CoreClockSpeeds)
                    {
                        output.AppendFormat("     · Core #{0}: {1:F2} MHz (multiplier: × {2:F2})", count, clock, clock / amd10CPU.BusClock).AppendLine();
                        count++;
                    }

                    if (amd10CPU.CoreTemperature.HasValue)
                    {
                        output.AppendFormat("   · Core Temperature: {1:F2} °C", count, amd10CPU.CoreTemperature.Value.Value).AppendLine();
                    }
                }

                #endregion

                #region AMD 17 CPU

                else if (p.Type == ProcessorType.AMD17CPU)
                {
                    AMD17CPU amd17CPU = (AMD17CPU)p;
                    amd17CPU.Update();
                    output.Append(" · AMD-Specific Properties:").AppendLine();
                    output.AppendFormat("   · Code Name: {0}", amd17CPU.CodeName).AppendLine();
                    output.AppendFormat("   · Bus Clock Speed: {0:F2} MHz", amd17CPU.BusClock).AppendLine();
                    output.AppendFormat("   · Core Voltage (VID): {0:F3} V", amd17CPU.CoreVoltage).AppendLine();
                    output.AppendFormat("   · Package Power: {0:F3} V", amd17CPU.PackagePower).AppendLine();

                    if (amd17CPU.SoCVoltage.HasValue)
                    {
                        output.AppendFormat("   · SoC Voltage: {0:F3} V", amd17CPU.SoCVoltage.Value).AppendLine();
                    }

                    output.AppendFormat("   · Core Temperature (Tctl): {1:F2} °C", count, amd17CPU.CoreTemperatureTctl).AppendLine();
                    output.AppendFormat("   · Core Temperature (Tctl/Tdie): {1:F2} °C", count, amd17CPU.CoreTemperatureTctlTdie).AppendLine();
                    output.AppendFormat("   · Core Temperature (Tdie): {1:F2} °C", count, amd17CPU.CoreTemperatureTctlTdie).AppendLine();

                    output.Append("   · CCD Temperatures:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in amd17CPU.CCDTemperatures)
                    {
                        output.AppendFormat("     · CCD {0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · SMU Sensors:").AppendLine();
                    foreach (var smuSensor in amd17CPU.SMUSensors)
                    {
                        switch (smuSensor.Key.Type)
                        {
                            case SMUSensorType.Voltage:
                                output.AppendFormat("     · (Voltage) {0}: {1:F2} V", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Current:
                                output.AppendFormat("     · (Current) {0}: {1:F2} A", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Power:
                                output.AppendFormat("     · (Power) {0}: {1:F2} W", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Clock:
                                output.AppendFormat("     · (Clock Speed) {0}: {1:F2} MHz", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Temperature:
                                output.AppendFormat("     · (Temperature) {0}: {1:F2} °C", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Load:
                                output.AppendFormat("     · (Load) {0}: {1:F2} %", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Factor:
                                output.AppendFormat("     · (Factor) {0}: {1:F2}", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                        }
                    }
                }

                #endregion
            }

            #endregion

            output.AppendLine();

            #region GPUs Information

            GPUs.LoadGPUs();
            IList<GPU> gpus = GPUs.List;
            count = 1;
            foreach (GPU gpu in gpus)
            {
                gpu.Update();
                output.AppendFormat("GPU #{0}:", count).AppendLine();
                output.AppendFormat(" · Name: {0}", gpu.Name).AppendLine();
                output.AppendFormat(" · Dedicated Memory Usage: {0}", ByteConversions.ConvertBytesToString(gpu.DedicatedMemoryUsage, Unit.BinaryByte)).AppendLine();
                output.AppendFormat(" · Shared Memory Usage: {0}", ByteConversions.ConvertBytesToString(gpu.SharedMemoryUsage, Unit.BinaryByte)).AppendLine();
                output.Append(" · Node Usages:").AppendLine();
                foreach (var nodeGroup in from nd in gpu.NodeUsage
                                          orderby GetNodeName(nd.NodeEngineType, nd.NodeEngineTypeString) ascending
                                          group nd by GetNodeName(nd.NodeEngineType, nd.NodeEngineTypeString))
                {
                    int nodeCount = 0;
                    foreach (NodeUsageSensor node in nodeGroup)
                    {
                        string nodeName = GetNodeName(node.NodeEngineType, node.NodeEngineTypeString);

                        if (nodeGroup.Count() > 1)
                        {
                            nodeCount++;
                            output.AppendFormat(@"   · {0} #{1}: {2:F2} % (running time: {3:hh\:mm\:ss\.fff})", nodeName, nodeCount, node.Value, node.RunningTime).AppendLine();
                        }
                        else
                        {
                            output.AppendFormat(@"   · {0}: {1:F2} % (running time: {2:hh\:mm\:ss\.fff})", nodeName, node.Value, node.RunningTime).AppendLine();
                        }
                    }
                }

                #region Intel integrated GPU

                if (gpu.Type == GPUType.IntelIntegratedGPU)
                {
                    IntelIntegratedGPU iiGPU = (IntelIntegratedGPU)gpu;
                    iiGPU.Update();
                    output.Append(" · Intel Integrated GPU Specific Properties:").AppendLine();

                    if (iiGPU.TotalPower.HasValue)
                    {
                        output.AppendFormat("   · Total Power: {0:F2} W", iiGPU.TotalPower.Value).AppendLine();
                    }
                }

                #endregion

                #region NVIDIA GPU

                else if (gpu.Type == GPUType.NvidiaGPU)
                {
                    NvidiaGPU nvGPU = (NvidiaGPU)gpu;
                    nvGPU.Update();
                    output.Append(" · NVIDIA GPU Specific Properties:").AppendLine();

                    output.Append("   · Load Sensors:").AppendLine();
                    foreach (NvidiaLoadSensor load in nvGPU.Loads)
                    {
                        string type = "<Unknown>";
                        switch (load.Type)
                        {
                            case NvidiaLoadType.Gpu:
                                type = "GPU Load";
                                break;
                            case NvidiaLoadType.FrameBuffer:
                                type = "Framebuffer Load";
                                break;
                            case NvidiaLoadType.VideoEngine:
                                type = "Video Engine Load";
                                break;
                            case NvidiaLoadType.BusInterface:
                                type = "Bus Interface Load";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} %", type, load.Value).AppendLine();
                        count++;
                    }

                    output.AppendFormat(" · HotSpot Temperature: {0} °C", nvGPU.HotSpotTemperature).AppendLine();
                    output.AppendFormat(" · Memory Junction Temperature: {0} °C", nvGPU.MemoryJunctionTemperature).AppendLine();

                    output.Append("   · Temperature Sensors:").AppendLine();
                    foreach (NvidiaTempSensor temp in nvGPU.Temperatures)
                    {
                        string type = "<Unknown>";
                        switch (temp.Type)
                        {
                            case NvidiaTempSensorType.Gpu:
                                type = "GPU";
                                break;
                            case NvidiaTempSensorType.Memory:
                                type = "Memory";
                                break;
                            case NvidiaTempSensorType.PowerSupply:
                                type = "Power Supply";
                                break;
                            case NvidiaTempSensorType.Board:
                                type = "PCB";
                                break;
                            case NvidiaTempSensorType.VisualComputingBoard:
                                type = "Visual Computing Board";
                                break;
                            case NvidiaTempSensorType.VisualComputingInlet:
                                type = "Visua Computing Inlet";
                                break;
                            case NvidiaTempSensorType.VisualComputingOutlet:
                                type = "Visual Computing Outlet";
                                break;
                            case NvidiaTempSensorType.All:
                                type = "All";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} °C", type, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Clock Speeds:").AppendLine();
                    foreach (NvidiaClockSensor clock in nvGPU.ClockSpeeds)
                    {
                        string type = "<Desconocido>";
                        switch (clock.Type)
                        {
                            case NvidiaClockType.Graphics:
                                type = "Graphics";
                                break;
                            case NvidiaClockType.Memory:
                                type = "Memory";
                                break;
                            case NvidiaClockType.Processor:
                                type = "Processor";
                                break;
                            case NvidiaClockType.Video:
                                type = "Video";
                                break;
                            case NvidiaClockType.Undefined:
                                type = "<Undefined>";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} MHz", type, clock.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Power Sensors:").AppendLine();
                    foreach (NvidiaPowerSensor power in nvGPU.PowerSensors)
                    {
                        string type = "<Desconocido>";
                        switch (power.Type)
                        {
                            case NvidiaPowerType.Gpu:
                                type = "GPU Power";
                                break;
                            case NvidiaPowerType.Board:
                                type = "PCB";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} W", type, power.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Fan Speeds:").AppendLine();
                    count = 1;
                    foreach (double fanRPM in nvGPU.FanRPMs)
                    {
                        output.AppendFormat("     · Fan #{0}: {1:F2} RPM", count, fanRPM).AppendLine();
                        count++;
                    }

                    output.AppendFormat(" · Total Memory: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryTotal, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat(" · Used Memory: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryUsed, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat(" · Available Memory: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryFree, Unit.BinaryByte)).AppendLine();

                    if (nvGPU.PCIeThroughputRX.HasValue)
                    {
                        output.AppendFormat(" · PCI-Express RX Throughput: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.PCIeThroughputRX.Value, Unit.BytesPerSecond)).AppendLine();
                    }
                    if (nvGPU.PCIeThroughputTX.HasValue)
                    {
                        output.AppendFormat(" · PCI-Express TX Throughput: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.PCIeThroughputTX.Value, Unit.BytesPerSecond)).AppendLine();
                    }
                }

                #endregion

                #region ATI/AMD GPU

                else if (gpu.Type == GPUType.AMDGPU)
                {
                    AMDGPU amdGPU = (AMDGPU)gpu;
                    amdGPU.Update();
                    output.Append(" · AMD GPU Specific Properties:").AppendLine();

                    if (amdGPU.CoreClockSpeed.HasValue)
                        output.AppendFormat("   · Core Clock Speed: {0} MHz", amdGPU.CoreClockSpeed.Value).AppendLine();

                    if (amdGPU.MemoryClockSpeed.HasValue)
                        output.AppendFormat("   · Memory Clock Speed: {0:F2} MHz", amdGPU.MemoryClockSpeed.Value).AppendLine();

                    if (amdGPU.SoCClock.HasValue)
                        output.AppendFormat("   · SoC Clock: {0:F2} MHz", amdGPU.SoCClock.Value).AppendLine();

                    if (amdGPU.CoreLoad.HasValue)
                        output.AppendFormat("   · Core Load: {0:F2} %", amdGPU.CoreLoad.Value).AppendLine();

                    if (amdGPU.MemoryLoad.HasValue)
                        output.AppendFormat("   · Memory Load: {0:F2} %", amdGPU.MemoryLoad.Value).AppendLine();

                    if (amdGPU.CoreVoltage.HasValue)
                        output.AppendFormat("   · Core Voltage: {0:F2} V", amdGPU.CoreVoltage.Value).AppendLine();

                    if (amdGPU.MemoryVoltage.HasValue)
                        output.AppendFormat("   · Memory Voltage: {0:F2} V", amdGPU.MemoryVoltage.Value).AppendLine();

                    if (amdGPU.SoCVoltage.HasValue)
                        output.AppendFormat("   · SoC Voltage: {0:F2} V", amdGPU.SoCVoltage.Value).AppendLine();

                    if (amdGPU.FanRPM.HasValue)
                        output.AppendFormat("   · Fan Speed: {0:F2} RPM", amdGPU.FanRPM.Value).AppendLine();

                    if (amdGPU.FanSpeedPercentage.HasValue)
                        output.AppendFormat("   · Fan Speed Percentage: {0:F2} &", amdGPU.FanSpeedPercentage.Value).AppendLine();

                    if (amdGPU.FullscreenFPS.HasValue)
                        output.AppendFormat("   · Fullscreen FPS: {0:F2} FPS", amdGPU.FullscreenFPS.Value).AppendLine();

                    output.Append("   · Temperatures:").AppendLine();
                    if (amdGPU.Temperatures.Core.HasValue)
                        output.AppendFormat("     · GPU: {0:F2} °C", amdGPU.Temperatures.Core.Value).AppendLine();
                    if (amdGPU.Temperatures.HotSpot.HasValue)
                        output.AppendFormat("     · Hot Spot: {0:F2} °C", amdGPU.Temperatures.HotSpot.Value).AppendLine();
                    if (amdGPU.Temperatures.Liquid.HasValue)
                        output.AppendFormat("     · Liquid: {0:F2} °C", amdGPU.Temperatures.Liquid.Value).AppendLine();
                    if (amdGPU.Temperatures.Memory.HasValue)
                        output.AppendFormat("     · Memory: {0:F2} °C", amdGPU.Temperatures.Memory.Value).AppendLine();
                    if (amdGPU.Temperatures.MVDD.HasValue)
                        output.AppendFormat("     · MVDD: {0:F2} °C", amdGPU.Temperatures.MVDD.Value).AppendLine();
                    if (amdGPU.Temperatures.PLX.HasValue)
                        output.AppendFormat("     · PLX: {0:F2} °C", amdGPU.Temperatures.PLX.Value).AppendLine();
                    if (amdGPU.Temperatures.SoC.HasValue)
                        output.AppendFormat("     · SoC: {0:F2} °C", amdGPU.Temperatures.SoC.Value).AppendLine();
                    if (amdGPU.Temperatures.VDDC.HasValue)
                        output.AppendFormat("     · VDDC: {0:F2} °C", amdGPU.Temperatures.VDDC.Value).AppendLine();

                    output.Append(" · Power Sensors:").AppendLine();
                    if (amdGPU.PowerSensors.Core.HasValue)
                        output.AppendFormat("     · GPU Power: {0:F2} W", amdGPU.PowerSensors.Core.Value).AppendLine();
                    if (amdGPU.PowerSensors.PPT.HasValue)
                        output.AppendFormat("     · PPT: {0:F2} W", amdGPU.PowerSensors.PPT.Value).AppendLine();
                    if (amdGPU.PowerSensors.SoC.HasValue)
                        output.AppendFormat("     · SoC: {0:F2} W", amdGPU.PowerSensors.SoC.Value).AppendLine();
                    if (amdGPU.PowerSensors.Total.HasValue)
                        output.AppendFormat("     · Total Power: {0:F2} W", amdGPU.PowerSensors.Total.Value).AppendLine();
                }

                #endregion

                count++;
            }
            GPUs.DisposeGPUs();

            #endregion

            output.AppendLine();

            #region LPC Chips Information

            if (LPCChips.LoadLPCChips())
            {
                LPC[] lpcChips = LPCChips.List;
                count = 1;
                foreach (LPC lpc in lpcChips)
                {
                    #region Embedded Controller

                    if (lpc.Type == LPCType.EmbeddedController)
                    {
                        EmbeddedController ec = (EmbeddedController)lpc;
                        ec.Update();
                        output.AppendFormat("Embedded Controller #{0}:", count).AppendLine();
                        output.Append(" · Sensors:").AppendLine();
                        foreach (ECSensor ecs in ec.Sensors)
                        {
                            string typeValueFormat = "<Desconocido>: {0}";
                            switch (ecs.Type)
                            {
                                case ECSensorType.TempChipset:
                                    typeValueFormat = "Chipset Temperature: {0:F2} °C";
                                    break;
                                case ECSensorType.TempCPU:
                                    typeValueFormat = "CPU Temperature: {0:F2} °C";
                                    break;
                                case ECSensorType.TempMB:
                                    typeValueFormat = "Motherboard Temperatur: {0:F2} °C";
                                    break;
                                case ECSensorType.TempTSensor:
                                    typeValueFormat = "\"T_Sensor\" Temperature: {0:F2} °C";
                                    break;
                                case ECSensorType.TempVrm:
                                    typeValueFormat = "VRM Temperature: {0:F2} °C";
                                    break;
                                case ECSensorType.FanCPUOpt:
                                    typeValueFormat = "CPU Optional Fan Speed: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanVrmHS:
                                    typeValueFormat = "VRM Heat Sink Fan Speed: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanChipset:
                                    typeValueFormat = "Chipset Fan Speed: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanWaterFlow:
                                    typeValueFormat = "Water Flow (water cooling system): {0:F2} L/h";
                                    break;
                                case ECSensorType.CurrCPU:
                                    typeValueFormat = "CPU Current: {0:F3} A";
                                    break;
                                case ECSensorType.TempWaterIn:
                                    typeValueFormat = "Water In Temperature (water cooling system): {0:F2} °C";
                                    break;
                                case ECSensorType.TempWaterOut:
                                    typeValueFormat = "Water Out Temperature (water cooling system): {0:F2} °C";
                                    break;
                            }

                            if (ecs.Value.HasValue)
                            {
                                output.AppendFormat($"   · {typeValueFormat}", ecs.Value.Value).AppendLine();
                            }
                        }
                    }

                    #endregion

                    #region Super I/O

                    else if (lpc.Type == LPCType.SuperIO)
                    {
                        SuperIOHardware superIO = (SuperIOHardware)lpc;
                        superIO.Update();
                        output.AppendFormat("LPC Chip #{0}:", count).AppendLine();
                        output.AppendFormat(" · Chip Name: {0}", superIO.ChipName).AppendLine();

                        if (superIO.ControlSensors.Length > 0)
                        {
                            output.Append(" · Fan Controls:").AppendLine();
                            foreach (LPCControlSensor controlSensor in superIO.ControlSensors)
                            {
                                output.AppendFormat("   · {0}: {1:F2} %", controlSensor.Identifier, controlSensor.Value).AppendLine();
                            }
                        }

                        if (superIO.Fans.Length > 0)
                        {
                            output.Append(" · Fan Speeds:").AppendLine();
                            foreach (LPCSensor controlSensor in superIO.Fans)
                            {
                                output.AppendFormat("   · {0}: {1:F2} RPM", controlSensor.Identifier, controlSensor.Value).AppendLine();
                            }
                        }

                        if (superIO.Temperatures.Length > 0)
                        {
                            output.Append(" · Temperature Sensors:").AppendLine();
                            foreach (LPCSensor temps in superIO.Temperatures)
                            {
                                output.AppendFormat("   · {0}: {1:F2} °C", temps.Identifier, temps.Value).AppendLine();
                            }
                        }

                        if (superIO.Voltages.Length > 0)
                        {
                            output.Append(" · Voltages:").AppendLine();
                            foreach (LPCVoltageSensor voltageSensor in superIO.Voltages)
                            {
                                output.AppendFormat("   · {0}: {1:F2} V", voltageSensor.Identifier, voltageSensor.Value).AppendLine();
                            }
                        }
                    }

                    #endregion

                    count++;
                }
            }

            #endregion

            output.AppendLine();

            #region Storage Information

            StorageDrives.LoadDrives();
            foreach (Drive drive in from d in StorageDrives.List
                                    orderby d.Index ascending
                                    select d)
            {
                drive.Update();
                output.AppendFormat("Physical Storage #{0}:", drive.Index + 1).AppendLine();
                output.AppendFormat(" · Name: {0}", drive.Name).AppendLine();
                output.AppendFormat(" · Serial Number: {0}", drive.SerialNumber).AppendLine();
                output.AppendFormat(" · Capacity: {0}", ByteConversions.ConvertBytesToString(drive.Capacity, Unit.BinaryByte)).AppendLine();
                output.AppendFormat(" · Firmware Revision: {0}", drive.FirmwareRevision).AppendLine();
                output.AppendFormat(" · Device Id.: {0}", drive.DeviceId).AppendLine();
                output.AppendFormat(" · Type: {0}", drive.Type).AppendLine();
                output.AppendFormat(" · Removable?: {0}", drive.IsRemovable).AppendLine();
                output.AppendFormat(" · Used Capacity: {0:F2} %", drive.UsedCapacityPercentage).AppendLine();

                #region Logical Drives Info

                output.Append(" · Logical Drives:").AppendLine();
                foreach (DriveInfo logicalDrive in drive.LogicalDrives)
                {
                    long usedSpace = logicalDrive.TotalSize - logicalDrive.AvailableFreeSpace;
                    output.AppendFormat("   · Logical Drive {0}", logicalDrive.Name.Replace("\\", "")).AppendLine();
                    output.AppendFormat("     · Name: {0}", logicalDrive.VolumeLabel).AppendLine();
                    output.AppendFormat("     · Drive Type: {0}", logicalDrive.DriveType).AppendLine();
                    output.AppendFormat("     · File System: {0}", logicalDrive.DriveFormat).AppendLine();
                    output.AppendFormat("     · Root Directory: {0}", logicalDrive.RootDirectory).AppendLine();
                    output.AppendFormat("     · Capacity: {0}", ByteConversions.ConvertBytesToString((ulong)logicalDrive.TotalSize, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("     · Used Space Percentage: {0:F2} %", usedSpace * 100d / logicalDrive.TotalSize).AppendLine();
                    output.AppendFormat("     · Used Space: {0}", ByteConversions.ConvertBytesToString((ulong)usedSpace, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("     · Available Space: {0}", ByteConversions.ConvertBytesToString((ulong)logicalDrive.AvailableFreeSpace, Unit.BinaryByte)).AppendLine();
                }

                #endregion

                #region Drive Geometry Info

                output.Append(" · Drive Geometry Info:").AppendLine();
                if (drive.Geometry.BytesPerSector.HasValue)
                    output.AppendFormat("   · Bytes per Sector: {0}", drive.Geometry.BytesPerSector.Value).AppendLine();
                if (drive.Geometry.Cylinders.HasValue)
                    output.AppendFormat("   · Cylinders: {0}", drive.Geometry.Cylinders.Value).AppendLine();
                if (drive.Geometry.Heads.HasValue)
                    output.AppendFormat("   · Heads: {0}", drive.Geometry.Heads.Value).AppendLine();
                if (drive.Geometry.Sectors.HasValue)
                    output.AppendFormat("   · Sectors: {0}", drive.Geometry.Sectors.Value).AppendLine();
                if (drive.Geometry.SectorsPerTrack.HasValue)
                    output.AppendFormat("   · Sectors per Track: {0}", drive.Geometry.SectorsPerTrack.Value).AppendLine();
                if (drive.Geometry.Tracks.HasValue)
                    output.AppendFormat("   · Tracks: {0}", drive.Geometry.Tracks.Value).AppendLine();
                if (drive.Geometry.TracksPerCylinder.HasValue)
                    output.AppendFormat("   · Tracks per Cylinder: {0}", drive.Geometry.TracksPerCylinder.Value).AppendLine();

                #endregion

                if (drive.TotalActivityPercentage.HasValue)
                    output.AppendFormat(" · Total Activity: {0:F2} %", drive.TotalActivityPercentage.Value).AppendLine();

                if (drive.TotalReadActivityPercentage.HasValue)
                    output.AppendFormat(" · Read Activity: {0:F2} %", drive.TotalReadActivityPercentage.Value).AppendLine();

                if (drive.TotalWriteActivityPercentage.HasValue)
                    output.AppendFormat(" · Write Activity: {0:F2} %", drive.TotalWriteActivityPercentage.Value).AppendLine();

                if (drive.ReadSpeed.HasValue)
                    output.AppendFormat(" · Read Speed: {0}", ByteConversions.ConvertBytesToString((ulong)drive.ReadSpeed.Value, Unit.BytesPerSecond)).AppendLine();

                if (drive.WriteSpeed.HasValue)
                    output.AppendFormat(" · Write Speed: {0}", ByteConversions.ConvertBytesToString((ulong)drive.WriteSpeed.Value, Unit.BytesPerSecond)).AppendLine();

                if (drive.AverageResponseTimePerTransfer.HasValue)
                    output.AppendFormat(" · Avg. Response Time per Transfer: {0}", SecondsToString.Convert(drive.AverageResponseTimePerTransfer.Value)).AppendLine();
                
                if (drive.AverageResponseTimePerRead.HasValue)
                    output.AppendFormat(" · Avg. Response Time per Read: {0}", SecondsToString.Convert(drive.AverageResponseTimePerRead.Value)).AppendLine();
                
                if (drive.AverageResponseTimePerWrite.HasValue)
                    output.AppendFormat(" · Avg. Response Time per Write: {0}", SecondsToString.Convert(drive.AverageResponseTimePerWrite.Value)).AppendLine();

                output.AppendFormat(" · Is ATA Drive?: {0}", drive.IsATADrive).AppendLine();

                #region ATA Information

                if (drive.IsATADrive)
                {
                    output.Append(" · ATA Drive Specific Information:").AppendLine();
                    ATADrive ataDrive = (ATADrive)drive;

                    output.AppendFormat("   · Drive Health: {0}", ataDrive.DriveHealth).AppendLine();

                    if (ataDrive.PowerOnTime.HasValue)
                    {
                        output.AppendFormat("   · Power On Time: {0:F1} hours", ataDrive.PowerOnTime.Value.TotalHours).AppendLine();
                    }
                    if (ataDrive.PowerCycleCount.HasValue)
                    {
                        output.AppendFormat("   · Power Cycles: {0} times", ataDrive.PowerCycleCount.Value).AppendLine();
                    }
                    if (ataDrive.Temperature.HasValue)
                    {
                        output.AppendFormat("   · Temperature: {0:F2} °C", ataDrive.Temperature.Value).AppendLine();
                    }

                    output.Append("   · S.M.A.R.T. Information:").AppendLine();
                    output.Append("     ———————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————").AppendLine();
                    output.Append("     Index    | Identifier      | Attribute Name                                    | Current Value  | Worst Value  | Threshold | Converted Value    | Raw Value        ").AppendLine();
                    output.Append("     ———————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————").AppendLine();
                    count = 1;
                    foreach (SMARTSensor sensor in from s in ataDrive.SMARTSensors
                                                   orderby s.Attribute.Id ascending
                                                   select s)
                    {
                        output.AppendFormat("     {0,-5}", count);
                        output.AppendFormat("    | {0,-12:X2}", sensor.Attribute.Id);
                        output.AppendFormat("    | {0,-46}", sensor.Attribute.Name);
                        output.AppendFormat("    | {0,-11}", sensor.NormalizedValue);
                        output.AppendFormat("    | {0,-9}", sensor.WorstValue);
                        output.AppendFormat("    | {0,-6}", sensor.Threshold);
                        output.AppendFormat("    | {0,-16}", sensor.Value);
                        output.AppendFormat("    | {0}", ConvertBytesToHexString(sensor.RawValue));

                        output.AppendLine("\n     ———————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————");
                        count++;
                    }
                }

                #endregion

                #region NVMe Information

                else if (drive.Type == PhysicalDriveType.NVMe)
                {
                    output.Append(" · NVM Express (NVMe) Drive Specific Information:").AppendLine();
                    NVMeDrive nvmeDrive = (NVMeDrive)drive;

                    output.AppendFormat("   · Total NVM Capacity: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.TotalNVMCapacity, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("   · Unallocated NVM Capacity: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.UnallocatedNVMCapacity, Unit.BinaryByte)).AppendLine();

                    output.Append("   · Critical Warnings:").AppendLine();
                    NVMeCriticalWarning[] warnings = nvmeDrive.CriticalWarnings;
                    if (warnings.Length > 0)
                    {
                        foreach (NVMeCriticalWarning warning in nvmeDrive.CriticalWarnings)
                        {
                            string criticalWarningDesc = "";
                            switch (warning)
                            {
                                case NVMeCriticalWarning.AvailableSpaceLow:
                                    criticalWarningDesc = "Available spare space has fallen below the threshold.";
                                    break;
                                case NVMeCriticalWarning.TemperatureThreshold:
                                    criticalWarningDesc = "A temperature sensor is above an over-temperature threshold or an under-temperature threshold.";
                                    break;
                                case NVMeCriticalWarning.ReliabilityDegraded:
                                    criticalWarningDesc = "Device reliability has been degraded due to significant media-related errors or any internal error that degrades device reliability.";
                                    break;
                                case NVMeCriticalWarning.ReadOnly:
                                    criticalWarningDesc = "Device has entered in read-only mode.";
                                    break;
                                case NVMeCriticalWarning.VolatileMemoryBackupDeviceFailed:
                                    criticalWarningDesc = "Volatile memory backup device has failed.";
                                    break;
                            }
                            output.AppendFormat("     · {0}", criticalWarningDesc).AppendLine();
                        }
                    }
                    else
                    {
                        output.Append("     · There are no warnings to show").AppendLine();
                    }

                    if (nvmeDrive.Temperature.HasValue)
                        output.AppendFormat("   ·  Temperature: {0} °C", nvmeDrive.Temperature.Value).AppendLine();

                    output.AppendFormat("   ·  Temperature Sensors:").AppendLine();
                    count = 1;
                    foreach (double tempSensor in nvmeDrive.TemperatureSensors)
                    {
                        output.AppendFormat("     ·  Sensor #{0}: {1:F2} °C", count, tempSensor).AppendLine();
                        count++;
                    }

                    if (nvmeDrive.AvailableSpare.HasValue)
                        output.AppendFormat("   · Available Spare: {0}", ByteConversions.ConvertBytesToString((ulong)nvmeDrive.AvailableSpare.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.AvailableSpareThreshold.HasValue)
                        output.AppendFormat("   · Available Spare Threshold: {0}", ByteConversions.ConvertBytesToString((ulong)nvmeDrive.AvailableSpareThreshold.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.PercentageUsed.HasValue)
                        output.AppendFormat("   · Percentage Used: {0} %", nvmeDrive.DataRead.Value).AppendLine();
                    if (nvmeDrive.DataRead.HasValue)
                        output.AppendFormat("   · Data Read: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.DataRead.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.DataWritten.HasValue)
                        output.AppendFormat("   · Data Written: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.DataWritten.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.HostReadCommands.HasValue)
                        output.AppendFormat("   · Host Read Commands: {0}", nvmeDrive.HostReadCommands.Value).AppendLine();
                    if (nvmeDrive.HostWriteCommands.HasValue)
                        output.AppendFormat("   · Host Write Commands: {0}", nvmeDrive.HostWriteCommands.Value).AppendLine();
                    if (nvmeDrive.ControllerBusyTime.HasValue)
                        output.AppendFormat("   · Controller Busy Time: {0}", nvmeDrive.ControllerBusyTime.Value).AppendLine();
                    if (nvmeDrive.PowerCycles.HasValue)
                        output.AppendFormat("   · Power Cycles: {0} times", nvmeDrive.PowerCycles.Value).AppendLine();
                    if (nvmeDrive.PowerOnTime.HasValue)
                        output.AppendFormat("   · Power On Time: {0:g}", nvmeDrive.PowerOnTime.Value).AppendLine();
                    if (nvmeDrive.UnsafeShutdowns.HasValue)
                        output.AppendFormat("   · Unsafe Shutdowns: {0}", nvmeDrive.UnsafeShutdowns.Value).AppendLine();
                    if (nvmeDrive.MediaErrors.HasValue)
                        output.AppendFormat("   · Media Errors: {0}", nvmeDrive.MediaErrors.Value).AppendLine();
                    if (nvmeDrive.ErrorInfoLogCount.HasValue)
                        output.AppendFormat("   · Error Info Log Count: {0}", nvmeDrive.ErrorInfoLogCount.Value).AppendLine();
                    if (nvmeDrive.WarningCompositeTemperatureTime.HasValue)
                        output.AppendFormat("   · Warning Composite Temperature Time: {0}", nvmeDrive.WarningCompositeTemperatureTime.Value).AppendLine();
                    if (nvmeDrive.CriticalCompositeTemperatureTime.HasValue)
                        output.AppendFormat("   · Critical Composite Temperature Time: {0}", nvmeDrive.CriticalCompositeTemperatureTime.Value).AppendLine();

                    output.AppendFormat("   · PCI Vendor ID: {0} (0x{0:X})", nvmeDrive.PCIVendorID).AppendLine();
                    output.AppendFormat("   · PCI Subsystem Vendor ID: {0} (0x{0:X})", nvmeDrive.PCISubsystemVendorID).AppendLine();
                    output.AppendFormat("   · IEEE OUI Identifier: {0}", nvmeDrive.IEEEOuiIdentifier).AppendLine();
                    output.AppendFormat("   · Controller ID: {0}", nvmeDrive.ControllerID).AppendLine();
                }

                #endregion
            }
            StorageDrives.DisposeDrives();

            #endregion

            Console.WriteLine(output);
            Console.Write("Press any key to exit...");
            Console.ReadKey();
            LibrarySettings.Close();
        }
    }
}
