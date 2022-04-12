# EMNSystemInfo.HardwareAPI

[![GitHub license](https://img.shields.io/github/license/EMN-CSharp/EMNSystemInfo.HardwareAPI)](https://github.com/EMN-CSharp/EMNSystemInfo.HardwareAPI/blob/master/LICENSE) [![Nuget](https://img.shields.io/nuget/v/EMNSystemInfo.HardwareAPI)](https://www.nuget.org/packages/EMNSystemInfo.HardwareAPI/)

EMNSystemInfo.HardwareAPI es una biblioteca de hardware para Windows basada en [Libre Hardware Monitor](https://github.com/LibreHardwareMonitor/LibreHardwareMonitor), diseñada para la plataforma .NET. Provee un modelo base para acceder la información del hardware, en el cual puedes construir otros modelos como el de "vista de árbol" que ofrece Libre Hardware Monitor. Este modelo que propongo aquí consiste en listas de clases que representan un hardware en específico. Estas clases contienen las propiedades del hardware que representan y un método para actualizarlas.

## Soporte de hardware
- Baterías de laptops

- Procesadores:
  - Intel
  - AMD

- Tarjetas gráficas:
  - AMD
  - NVIDIA
  - Gráficos integrados Intel

- Chips LPC (las "x" representan cualquier número o caracter que contenga el modelo del chip):
  - ASUS ATK0110 (Super I/O)
  - Fintek F718xx (Super I/O)
  - iTE IT8xx (Super I/O)
  - Nuvoton NCT6xx (Super I/O)
  - Winbond W836xx (Super I/O)
  - Embedded Controller (Controlador embebido)

- Unidades de almacenamiento físico:
  - Todos los tipos, aunque solo se puede acceder a su información genérica.
    Para acceder a información más específica se soportan las siguientes unidades:
  - Cualquier unidad con acceso al S.M.A.R.T.
  - Unidades HDD
  - Unidades NVMe
  - SSDs Indilinx
  - SSDs Intel
  - SSDs Micron
  - SSDs Plextor
  - SSDs Samsung
  - SSDs Sandforce

## ¿De dónde lo puedo descargar?
Puedes decargar la última versión desde NuGet [aquí](https://www.nuget.org/packages/EMNSystemInfo.HardwareAPI/).
Alternatiamente, puedes descargar el paquete NuGet desde la página [Releases](https://github.com/EMN-CSharp/EMNSystemInfo.HardwareAPI/releases).

## ¿Cómo funciona?
Como principal componente, está la clase LibrarySettings, encargada de realizar la configuración necesaria para utilizar la bibiloteca, como el nombre del controlador en modo de kernel que se va a instalar y proveer los métodos Initialize y Close que preparan la biblioteca para su uso y liberan los recursos una vez utilizada, respectivamente.<br/>Esta biblioteca tiene clases estáticas que representan un grupo de hardware, algunas de ellas tienen requerimientos sobre la inicialización de la bibiloteca y su ejecución con permisos de administrador.<br/>La siguiente tabla muestra las clases de grupos de hardware y sus requerimientos:

| Nombre de clase| ¿Requiere inicializar la biblioteca? | ¿Requiere permisos de administrador? |
| --- | --- | --- |
| Batteries| No | No |
| Processors| Sí | Sí |
| GPUs| No | Sí |
| LPCChips| Sí | Sí |
| StorageDrives| No | Sí |

Estas clases de grupos de hardware contienen una propiedad llamada List que contiene clases que representan cada elemento de hardware instalado, y métodos para cargarlos y eliminarlos de la propiedad mencionada.<br/>Finalmente, éstas clases que representan un elemento de hardware contienen propiedades que representan su información y valores de sensores, además de un método Update para actualizarlas. Algunas de estas instancias de clases ofrecen información más detallada y específica para un hardware. Para acceder a ella, hay que verificar el tipo de hardware que representa la instancia, indicado con la propiedad Type. Después, hay que convertir la instancia en la instancia indicada por la propiedad Type.

## ¿Por qué esta biblioteca instala un controlador en modo de kernel?
Debido a que la mayoría de información del hardware, como la de los procesadores o los chips LPC, se obtiene mediante la ejecución de instrucciones no disponibles para una aplicación normal, además de comunicarse con el hardware mediante los buses del sistema. Todas estas funciones están disponibles para los controladores y por eso, es necesario instalar uno. Todos los programas que dan información detallada del hardware y sus sensores instalan uno también. En el caso de esta biblioteca, tú decides cuando instalar o desinstalar el controlador, con los métodos Initialize y Close de la clase LibrarySettings.

## Ejemplo para .NET 6:
Obtener info. de los procesadores instalados (requiere permisos de administrador)
``` c#
using EMNSystemInfo.HardwareAPI;
using EMNSystemInfo.HardwareAPI.CPU;

// Asignar el nombre y nombre para mostrar del controlador en modo de kernel
LibrarySettings.KernelDriverName = "CPUInfoExampleKrnlDrv";
LibrarySettings.KernelDriverDisplayName = "Controlador del kernel para el ejemplo de info. de CPU";
LibrarySettings.Initialize(); // Es necesario inicializar la biblioteca para obtener info. de las CPU

Processors.LoadProcessors(); // Cargar las CPU a la propiedad List
foreach (Processor p in Processors.List)
{
    Console.WriteLine("Procesador N.º {0}", p.Index + 1);
    Console.WriteLine(" · Nombre: {0}", p.BrandString);
    Console.WriteLine(" · Nombre resumido: {0}", p.Name);
    Console.WriteLine(" · Fabricante: {0}", p.Vendor);
    Console.WriteLine(" · Utilización: {0:F2} %", p.TotalLoad);
}
Processors.DisposeAllProcessors();

LibrarySettings.Close();
```
Para ver un ejemplo completo para todos los componentes de hardware que soporta esta biblioteca, ve al directorio EMNSystemInfo.HardwareAPITest.

## Licencia
Esta biblioteca está disponible bajo la Licencia Pública de Mozilla (MPL) 2.0. Puedes utilizarla en proyectos privados y comerciales, pero debes incluir una copia de esta licencia en tu proyecto.

## Significado del nombre y ¿por qué decidí hacer esta biblioteca?
El nombre de esta biblioteca deriva del nombre de una aplicación personal de información del sistema que desarrollo como hobby. Las letras EMN son mis iniciales.
Decidí hacer esta biblioteca ya que no estaba conforme con la forma de obtener información que me otorgaba Libre Hardware Monitor, me restringía a un modelo de "vista de árbol" y para buscar un sensor específico debía buscarlo con su nombre en la lista de sensores. A pesar de que no es un mal método para acceder a la información, yo quería acceder a ella directamente con una o varias propiedades, por eso decidí crear un fork de Libre Hardware Monitor, cambiando el modelo que ofrecía para acceder a la información por el que propongo aquí.
