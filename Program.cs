using System;
using System.Diagnostics;
using System.Security;
using System.Text;

namespace RunAs
{
    class Program
    {
        static void Main(string[] args)
        {
            // Check if command was provided
            if (args.Length == 0)
            {
                ShowUsage();
                Environment.Exit(1);
                return;
            }

            // Get the command and arguments
            string command = args[0];
            string arguments = args.Length > 1 ? BuildArgumentsString(args, 1) : string.Empty;

            try
            {
                // Prompt for credentials
                Console.Write("Enter username (DOMAIN\\username or username): ");
                string username = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Username cannot be empty.");
                    Environment.Exit(1);
                    return;
                }

                Console.Write("Enter password: ");
                SecureString password = GetSecurePassword();

                if (password.Length == 0)
                {
                    Console.WriteLine("Password cannot be empty.");
                    Environment.Exit(1);
                    return;
                }

                // Run the command with the provided credentials
                RunCommandAsUser(command, arguments, username, password);

                // Clear the password from memory
                password.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static SecureString GetSecurePassword()
        {
            SecureString password = new SecureString();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                // Ignore Backspace and Delete
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Delete && key.Key != ConsoleKey.Enter)
                {
                    password.AppendChar(key.KeyChar);
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.RemoveAt(password.Length - 1);
                    Console.Write("\b \b");
                }
            }
            while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return password;
        }

        static void RunCommandAsUser(string command, string arguments, string username, SecureString password)
        {
            // Extract domain and username
            string domain = ExtractDomain(username);
            string userOnly = ExtractUsername(username);

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = false,
                UserName = userOnly,
                Domain = domain,
                Password = password,
                LoadUserProfile = true
            };

            Console.WriteLine($"\nRunning '{command} {arguments}' as {username}...\n");

            using (Process process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start process. Check credentials and permissions.");
                }

                // Read output
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.WriteLine(e.Data);
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Console.Error.WriteLine(e.Data);
                    }
                };

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                process.WaitForExit();

                Console.WriteLine($"\nProcess exited with code: {process.ExitCode}");
                Environment.Exit(process.ExitCode);
            }
        }

        static string ExtractDomain(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return string.Empty;

            int backslashIndex = username.IndexOf('\\');
            if (backslashIndex > 0)
            {
                return username.Substring(0, backslashIndex);
            }

            // If no domain specified, return empty string (local machine or default domain)
            return string.Empty;
        }

        static string ExtractUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
                return username;

            int backslashIndex = username.IndexOf('\\');
            if (backslashIndex >= 0 && backslashIndex < username.Length - 1)
            {
                return username.Substring(backslashIndex + 1);
            }

            return username;
        }

        static string BuildArgumentsString(string[] args, int startIndex)
        {
            // Build argument string, preserving quotes for arguments that need them
            StringBuilder argBuilder = new StringBuilder();
            for (int i = startIndex; i < args.Length; i++)
            {
                if (i > startIndex)
                    argBuilder.Append(" ");

                // If argument contains spaces, wrap it in quotes
                if (args[i].Contains(" "))
                {
                    argBuilder.Append("\"");
                    argBuilder.Append(args[i]);
                    argBuilder.Append("\"");
                }
                else
                {
                    argBuilder.Append(args[i]);
                }
            }
            return argBuilder.ToString();
        }

        static void ShowUsage()
        {
            Console.WriteLine("RunAsCmd - Execute Commands as Another User");
            Console.WriteLine();
            Console.WriteLine("Usage: RunAsCmd.exe <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RunAsCmd.exe cmd.exe /c dir");
            Console.WriteLine("  RunAsCmd.exe powershell.exe -Command \"Get-Process\"");
            Console.WriteLine();
            Console.WriteLine("Kill a process on a remote computer using psexec:");
            Console.WriteLine("  RunAsCmd.exe psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /IM notepad.exe");
            Console.WriteLine();
            Console.WriteLine("Kill a process by PID on remote computer:");
            Console.WriteLine("  RunAsCmd.exe psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /PID 1234");
            Console.WriteLine();
            Console.WriteLine("Kill multiple processes on remote computer:");
            Console.WriteLine("  RunAsCmd.exe psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /IM notepad.exe /IM calc.exe");
        }
    }
}

