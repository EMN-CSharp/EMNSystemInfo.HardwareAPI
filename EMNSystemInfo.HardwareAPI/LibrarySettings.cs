using EMNSystemInfo.HardwareAPI.CPU;
using System;
using System.Security.Principal;

namespace EMNSystemInfo.HardwareAPI
{
    public static class LibrarySettings
    {
        private static string _krnldrvName;

        /// <summary>
        /// Gets the value that represents if the library is initialized
        /// </summary>
        public static bool IsInitialized { get; private set; } = false;

        /// <summary>
        /// Gets the value that represents if user is an administrator
        /// </summary>
        public static bool UserIsAdmin
        {
            get
            {
                WindowsPrincipal adminCheck = new WindowsPrincipal(WindowsIdentity.GetCurrent());
                return adminCheck.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Gets or sets the kernel driver name. When necessary, the set value will be validated.
        /// </summary>
        public static string KernelDriverName
        {
            get => _krnldrvName;
            set
            {
                _krnldrvName = value.Replace(" ", string.Empty).Replace(".", "_");
            }
        }

        /// <summary>
        /// Gets or sets the kernel driver display name.
        /// </summary>
        public static string KernelDriverDisplayName { get; set; }

        /// <summary>
        /// Initializes the library. Installs the kernel driver used to get hardware info, with <see cref="KernelDriverName"/> and <see cref="KernelDriverDisplayName"/> as the driver name and display name respectively.
        /// </summary>
        public static void Initialize()
        {
            if (!IsInitialized)
            {
                if (!UserIsAdmin)
                {
                    throw new UnauthorizedAccessException("Program requires to be executing with administrator rights");
                }
                else if (string.IsNullOrWhiteSpace(KernelDriverName))
                {
                    throw new FormatException($"{nameof(KernelDriverName)} cannot be null, empty, or just white spaces");
                }
                else
                {
                    Ring0.Open();
                    OpCode.Open();
                    IsInitialized = true;
                    Processors.LoadProcessors();
                }
            }
        }

        public static string GetRing0Report()
        {
            return Ring0.GetReport();
        }

        /// <summary>
        /// Frees the resources used in the library. Uninstalls the kernel driver.
        /// </summary>
        public static void Close()
        {
            if (IsInitialized)
            {
                Ring0.Close();
                OpCode.Close();
                IsInitialized = false;
            }
        }
    }
}
