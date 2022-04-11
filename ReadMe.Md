# EMNSystemInfo.HardwareAPI

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
  - iTE IT87xx (Super I/O)
  - Nuvoton NCT67xx (Super I/O)
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

## How does it work?
The main component is the LibrarySettings class, whose task is provide library configuration options, like the kernel driver name, and provide the Initialize & Close methods that prepare the library to use its components and free resources when it's no longer needed, respectively.<br/>This library has static classes that represent a hardware group, some of them have requirements about library initialization and execution with admin privileges.<br/>This table shows the hardware group classes and its requirements:

| Class name| Requires library initialization? | Requires admin privileges? |
| --- | --- | --- |
| Batteries| No | No |
| Processors| Yes | Yes |
| GPUs| No | Yes |
| LPCChips| Yes | Yes |
| StorageDrives| No | Yes |

These hardware group classes have a property named "List" that contains classes that represent each installed hardware element, and methods to load and delete them from the mentioned property.<br/>Finally, these classes that represent a hardware element contain properties about its information and sensor values. Some of these classes instances provide more detailed-and-specific information for a hardware. To access it, you have to check the hardware type the instance is representing, with the "Type" property. Then, you have to convert that instance into the one denoted by the "Type" property.

## Why this library installs a kernel driver?
Because the majority of hardware information, like CPU info or LPC chips info, is obtained through the execution of instructions not available for a normal app. Also, it communicates with hardware through system buses. Those functions are only available for kernel drivers, that's the reason it is necessary to install one. All programs that show detailed info about hardware and sensors install one too. In the case of this library, you decide when to install or uninstall the driver, with the methods Initialize & Close from LibrarySettings class.

## Example for .NET 6:
Get info about installed CPUs (admin privileges are required)
``` c#
using EMNSystemInfo.HardwareAPI.CPU;

// Set name and display name for the kernel driver
LibrarySettings.KernelDriverName = "CPUInfoExampleKrnlDrv";
LibrarySettings.KernelDriverDisplayName = "CPU Info Example Kernel Driver";
LibrarySettings.Initialize(); // It's necessary to initialize the library to get CPU info

Processors.LoadProcessors(); // Load CPUs into the List property
foreach (Processor p in Processors.List)
{
    Console.WriteLine("Processor #{0}", p.Index + 1);
    Console.WriteLine(" · Name: {0}", p.BrandString);
    Console.WriteLine(" · Short Name: {0}", p.Name);
    Console.WriteLine(" · Manufacturer: {0}", p.Vendor);
    Console.WriteLine(" · Load: {0:F2} %", p.TotalLoad);
}
Processors.DisposeAllProcessors();

LibrarySettings.Close();
```
Check out the EMNSystemInfo.HardwareAPITest folder for a complete example with all the hardware supported by this library.

## License
This library is licensed under the Mozilla Public License (MPL) 2.0, you can use it on private and commercial projects, but you must include a copy of this license in your project.

## Name meaning and, why I've made this library?
The name comes from a personal app about system info I'm developing as a hobby. The letters EMN are my initials.<br/>
I've made this library because the way of getting hardware info Libre Hardware Monitor offers didn't convince me. I wanted to access hardware info directly with one or more properties instead of searching it in a sensor list. That's why I've decided to fork Libre Hardware Monitor, replacing the model it offered with the one I'm proposing here.
