# EMNSystemInfo.HardwareAPI

[![GitHub license](https://img.shields.io/github/license/EMN-CSharp/EMNSystemInfo.HardwareAPI)](https://github.com/EMN-CSharp/EMNSystemInfo.HardwareAPI/blob/master/LICENSE) [![Nuget](https://img.shields.io/nuget/v/EMNSystemInfo.HardwareAPI)](https://www.nuget.org/packages/EMNSystemInfo.HardwareAPI/)

EMNSystemInfo.HardwareAPI is a hardware library for Windows based on [Libre Hardware Monitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), designed for the .NET platform. It provides a base model to access hardware information, in which you can build other models like "tree view", which Libre Hardware Monitor offers. The model I'm proposing here consists on lists of classes that represent a specific hardware. These classes contain the properties of the hardware they are representing and a method to update them.

## Hardware support
- Laptop batteries

- Processors:
  - Intel
  - AMD

- Video cards:
  - AMD
  - NVIDIA
  - Intel integrated graphics

- LPC chips (the "x"s represent any number or character the chip model could contain):
  - ASUS ATK0110 (Super I/O)
  - Fintek F718xx (Super I/O)
  - iTE IT8xx (Super I/O)
  - Nuvoton NCT6xx (Super I/O)
  - Winbond W836xx (Super I/O)
  - Embedded Controllers

- Physical storage:
  - All types. Though only its generic information can be accessed.
    To access more specific info, these drives are supported:
  - Any drive with S.M.A.R.T. access
  - HDDs
  - NVMe drives
  - SSDs Indilinx
  - SSDs Intel
  - SSDs Micron
  - SSDs Plextor
  - SSDs Samsung
  - SSDs Sandforce

- Coolers:
  - AeroCool P7-H1
  - AquaComputer (Aquastream XT, D5Next, MPS) (water cooling)
  - Heatmaster (water cooling)
  - NZXT Kraken X3 (water cooling)

## Where can I download it?
You can download the latest version from NuGet [here](https://www.nuget.org/packages/EMNSystemInfo.HardwareAPI/).
Alternatively, you can download the NuGet package from [Releases](https://github.com/EMN-CSharp/EMNSystemInfo.HardwareAPI/releases) page.

## How does it work?
The main component is the LibrarySettings class, whose task is provide library configuration options, like the kernel driver name, and provide the Initialize & Close methods that prepare the library to use its components and free resources when it's no longer needed, respectively.<br/>This library has static classes that represent a hardware group, some of them have requirements about library initialization and execution with admin privileges.<br/>This table shows the hardware group classes and its requirements:

| Class name| Requires library initialization? | Requires admin privileges? |
| --- | --- | --- |
| Batteries| No | No |
| Processors| Yes | No* |
| GPUs| No | Yes |
| LPCChips| Yes | Yes |
| StorageDrives| No | No** |
| Coolers| No | Yes (probably) |

**: But, if the library is not executing with admin privileges, only generic storage info can be accessed, regardless of the drive type.<br/>
*: It's the same case as StorageDrives. No admin privileges, only generic CPU info can be accessed, regardless of the type.

These hardware group classes have a property named "List" that contains classes that represent each installed hardware element, and methods to load and delete them from the mentioned property.<br/>Finally, these classes that represent a hardware element contain properties about its information and sensor values. Some of these classes instances provide more detailed-and-specific information for a hardware. To access it, you have to check the hardware type the instance is representing, with the "Type" property. Then, you have to convert that instance into the one denoted by the "Type" property.

## Why this library installs a kernel driver?
Because the majority of hardware information, like CPU info or LPC chips info, is obtained through the execution of instructions not available for a normal app. Also, it communicates with hardware through system buses. Those functions are only available for kernel drivers, that's the reason it is necessary to install one. All programs that show detailed info about hardware and sensors install one too. In the case of this library, you decide when to install or uninstall the driver, with the methods Initialize & Close from LibrarySettings class.

## Example for .NET 6:
Get info about installed CPUs
``` c#
using EMNSystemInfo.HardwareAPI;
using EMNSystemInfo.HardwareAPI.CPU;

LibrarySettings.Initialize(); // It's necessary to initialize the library to get generic CPU info

Processors.Load(); // Load CPUs into the List property
foreach (Processor p in Processors.List)
{
    p.Update();

    Console.WriteLine("Processor N.º {0}", p.Index + 1);
    Console.WriteLine(" · Brand String: {0}", p.BrandString);
    Console.WriteLine(" · Name: {0}", p.Name);
    Console.WriteLine(" · Manufacturer: {0}", p.Vendor);
    Console.WriteLine(" · Family: {0}", p.Family);
    Console.WriteLine(" · Model: {0}", p.Model);
    Console.WriteLine(" · Stepping: {0}", p.Stepping);
    Console.WriteLine(" · Core Count: {0}", p.CoreCount);
    Console.WriteLine(" · Thread Count: {0}", p.ThreadCount);
    Console.WriteLine(" · Total Load: {0:F2} %", p.TotalLoad);
}
Processors.Dispose();

LibrarySettings.Close();
```
Check out the EMNSystemInfo.HardwareAPITest folder for a complete example with all the hardware supported by this library.

## License
This library is licensed under the Mozilla Public License (MPL) 2.0, you can use it on private and commercial projects, but you must include a copy of this license in your project.

## Name meaning and, why I've made this library?
The name comes from a personal app about system info I'm developing as a hobby. The letters EMN are my initials.<br/>
I've made this library because the way of getting hardware info Libre Hardware Monitor offers didn't convince me. I wanted to access hardware info directly with one or more properties instead of searching it in a sensor list. That's why I've decided to fork Libre Hardware Monitor, replacing the model it offered with the one I'm proposing here.
