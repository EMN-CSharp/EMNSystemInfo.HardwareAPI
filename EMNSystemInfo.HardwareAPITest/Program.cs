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
            string nodeName = "<Desconocido>";
            switch (nodeEngineType)
            {
                case NodeEngineType.Other:
                    if (string.IsNullOrEmpty(nodeEngTypeStrWhenOther))
                    {
                        nodeName = "Otro";
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
                    nodeName = "Decodificación de video";
                    break;
                case NodeEngineType.VideoEncode:
                    nodeName = "Codificación de video";
                    break;
                case NodeEngineType.VideoProcessing:
                    nodeName = "Procesado de video";
                    break;
                case NodeEngineType.SceneAssembly:
                    nodeName = "Ensamblado de escena";
                    break;
                case NodeEngineType.Copy:
                    nodeName = "Copia";
                    break;
                case NodeEngineType.Overlay:
                    nodeName = "Incrustación";
                    break;
                case NodeEngineType.Crypto:
                    nodeName = "Criptografía";
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
                string chemistry = "<Desconocida>";
                switch (bat.Chemistry)
                {
                    case BatteryChemistry.LeadAcid:
                        chemistry = "Ácido-plomo";
                        break;
                    case BatteryChemistry.NickelCadmium:
                        chemistry = "Níquel-cadmio";
                        break;
                    case BatteryChemistry.NickelMetalHydride:
                        chemistry = "Níquel-hidruro metálico";
                        break;
                    case BatteryChemistry.LithiumIon:
                        chemistry = "Ion de litio";
                        break;
                    case BatteryChemistry.NickelZinc:
                        chemistry = "Níquel-zinc";
                        break;
                    case BatteryChemistry.AlkalineManganese:
                        chemistry = "Alcalina de manganeso recargable";
                        break;
                }

                string powerState = "<Desconocido>";
                switch (bat.PowerState)
                {
                    case BatteryPowerState.Charging:
                        powerState = "Cargando";
                        break;
                    case BatteryPowerState.Critical:
                        powerState = "Crítico";
                        break;
                    case BatteryPowerState.Discharging:
                        powerState = "Descargando";
                        break;
                    case BatteryPowerState.OnAC:
                        powerState = "Conectado al cargador";
                        break;
                }

                output.Append("Batería N.º ").Append(count).AppendLine(":")
                      .Append(" · Nombre: ").AppendLine(bat.Name)
                      .Append(" · Fabricante: ").AppendLine(bat.Manufacturer)
                      .Append(" · Estado: ").AppendLine(powerState)
                      .Append(" · Composición química: ").AppendLine(chemistry)
                      .Append(" · Porcentaje de degradación: ").AppendFormat("{0:F2}", bat.DegradationLevel).AppendLine(" %")
                      .Append(" · Capacidad de fábrica: ").Append(bat.DesignedCapacity).AppendLine(" mWh")
                      .Append(" · Capacidad a máxima carga: ").Append(bat.FullChargedCapacity).AppendLine(" mWh");
                if (bat.RemainingCapacity.HasValue)
                {
                    output.Append(" · Capacidad restante: ").Append(bat.RemainingCapacity.Value).AppendLine(" mWh");
                }
                if (bat.ChargeLevel.HasValue)
                {
                    output.Append(" · Nivel de carga: ").AppendFormat("{0:F2}", bat.ChargeLevel.Value).AppendLine(" %");
                }
                if (bat.Voltage.HasValue)
                { 
                    output.Append(" · Voltaje: ").AppendFormat("{0:F3}", bat.Voltage.Value).AppendLine(" V");
                }
                if (bat.EstimatedRemainingTime.HasValue)
                {
                    output.Append(" · Tiempo restante (estimado): ").AppendFormat("{0:g}", bat.EstimatedRemainingTime.Value).AppendLine();
                }
                if (bat.ChargeDischargeRate.HasValue)
                {
                    switch (bat.ChargeDischargeRate.Value)
                    {
                        case > 0:
                            output.Append(" · Velocidad de carga: ").AppendFormat("{0:F1}", bat.ChargeDischargeRate).AppendLine(" W")
                                  .Append(" · Corriente de carga: ").AppendFormat("{0:F3}", bat.ChargeDischargeRate / bat.Voltage).AppendLine(" A");

                            break;
                        case < 0:
                            output.Append(" · Velocidad de descarga: ").AppendFormat("{0:F1}", Math.Abs(bat.ChargeDischargeRate.Value)).AppendLine(" W")
                                  .Append(" · Corriente de descarga: ").AppendFormat("{0:F3}", Math.Abs(bat.ChargeDischargeRate.Value) / bat.Voltage).AppendLine(" A");

                            break;
                        default:
                            output.AppendLine(" · Velocidad de carga/descarga: 0 W")
                                  .AppendLine(" · Corriente de carga/descarga: 0 A");

                            break;
                    }
                }
                count++;
            }
            Batteries.DisposeAllBatteries();

            #endregion

            output.AppendLine();

            #region Processors Information

            IList<Processor> processors = Processors.List;
            foreach (Processor p in processors)
            {
                output.AppendFormat("Procesador N.º {0}", p.Index + 1).AppendLine();
                output.AppendFormat(" · Nombre: {0}", p.BrandString).AppendLine();
                output.AppendFormat(" · Nombre resumido: {0}", p.Name).AppendLine();
                output.AppendFormat(" · Fabricante: {0}", p.Vendor).AppendLine();
                output.AppendFormat(" · Porcentaje de utilización: {0:F2} %", p.TotalLoad).AppendLine();

                output.Append(" · Utilización de los núcleos:").AppendLine();
                foreach (ThreadLoad threadLoad in p.ThreadLoads)
                {
                    if (threadLoad.Thread != null)
                    {
                        output.AppendFormat("   · Núcleo N.º {0}, hilo N.º {1}: {2:F2} %", threadLoad.Core + 1, threadLoad.Thread + 1, threadLoad.Value).AppendLine();
                    }
                    else
                    {
                        output.AppendFormat("   · Núcleo N.º {0}: {1:F2} %", threadLoad.Core + 1, threadLoad.Value).AppendLine();
                    }
                }

                output.AppendFormat(" · ¿Tiene TSC?: {0}", p.HasTimeStampCounter).AppendLine();
                if (p.HasTimeStampCounter)
                {
                    output.AppendFormat(" · Frecuencia del TSC: {0:F2} MHz", p.TimeStampCounterFrequency).AppendLine();
                }
                output.AppendFormat(" · ¿Tiene MSRs?: {0}", p.HasModelSpecificRegisters).AppendLine();

                #region Intel CPU

                if (p.Type == ProcessorType.IntelCPU)
                {
                    IntelCPU intelCPU = (IntelCPU)p;
                    intelCPU.Update();
                    output.Append(" · Propiedades específicas de Intel:").AppendLine();
                    output.AppendFormat("   · Microarquitectura: {0}", intelCPU.Microarchitecture).AppendLine();
                    output.AppendFormat("   · Frecuencia del bus: {0:F2} MHz", intelCPU.BusClock).AppendLine();
                    output.AppendFormat("   · Voltaje del núcleo (VID): {0:F3} V", intelCPU.CoreVoltage).AppendLine();

                    output.Append("   · Frecuencias de reloj por núcleo:").AppendLine();
                    count = 1;
                    foreach (double clock in intelCPU.CoreFrequencyClocks)
                    {
                        output.AppendFormat("     · Núcleo N.º {0}: {1:F2} MHz (multiplicador: × {2:F2})", count, clock, clock / intelCPU.BusClock).AppendLine();
                        count++;
                    }

                    if (intelCPU.PackageTemperature.HasValue)
                    {
                        output.AppendFormat("   · Temperatura del encapsulado: {0:F2} °C", intelCPU.PackageTemperature.Value.Value).AppendLine();
                        output.AppendFormat("   · Temperatura máxima soportada por el encapsulado (valor TjMax): {0:F2} °C", intelCPU.PackageTemperature.Value.TjMax).AppendLine();
                    }
                    output.AppendFormat("   · Temperatura promedio del núcleo: {0:F2} °C", intelCPU.CoreAverageTemperature).AppendLine();

                    output.Append("   · Temperaturas por núcleo:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in intelCPU.CoreTemperatures)
                    {
                        output.AppendFormat("     · Núcleo N.º {0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Sensores de potencia:").AppendLine();
                    count = 1;
                    foreach (IntelPowerSensor? ps in intelCPU.PowerSensors)
                    {
                        string type = "<Desconocido>";
                        switch (ps?.Type)
                        {
                            case IntelPowerSensorType.Package:
                                type = "Potencia total consumida";
                                break;
                            case IntelPowerSensorType.Cores:
                                type = "Potencia consumida por los núcleos";
                                break;
                            case IntelPowerSensorType.Graphics:
                                type = "Potencia consumida por los gráficos";
                                break;
                            case IntelPowerSensorType.Memory:
                                type = "Potencia consumida por la memoria";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} W", type, ps?.Value).AppendLine();
                        count++;
                    }
                }

                #endregion

                #region AMD 0F CPU

                else if (p.Type == ProcessorType.AMD0FCPU)
                {
                    AMD0FCPU amd0FCPU = (AMD0FCPU)p;
                    amd0FCPU.Update();
                    output.Append(" · Propiedades específicas de AMD:").AppendLine();
                    output.AppendFormat("   · Frecuencia del bus: {0:F2} MHz", amd0FCPU.BusClock).AppendLine();

                    output.Append("   · Frecuencias de reloj por núcleo:").AppendLine();
                    count = 1;
                    foreach (double clock in amd0FCPU.CoreFrequencyClocks)
                    {
                        output.AppendFormat("     · Núcleo N.º {0}: {1:F2} MHz (multiplicador: × {2:F2})", count, clock, clock / amd0FCPU.BusClock).AppendLine();
                        count++;
                    }

                    output.Append("   · Temperaturas por núcleo:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in amd0FCPU.CoreTemperatures)
                    {
                        output.AppendFormat("     · Núcleo N.º {0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }
                }

                #endregion

                #region AMD 10 CPU

                else if (p.Type == ProcessorType.AMD10CPU)
                {
                    AMD10CPU amd10CPU = (AMD10CPU)p;
                    amd10CPU.Update();
                    output.Append(" · Propiedades específicas de AMD:").AppendLine();
                    output.AppendFormat("   · Frecuencia del bus: {0:F2} MHz", amd10CPU.BusClock).AppendLine();
                    output.AppendFormat("   · Voltaje del núcleo (VID): {0:F3} V", amd10CPU.CoreVoltage).AppendLine();
                    output.AppendFormat("   · Voltaje del puente norte: {0:F3} V", amd10CPU.NorthbridgeVoltage).AppendLine();
                    output.AppendFormat("   · C2 State Residency Level: {0:F3} %", amd10CPU.C2StateResidencyLevel).AppendLine();
                    output.AppendFormat("   · C3 State Residency Level: {0:F3} %", amd10CPU.C3StateResidencyLevel).AppendLine();

                    output.Append("   · Frecuencias de reloj por núcleo:").AppendLine();
                    count = 1;
                    foreach (double clock in amd10CPU.CoreFrequencyClocks)
                    {
                        output.AppendFormat("     · Núcleo N.º {0}: {1:F2} MHz (multiplicador: × {2:F2})", count, clock, clock / amd10CPU.BusClock).AppendLine();
                        count++;
                    }

                    output.AppendFormat("   · Temperatura del núcleo: {1:F2} °C", count, amd10CPU.CoreTemperature).AppendLine();
                }

                #endregion

                #region AMD 17 CPU

                else if (p.Type == ProcessorType.AMD17CPU)
                {
                    AMD17CPU amd17CPU = (AMD17CPU)p;
                    amd17CPU.Update();
                    output.Append(" · Propiedades específicas de AMD:").AppendLine();
                    output.AppendFormat("   · Nombre código: {0}", amd17CPU.CodeName).AppendLine();
                    output.AppendFormat("   · Frecuencia del bus: {0:F2} MHz", amd17CPU.BusClock).AppendLine();
                    output.AppendFormat("   · Voltaje del núcleo (VID): {0:F3} V", amd17CPU.CoreVoltage).AppendLine();
                    output.AppendFormat("   · Potencia total consumida: {0:F3} V", amd17CPU.PackagePower).AppendLine();

                    if (amd17CPU.SoCVoltage.HasValue)
                    {
                        output.AppendFormat("   · Voltaje del SoC: {0:F3} V", amd17CPU.SoCVoltage.Value).AppendLine();
                    }

                    output.AppendFormat("   · Temperatura del núcleo (Tctl): {1:F2} °C", count, amd17CPU.CoreTemperatureTctl).AppendLine();
                    output.AppendFormat("   · Temperatura del núcleo (Tctl/Tdie): {1:F2} °C", count, amd17CPU.CoreTemperatureTctlTdie).AppendLine();
                    output.AppendFormat("   · Temperatura del núcleo (Tdie): {1:F2} °C", count, amd17CPU.CoreTemperatureTctlTdie).AppendLine();

                    output.Append("   · Temperaturas de los CCDs:").AppendLine();
                    count = 1;
                    foreach (CoreTemperature temp in amd17CPU.CCDTemperatures)
                    {
                        output.AppendFormat("     · CCD {0}: {1:F2} °C", count, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Sensores SMU:").AppendLine();
                    foreach (var smuSensor in amd17CPU.SMUSensors)
                    {
                        switch (smuSensor.Key.Type)
                        {
                            case SMUSensorType.Voltage:
                                output.AppendFormat("     · (Voltaje) {0}: {1:F2} V", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Current:
                                output.AppendFormat("     · (Corriente) {0}: {1:F2} A", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Power:
                                output.AppendFormat("     · (Potencia) {0}: {1:F2} W", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Clock:
                                output.AppendFormat("     · (Frecuencia de reloj) {0}: {1:F2} MHz", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Temperature:
                                output.AppendFormat("     · (Temperatura) {0}: {1:F2} °C", smuSensor.Key.Name, smuSensor.Value).AppendLine();
                                break;
                            case SMUSensorType.Load:
                                output.AppendFormat("     · (Utilización) {0}: {1:F2} %", smuSensor.Key.Name, smuSensor.Value).AppendLine();
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
                output.AppendFormat("GPU N.º {0}:", count).AppendLine();
                output.AppendFormat(" · Nombre: {0}", gpu.Name).AppendLine();
                output.AppendFormat(" · Uso de memoria dedicada: {0}", ByteConversions.ConvertBytesToString(gpu.DedicatedMemoryUsage, Unit.BinaryByte)).AppendLine();
                output.AppendFormat(" · Uso de memoria compartida: {0}", ByteConversions.ConvertBytesToString(gpu.SharedMemoryUsage, Unit.BinaryByte)).AppendLine();
                output.Append(" · Uso de los nodos de motores gráficos:").AppendLine();
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
                            output.AppendFormat(@"   · {0} N.º {1}: {2:F2} % (tiempo activado: {3:hh\:mm\:ss\.fff})", nodeName, nodeCount, node.Value, node.RunningTime).AppendLine();
                        }
                        else
                        {
                            output.AppendFormat(@"   · {0}: {1:F2} % (tiempo activado: {2:hh\:mm\:ss\.fff})", nodeName, node.Value, node.RunningTime).AppendLine();
                        }
                    }
                }

                #region Intel integrated GPU

                if (gpu.Type == GPUType.IntelIntegratedGPU)
                {
                    IntelIntegratedGPU iiGPU = (IntelIntegratedGPU)gpu;
                    iiGPU.Update();
                    output.Append(" · Propiedades específicas de las GPU integradas Intel:").AppendLine();

                    if (iiGPU.TotalPower.HasValue)
                    {
                        output.AppendFormat("   · Potencia total consumida: {0:F2} W", iiGPU.TotalPower.Value).AppendLine();
                    }
                }

                #endregion

                #region NVIDIA GPU

                else if (gpu.Type == GPUType.NvidiaGPU)
                {
                    NvidiaGPU nvGPU = (NvidiaGPU)gpu;
                    nvGPU.Update();
                    output.Append(" · Propiedades específicas de las GPU NVIDIA:").AppendLine();

                    output.Append("   · Sensores de utilización:").AppendLine();
                    foreach (NvidiaLoadSensor load in nvGPU.Loads)
                    {
                        string type = "<Desconocido>";
                        switch (load.Type)
                        {
                            case NvidiaLoadType.Gpu:
                                type = "Utilización de la GPU";
                                break;
                            case NvidiaLoadType.FrameBuffer:
                                type = "Utilización del framebuffer";
                                break;
                            case NvidiaLoadType.VideoEngine:
                                type = "Utilización del motor de video";
                                break;
                            case NvidiaLoadType.BusInterface:
                                type = "Utilización de la interfaz de bus";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} %", type, load.Value).AppendLine();
                        count++;
                    }

                    output.AppendFormat(" · Temperatura de la zona caliente: {0} °C", nvGPU.HotSpotTemperature).AppendLine();
                    output.AppendFormat(" · Temperatura en la unión de las memorias: {0} °C", nvGPU.MemoryJunctionTemperature).AppendLine();

                    output.Append("   · Sensores de temperatura:").AppendLine();
                    foreach (NvidiaTempSensor temp in nvGPU.Temperatures)
                    {
                        string type = "<Desconocido>";
                        switch (temp.Type)
                        {
                            case NvidiaTempSensorType.Gpu:
                                type = "GPU";
                                break;
                            case NvidiaTempSensorType.Memory:
                                type = "Memoria";
                                break;
                            case NvidiaTempSensorType.PowerSupply:
                                type = "Fuente de alimentación";
                                break;
                            case NvidiaTempSensorType.Board:
                                type = "PCB";
                                break;
                            case NvidiaTempSensorType.VisualComputingBoard:
                                type = "Placa de computación visual";
                                break;
                            case NvidiaTempSensorType.VisualComputingInlet:
                                type = "Entrada de computación visual";
                                break;
                            case NvidiaTempSensorType.VisualComputingOutlet:
                                type = "Salida de computación visual";
                                break;
                            case NvidiaTempSensorType.All:
                                type = "Todo";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} °C", type, temp.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Frecuencias de reloj:").AppendLine();
                    foreach (NvidiaClockSensor clock in nvGPU.FrequencyClocks)
                    {
                        string type = "<Desconocido>";
                        switch (clock.Type)
                        {
                            case NvidiaClockType.Graphics:
                                type = "Gráficos";
                                break;
                            case NvidiaClockType.Memory:
                                type = "Memoria";
                                break;
                            case NvidiaClockType.Processor:
                                type = "Procesador";
                                break;
                            case NvidiaClockType.Video:
                                type = "Video";
                                break;
                            case NvidiaClockType.Undefined:
                                type = "<Sin definir>";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} MHz", type, clock.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Sensores de potencia:").AppendLine();
                    foreach (NvidiaPowerSensor power in nvGPU.PowerSensors)
                    {
                        string type = "<Desconocido>";
                        switch (power.Type)
                        {
                            case NvidiaPowerType.Gpu:
                                type = "Potencia consumida por la GPU";
                                break;
                            case NvidiaPowerType.Board:
                                type = "Potencia consumida por el PCB";
                                break;
                        }
                        output.AppendFormat("     · {0}: {1:F2} W", type, power.Value).AppendLine();
                        count++;
                    }

                    output.Append("   · Velocidad de los ventiladores:").AppendLine();
                    count = 1;
                    foreach (double fanRPM in nvGPU.FanRPMs)
                    {
                        output.AppendFormat("     · Ventilador N.º {0}: {1:F2} RPM", count, fanRPM).AppendLine();
                        count++;
                    }

                    output.AppendFormat(" · Memoria total: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryTotal, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat(" · Memoria en uso: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryUsed, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat(" · Memoria disponible: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.MemoryFree, Unit.BinaryByte)).AppendLine();

                    if (nvGPU.PCIeThroughputRX.HasValue)
                    {
                        output.AppendFormat(" · Velocidad de recepción de datos del bus PCI-Express: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.PCIeThroughputRX.Value, Unit.BytesPerSecond)).AppendLine();
                    }
                    if (nvGPU.PCIeThroughputTX.HasValue)
                    {
                        output.AppendFormat(" · Velocidad de transmisión de datos del bus PCI-Express: {0}", ByteConversions.ConvertBytesToString((ulong)nvGPU.PCIeThroughputTX.Value, Unit.BytesPerSecond)).AppendLine();
                    }
                }

                #endregion

                #region ATI/AMD GPU

                else if (gpu.Type == GPUType.AMDGPU)
                {
                    AMDGPU amdGPU = (AMDGPU)gpu;
                    amdGPU.Update();
                    output.Append(" · Propiedades específicas de las GPU dedicadas AMD:").AppendLine();

                    if (amdGPU.CoreFrequencyClock.HasValue)
                        output.AppendFormat("   · Frecuencia de reloj de la GPU: {0} MHz", amdGPU.CoreFrequencyClock.Value).AppendLine();

                    if (amdGPU.MemoryFrequencyClock.HasValue)
                        output.AppendFormat("   · Frecuencia de reloj de la memoria: {0:F2} MHz", amdGPU.MemoryFrequencyClock.Value).AppendLine();

                    if (amdGPU.SoCClock.HasValue)
                        output.AppendFormat("   · Frecuencia de reloj del SoC: {0:F2} MHz", amdGPU.SoCClock.Value).AppendLine();

                    if (amdGPU.CoreLoad.HasValue)
                        output.AppendFormat("   · Utilización de la GPU: {0:F2} %", amdGPU.CoreLoad.Value).AppendLine();

                    if (amdGPU.MemoryLoad.HasValue)
                        output.AppendFormat("   · Utilización de la memoria: {0:F2} %", amdGPU.MemoryLoad.Value).AppendLine();

                    if (amdGPU.CoreVoltage.HasValue)
                        output.AppendFormat("   · Voltaje de la GPU: {0:F2} V", amdGPU.CoreVoltage.Value).AppendLine();

                    if (amdGPU.MemoryVoltage.HasValue)
                        output.AppendFormat("   · Voltaje de la memoria: {0:F2} V", amdGPU.MemoryVoltage.Value).AppendLine();

                    if (amdGPU.SoCVoltage.HasValue)
                        output.AppendFormat("   · Voltaje del SoC: {0:F2} V", amdGPU.SoCVoltage.Value).AppendLine();

                    if (amdGPU.FanRPM.HasValue)
                        output.AppendFormat("   · Velocidad del ventilador: {0:F2} RPM", amdGPU.FanRPM.Value).AppendLine();

                    if (amdGPU.FanSpeedPercentage.HasValue)
                        output.AppendFormat("   · Porcentaje de velocidad del ventilador: {0:F2} &", amdGPU.FanSpeedPercentage.Value).AppendLine();

                    if (amdGPU.FullscreenFPS.HasValue)
                        output.AppendFormat("   · FPS a pantalla completa: {0:F2} FPS", amdGPU.FullscreenFPS.Value).AppendLine();

                    output.Append("   · Temperaturas:").AppendLine();
                    if (amdGPU.Temperatures.Core.HasValue)
                        output.AppendFormat("     · GPU: {0:F2} °C", amdGPU.Temperatures.Core.Value).AppendLine();
                    if (amdGPU.Temperatures.HotSpot.HasValue)
                        output.AppendFormat("     · Zona caliente: {0:F2} °C", amdGPU.Temperatures.HotSpot.Value).AppendLine();
                    if (amdGPU.Temperatures.Liquid.HasValue)
                        output.AppendFormat("     · Líquido: {0:F2} °C", amdGPU.Temperatures.Liquid.Value).AppendLine();
                    if (amdGPU.Temperatures.Memory.HasValue)
                        output.AppendFormat("     · Memoria: {0:F2} °C", amdGPU.Temperatures.Memory.Value).AppendLine();
                    if (amdGPU.Temperatures.MVDD.HasValue)
                        output.AppendFormat("     · MVDD: {0:F2} °C", amdGPU.Temperatures.MVDD.Value).AppendLine();
                    if (amdGPU.Temperatures.PLX.HasValue)
                        output.AppendFormat("     · PLX: {0:F2} °C", amdGPU.Temperatures.PLX.Value).AppendLine();
                    if (amdGPU.Temperatures.SoC.HasValue)
                        output.AppendFormat("     · SoC: {0:F2} °C", amdGPU.Temperatures.SoC.Value).AppendLine();
                    if (amdGPU.Temperatures.VDDC.HasValue)
                        output.AppendFormat("     · VDDC: {0:F2} °C", amdGPU.Temperatures.VDDC.Value).AppendLine();

                    output.Append(" · Sensores de potencia:").AppendLine();
                    if (amdGPU.PowerSensors.Core.HasValue)
                        output.AppendFormat("     · Potencia consumida por la GPU: {0:F2} W", amdGPU.PowerSensors.Core.Value).AppendLine();
                    if (amdGPU.PowerSensors.PPT.HasValue)
                        output.AppendFormat("     · Potencia consumida por el PPT: {0:F2} W", amdGPU.PowerSensors.PPT.Value).AppendLine();
                    if (amdGPU.PowerSensors.SoC.HasValue)
                        output.AppendFormat("     · Potencia consumida por el SoC: {0:F2} W", amdGPU.PowerSensors.SoC.Value).AppendLine();
                    if (amdGPU.PowerSensors.Total.HasValue)
                        output.AppendFormat("     · Potencia total consumida: {0:F2} W", amdGPU.PowerSensors.Total.Value).AppendLine();
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
                        output.AppendFormat("Controlador embebido (Embedded Controller) N.º {0}:", count).AppendLine();
                        output.Append(" · Sensores:").AppendLine();
                        foreach (ECSensor ecs in ec.Sensors)
                        {
                            string typeValueFormat = "<Desconocido>: {0}";
                            switch (ecs.Type)
                            {
                                case ECSensorType.TempChipset:
                                    typeValueFormat = "Temperatura del chipset: {0:F2} °C";
                                    break;
                                case ECSensorType.TempCPU:
                                    typeValueFormat = "Temperatura de la CPU: {0:F2} °C";
                                    break;
                                case ECSensorType.TempMB:
                                    typeValueFormat = "Temperatura de la placa base: {0:F2} °C";
                                    break;
                                case ECSensorType.TempTSensor:
                                    typeValueFormat = "Temperatura del \"T_Sensor\": {0:F2} °C";
                                    break;
                                case ECSensorType.TempVrm:
                                    typeValueFormat = "Temperatura del VRM: {0:F2} °C";
                                    break;
                                case ECSensorType.FanCPUOpt:
                                    typeValueFormat = "Velocidad del ventilador opcional de la CPU: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanVrmHS:
                                    typeValueFormat = "Velocidad del ventilador disipador del VRM: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanChipset:
                                    typeValueFormat = "Velocidad del ventilador del chipset: {0:F2} RPM";
                                    break;
                                case ECSensorType.FanWaterFlow:
                                    typeValueFormat = "Caudal de líquido refrigerante: {0:F2} L/h";
                                    break;
                                case ECSensorType.CurrCPU:
                                    typeValueFormat = "Corriente de la CPU: {0:F3} A";
                                    break;
                                case ECSensorType.TempWaterIn:
                                    typeValueFormat = "Temperatura de entrada del líquido refrigerante: {0:F2} °C";
                                    break;
                                case ECSensorType.TempWaterOut:
                                    typeValueFormat = "Temperatura de salida del líquido refrigerante: {0:F2} °C";
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
                        output.AppendFormat("Chip LPC N.º {0}:", count).AppendLine();
                        output.AppendFormat(" · Nombre y modelo: {0}", superIO.ChipName).AppendLine();

                        if (superIO.ControlSensors.Length > 0)
                        {
                            output.Append(" · Controles de los ventiladores:").AppendLine();
                            foreach (LPCControlSensor controlSensor in superIO.ControlSensors)
                            {
                                output.AppendFormat("   · {0}: {1:F2} %", controlSensor.Identifier, controlSensor.Value).AppendLine();
                            }
                        }

                        if (superIO.Fans.Length > 0)
                        {
                            output.Append(" · Velocidades de los ventiladores:").AppendLine();
                            foreach (LPCSensor controlSensor in superIO.Fans)
                            {
                                output.AppendFormat("   · {0}: {1:F2} RPM", controlSensor.Identifier, controlSensor.Value).AppendLine();
                            }
                        }

                        if (superIO.Temperatures.Length > 0)
                        {
                            output.Append(" · Sensores de temperatura:").AppendLine();
                            foreach (LPCSensor temps in superIO.Temperatures)
                            {
                                output.AppendFormat("   · {0}: {1:F2} °C", temps.Identifier, temps.Value).AppendLine();
                            }
                        }

                        if (superIO.Voltages.Length > 0)
                        {
                            output.Append(" · Voltajes:").AppendLine();
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
                output.AppendFormat("Unidad de almacenamiento N.º {0}:", drive.Index + 1).AppendLine();
                output.AppendFormat(" · Nombre: {0}", drive.Name).AppendLine();
                output.AppendFormat(" · Número de serie: {0}", drive.SerialNumber).AppendLine();
                output.AppendFormat(" · Capacidad: {0}", ByteConversions.ConvertBytesToString(drive.Capacity, Unit.BinaryByte)).AppendLine();
                output.AppendFormat(" · Revisión del firmware: {0}", drive.FirmwareRevision).AppendLine();
                output.AppendFormat(" · Id. del dispositivo: {0}", drive.DeviceId).AppendLine();
                output.AppendFormat(" · Tipo: {0}", drive.Type).AppendLine();
                output.AppendFormat(" · ¿Es removible?: {0}", drive.IsRemovable).AppendLine();
                output.AppendFormat(" · Porcentaje de espacio ocupado: {0:F2} %", drive.UsedCapacityPercentage).AppendLine();

                #region Logical Drives Info

                output.Append(" · Unidades lógicas:").AppendLine();
                foreach (DriveInfo logicalDrive in drive.LogicalDrives)
                {
                    long usedSpace = logicalDrive.TotalSize - logicalDrive.AvailableFreeSpace;
                    output.AppendFormat("   · Unidad lógica {0}", logicalDrive.Name.Replace("\\", "")).AppendLine();
                    output.AppendFormat("     · Nombre: {0}", logicalDrive.VolumeLabel).AppendLine();
                    output.AppendFormat("     · Tipo de unidad: {0}", logicalDrive.DriveType).AppendLine();
                    output.AppendFormat("     · Sistema de archivos: {0}", logicalDrive.DriveFormat).AppendLine();
                    output.AppendFormat("     · Directorio raíz: {0}", logicalDrive.RootDirectory).AppendLine();
                    output.AppendFormat("     · Capacidad: {0}", ByteConversions.ConvertBytesToString((ulong)logicalDrive.TotalSize, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("     · Porcentaje de espacio ocupado: {0:F2} %", usedSpace * 100d / logicalDrive.TotalSize).AppendLine();
                    output.AppendFormat("     · Espacio en uso: {0}", ByteConversions.ConvertBytesToString((ulong)usedSpace, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("     · Espacio disponible: {0}", ByteConversions.ConvertBytesToString((ulong)logicalDrive.AvailableFreeSpace, Unit.BinaryByte)).AppendLine();
                }

                #endregion

                #region Drive Geometry Info

                output.Append(" · Geometría de la unidad:").AppendLine();
                if (drive.Geometry.BytesPerSector.HasValue)
                    output.AppendFormat("   · Bytes por sector: {0}", drive.Geometry.BytesPerSector.Value).AppendLine();
                if (drive.Geometry.Cylinders.HasValue)
                    output.AppendFormat("   · Cilindros: {0}", drive.Geometry.Cylinders.Value).AppendLine();
                if (drive.Geometry.Heads.HasValue)
                    output.AppendFormat("   · Cabezales: {0}", drive.Geometry.Heads.Value).AppendLine();
                if (drive.Geometry.Sectors.HasValue)
                    output.AppendFormat("   · Sectores: {0}", drive.Geometry.Sectors.Value).AppendLine();
                if (drive.Geometry.SectorsPerTrack.HasValue)
                    output.AppendFormat("   · Sectores por pista: {0}", drive.Geometry.SectorsPerTrack.Value).AppendLine();
                if (drive.Geometry.Tracks.HasValue)
                    output.AppendFormat("   · Pistas: {0}", drive.Geometry.Tracks.Value).AppendLine();
                if (drive.Geometry.TracksPerCylinder.HasValue)
                    output.AppendFormat("   · Pistas por cilindro: {0}", drive.Geometry.TracksPerCylinder.Value).AppendLine();

                #endregion

                if (drive.TotalActivityPercentage.HasValue)
                    output.AppendFormat(" · Porcentaje de actividad: {0:F2} %", drive.TotalActivityPercentage.Value).AppendLine();

                if (drive.TotalReadActivityPercentage.HasValue)
                    output.AppendFormat(" · Porcentaje de actividad de lectura: {0:F2} %", drive.TotalReadActivityPercentage.Value).AppendLine();

                if (drive.TotalWriteActivityPercentage.HasValue)
                    output.AppendFormat(" · Porcentaje de actividad de escritura: {0:F2} %", drive.TotalWriteActivityPercentage.Value).AppendLine();

                if (drive.ReadSpeed.HasValue)
                    output.AppendFormat(" · Velocidad de lectura: {0}", ByteConversions.ConvertBytesToString((ulong)drive.ReadSpeed.Value, Unit.BytesPerSecond)).AppendLine();

                if (drive.WriteSpeed.HasValue)
                    output.AppendFormat(" · Velocidad de escritura: {0}", ByteConversions.ConvertBytesToString((ulong)drive.WriteSpeed.Value, Unit.BytesPerSecond)).AppendLine();

                if (drive.AverageResponseTimePerTransfer.HasValue)
                    output.AppendFormat(" · Tiempo promedio de respuesta por transferencia: {0}", SecondsToString.Convert(drive.AverageResponseTimePerTransfer.Value)).AppendLine();
                
                if (drive.AverageResponseTimePerRead.HasValue)
                    output.AppendFormat(" · Tiempo promedio de respuesta por lectura: {0}", SecondsToString.Convert(drive.AverageResponseTimePerRead.Value)).AppendLine();
                
                if (drive.AverageResponseTimePerWrite.HasValue)
                    output.AppendFormat(" · Tiempo promedio de respuesta por escritura: {0}", SecondsToString.Convert(drive.AverageResponseTimePerWrite.Value)).AppendLine();

                output.AppendFormat(" · ¿Es almacenamiento ATA?: {0}", drive.IsATADrive).AppendLine();

                #region ATA Information

                if (drive.IsATADrive)
                {
                    output.Append(" · Información específica de unidades ATA:").AppendLine();
                    ATADrive ataDrive = (ATADrive)drive;

                    output.AppendFormat("   · Estado de salud de la unidad: {0}", ataDrive.DriveHealth).AppendLine();

                    if (ataDrive.PowerOnTime.HasValue)
                    {
                        output.AppendFormat("   · Tiempo encendido: {0:F1} horas", ataDrive.PowerOnTime.Value.TotalHours).AppendLine();
                    }
                    if (ataDrive.PowerCycleCount.HasValue)
                    {
                        output.AppendFormat("   · Número de ciclos de encendido: {0} veces", ataDrive.PowerCycleCount.Value).AppendLine();
                    }
                    if (ataDrive.Temperature.HasValue)
                    {
                        output.AppendFormat("   · Temperatura: {0:F2} °C", ataDrive.Temperature.Value).AppendLine();
                    }

                    output.Append("   · Información del S.M.A.R.T.:").AppendLine();
                    output.Append("     ——————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————").AppendLine();
                    output.Append("     Índice   | Identificador   | Nombre del atributo                               | Valor actual   | Peor valor   | Umbral   | Valor convertido   | Valor en bruto   ").AppendLine();
                    output.Append("     ——————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————").AppendLine();
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
                        output.AppendFormat("    | {0,-5}", sensor.Threshold);
                        output.AppendFormat("    | {0,-15}", sensor.Value);
                        output.AppendFormat("    | {0}", ConvertBytesToHexString(sensor.RawValue));

                        output.AppendLine("\n     ——————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————————");
                        count++;
                    }
                }

                #endregion

                #region NVMe Information

                if (drive.Type == PhysicalDriveType.NVMe)
                {
                    output.Append(" · Información específica de unidades NVM Express (NVMe):").AppendLine();
                    NVMeDrive nvmeDrive = (NVMeDrive)drive;

                    output.AppendFormat("   · Capacidad NVM total: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.TotalNVMCapacity, Unit.BinaryByte)).AppendLine();
                    output.AppendFormat("   · Capacidad NVM sin utilizar: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.UnallocatedNVMCapacity, Unit.BinaryByte)).AppendLine();

                    output.Append("   · Advertencias críticas:").AppendLine();
                    NVMeCriticalWarning[] warnings = nvmeDrive.CriticalWarnings;
                    if (warnings.Length > 0)
                    {
                        foreach (NVMeCriticalWarning warning in nvmeDrive.CriticalWarnings)
                        {
                            string criticalWarningDesc = "";
                            switch (warning)
                            {
                                case NVMeCriticalWarning.AvailableSpaceLow:
                                    criticalWarningDesc = "El espacio disponible está por debajo del umbral";
                                    break;
                                case NVMeCriticalWarning.TemperatureThreshold:
                                    criticalWarningDesc = "La temperatura de la unidad está fuera del rango seguro";
                                    break;
                                case NVMeCriticalWarning.ReliabilityDegraded:
                                    criticalWarningDesc = "La confiabilidad de la unidad es baja. Puede deberse a una gran cantidad de errores de medio o cualquier error interno que afecte a la confiabilidad de la unidad.";
                                    break;
                                case NVMeCriticalWarning.ReadOnly:
                                    criticalWarningDesc = "La unidad está en modo de solo lectura";
                                    break;
                                case NVMeCriticalWarning.VolatileMemoryBackupDeviceFailed:
                                    criticalWarningDesc = "La memoria volátil de recuperación falló";
                                    break;
                            }
                            output.AppendFormat("     · {0}", criticalWarningDesc).AppendLine();
                        }
                    }
                    else
                    {
                        output.Append("     · No hay advertencias para mostrar").AppendLine();
                    }

                    if (nvmeDrive.Temperature.HasValue)
                        output.AppendFormat("   ·  Temperatura de la unidad: {0} °C", nvmeDrive.Temperature.Value).AppendLine();

                    output.AppendFormat("   ·  Sensores de temperatura:").AppendLine();
                    count = 1;
                    foreach (double tempSensor in nvmeDrive.TemperatureSensors)
                    {
                        output.AppendFormat("     ·  Sensor N.º {0}: {1:F2} °C", count, tempSensor).AppendLine();
                        count++;
                    }

                    if (nvmeDrive.AvailableSpare.HasValue)
                        output.AppendFormat("   · Espacio de respuesto disponible: {0}", ByteConversions.ConvertBytesToString((ulong)nvmeDrive.AvailableSpare.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.AvailableSpareThreshold.HasValue)
                        output.AppendFormat("   · Umbral de espacio de respuesto disponible: {0}", ByteConversions.ConvertBytesToString((ulong)nvmeDrive.AvailableSpareThreshold.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.PercentageUsed.HasValue)
                        output.AppendFormat("   · Porcentaje de utilización: {0} %", nvmeDrive.DataRead.Value).AppendLine();
                    if (nvmeDrive.DataRead.HasValue)
                        output.AppendFormat("   · Datos leídos: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.DataRead.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.DataWritten.HasValue)
                        output.AppendFormat("   · Datos escritos: {0}", ByteConversions.ConvertBytesToString(nvmeDrive.DataWritten.Value, Unit.BinaryByte)).AppendLine();
                    if (nvmeDrive.HostReadCommands.HasValue)
                        output.AppendFormat("   · Comandos de lectura del host: {0}", nvmeDrive.HostReadCommands.Value).AppendLine();
                    if (nvmeDrive.HostWriteCommands.HasValue)
                        output.AppendFormat("   · Comandos de escritura del host: {0}", nvmeDrive.HostWriteCommands.Value).AppendLine();
                    if (nvmeDrive.ControllerBusyTime.HasValue)
                        output.AppendFormat("   · Tiempo del controlador ocupado: {0}", nvmeDrive.ControllerBusyTime.Value).AppendLine();
                    if (nvmeDrive.PowerCycles.HasValue)
                        output.AppendFormat("   · Número de ciclos de encendido: {0} veces", nvmeDrive.PowerCycles.Value).AppendLine();
                    if (nvmeDrive.PowerOnTime.HasValue)
                        output.AppendFormat("   · Tiempo encendido: {0:g}", nvmeDrive.PowerOnTime.Value).AppendLine();
                    if (nvmeDrive.UnsafeShutdowns.HasValue)
                        output.AppendFormat("   · Número de apagados inseguros de la unidad: {0}", nvmeDrive.UnsafeShutdowns.Value).AppendLine();
                    if (nvmeDrive.MediaErrors.HasValue)
                        output.AppendFormat("   · Errores del medio: {0}", nvmeDrive.MediaErrors.Value).AppendLine();
                    if (nvmeDrive.ErrorInfoLogCount.HasValue)
                        output.AppendFormat("   · Conteo de registros de información de errores: {0}", nvmeDrive.ErrorInfoLogCount.Value).AppendLine();
                    if (nvmeDrive.WarningCompositeTemperatureTime.HasValue)
                        output.AppendFormat("   · Duración de advertencia de temperatura compuesta: {0}", nvmeDrive.WarningCompositeTemperatureTime.Value).AppendLine();
                    if (nvmeDrive.CriticalCompositeTemperatureTime.HasValue)
                        output.AppendFormat("   · Duración de advertencia crítica de temperatura compuesta: {0}", nvmeDrive.CriticalCompositeTemperatureTime.Value).AppendLine();

                    output.AppendFormat("   · Id. de fabricante de PCI: {0} (0x{0:X})", nvmeDrive.PCIVendorID).AppendLine();
                    output.AppendFormat("   · Id. de fabricante del subsistema PCI: {0} (0x{0:X})", nvmeDrive.PCISubsystemVendorID).AppendLine();
                    output.AppendFormat("   · Identificador IEEE OUI: {0}", nvmeDrive.IEEEOuiIdentifier).AppendLine();
                    output.AppendFormat("   · Id. del controlador: {0}", nvmeDrive.ControllerID).AppendLine();
                }

                #endregion
            }
            StorageDrives.DisposeDrives();

            #endregion

            Console.WriteLine(output);
            Console.Write("Presione cualquier tecla para salir...");
            Console.ReadKey();
            LibrarySettings.Close();
        }
    }
}
