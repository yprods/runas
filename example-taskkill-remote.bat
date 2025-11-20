@echo off
REM Example: Kill notepad.exe on remote computer using psexec
REM Replace SERVER01 with your remote computer name
REM Replace DOMAIN\User with your domain and username

RunAsCmd.exe psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe

REM Other examples (uncomment to use):
REM Kill process by PID:
REM RunAsCmd.exe psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /PID 1234

REM Kill multiple processes:
REM RunAsCmd.exe psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe /IM calc.exe

pause

