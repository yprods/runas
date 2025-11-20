@echo off
REM Example: Kill notepad.exe on remote computer using psexec
REM Replace SERVER01 with your remote computer name
REM Replace DOMAIN\Admin with your domain and admin username (for running psexec)
REM Replace DOMAIN\User with the user account for the remote computer

REM With username passed as argument (no username prompt):
RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe

REM Write output to file:
RunAsCmd.exe -u DOMAIN\Admin -o result.txt psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe

REM Other examples (uncomment to use):
REM Kill process by PID with output file:
REM RunAsCmd.exe -u DOMAIN\Admin -o kill_result.txt psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /PID 1234

REM Kill multiple processes with output file:
REM RunAsCmd.exe -u DOMAIN\Admin -o multi_kill.txt psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe /IM calc.exe

REM Alternative format with /u: and /o: prefix:
REM RunAsCmd.exe /u:DOMAIN\Admin /o:C:\logs\result.txt psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe

pause

