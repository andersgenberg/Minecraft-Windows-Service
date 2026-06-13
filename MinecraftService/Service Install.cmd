sc.exe create "Minecraft Service" binpath= "%~dp0MinecraftService.exe" start= auto

rem sc.exe failure "Minecraft Service" reset= 0 actions= restart/600000
set /p DUMMY=Hit ENTER to continue...

