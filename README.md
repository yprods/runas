# RunAsCmd - Execute Commands as Another User

A .NET Framework C# console application that runs a command as another user using provided credentials. Perfect for running commands like `psexec` with `taskkill` on remote computers.

## Requirements

- .NET Framework 4.8 or later
- Windows OS
- Appropriate permissions to run processes as another user
- For remote operations: `psexec.exe` from Sysinternals (if using psexec)

## Building

```bash
dotnet build
```

The executable will be created at: `bin\Debug\net48\RunAsCmd.exe`

## Usage

```bash
RunAsCmd.exe [-u username] [-o outputfile] <command> [arguments]
```

### Options:
- `-u`, `-user`, `/u`, `/u:username` - Username to run command as (DOMAIN\user or user)
  - If omitted, you will be prompted for username
- `-o`, `--output`, `/o`, `/o:file` - Output file path (writes output to file instead of console)
- Password is always prompted (for security)

### Examples:

**With username in command line (recommended):**
```bash
RunAsCmd.exe -u DOMAIN\Admin cmd.exe /c dir
RunAsCmd.exe /u:DOMAIN\Admin powershell.exe -Command "Get-Process"
```

**Write output to file:**
```bash
RunAsCmd.exe -u DOMAIN\Admin -o output.txt cmd.exe /c dir
RunAsCmd.exe -u DOMAIN\Admin /o:C:\logs\result.txt cmd.exe /c dir
```

**Without username (will prompt):**
```bash
RunAsCmd.exe cmd.exe /c dir
```

## Examples

### Basic Command Execution

```bash
# Run a simple command
RunAsCmd.exe cmd.exe /c dir

# Run PowerShell command
RunAsCmd.exe powershell.exe -Command "Get-Process"
```

### Kill Process on Remote Computer using psexec

**Prerequisites:** You need `psexec.exe` from Microsoft Sysinternals installed and accessible in PATH.

**Kill a process by name (with username):**
```bash
RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\RemotePC -u DOMAIN\User taskkill /F /IM notepad.exe
```

**Kill a process and write output to file:**
```bash
RunAsCmd.exe -u DOMAIN\Admin -o kill_result.txt psexec.exe \\RemotePC -u DOMAIN\User taskkill /F /IM notepad.exe
```

**Kill a process by PID:**
```bash
RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\RemotePC -u DOMAIN\User taskkill /F /PID 1234
```

**Kill multiple processes:**
```bash
RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\RemotePC -u DOMAIN\User taskkill /F /IM notepad.exe /IM calc.exe
```

**Kill all instances of a process on remote computer:**
```bash
RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\RemotePC -u DOMAIN\User taskkill /F /IM chrome.exe /T
```

### Complete Workflow Example

1. You want to kill `notepad.exe` on a remote computer `SERVER01` 
2. You have admin credentials: `DOMAIN\Admin`
3. You run:
   ```
   RunAsCmd.exe -u DOMAIN\Admin psexec.exe \\SERVER01 -u DOMAIN\User taskkill /F /IM notepad.exe
   ```
4. You'll only be prompted for password (username is already provided)
5. Enter password: `********`
6. The command executes and shows the output

### Notes on psexec Usage

- The credentials you enter in `RunAsCmd` are used to run `psexec.exe` itself
- The `-u` flag in psexec specifies the user for remote operations on the target computer
- Ensure you have:
  - Network access to the remote computer
  - Administrative privileges
  - Remote access enabled on the target computer
  - Firewall rules allowing remote execution

## How It Works

1. The application accepts a command and optional arguments from the command line
2. Prompts for username (supports `DOMAIN\username` or just `username`)
3. Prompts for password (input is secure and masked with `*` characters)
4. Executes the command using `ProcessStartInfo` with the provided credentials
5. Displays the output and exit code

## Security Notes

- The password input is secure and masked on screen
- Passwords are stored in `SecureString` for better security
- The application requires appropriate permissions to create processes as another user
- Domain can be specified as part of the username (e.g., `DOMAIN\username`) or left blank for local accounts
