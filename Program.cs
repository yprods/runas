using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            string username = null;
            string outputFile = null;
            string command = null;
            string arguments = string.Empty;
            List<string> remainingArgs = new List<string>();

            // Parse arguments for username flag (-u, /u, -user) and output file (-o, /o, --output)
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                string argLower = arg.ToLower();
                
                if (argLower == "-u" || argLower == "-user" || argLower == "/u")
                {
                    // Username is the next argument
                    if (i + 1 < args.Length)
                    {
                        username = args[i + 1];
                        i++; // Skip the next argument
                        continue;
                    }
                }
                else if (argLower.StartsWith("/u:"))
                {
                    // Username is after /u:
                    username = arg.Substring(3);
                    continue;
                }
                else if (argLower == "-o" || argLower == "--output" || argLower == "/o")
                {
                    // Output file is the next argument
                    if (i + 1 < args.Length)
                    {
                        outputFile = args[i + 1];
                        i++; // Skip the next argument
                        continue;
                    }
                }
                else if (argLower.StartsWith("/o:") || argLower.StartsWith("-o:"))
                {
                    // Output file is after /o: or -o:
                    int colonIndex = arg.IndexOf(':');
                    if (colonIndex >= 0 && colonIndex < arg.Length - 1)
                    {
                        outputFile = arg.Substring(colonIndex + 1);
                    }
                    continue;
                }
                
                // This argument is not a flag, add to remaining args
                remainingArgs.Add(arg);
            }

            // Extract command from remaining arguments
            if (remainingArgs.Count > 0)
            {
                command = remainingArgs[0];
                if (remainingArgs.Count > 1)
                {
                    arguments = BuildArgumentsString(remainingArgs.ToArray(), 1);
                }
            }
            else if (args.Length > 0)
            {
                // Backward compatibility: no flags found, assume first arg is command
                command = args[0];
                if (args.Length > 1)
                {
                    arguments = BuildArgumentsString(args, 1);
                }
            }

            // Validate command was provided
            if (string.IsNullOrWhiteSpace(command))
            {
                Console.WriteLine("Error: Command not provided.");
                ShowUsage();
                Environment.Exit(1);
                return;
            }

            try
            {
                // Prompt for username if not provided
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.Write("Enter username (DOMAIN\\username or username): ");
                    username = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(username))
                    {
                        Console.WriteLine("Username cannot be empty.");
                        Environment.Exit(1);
                        return;
                    }
                }

                // Always prompt for password (for security)
                Console.Write("Enter password: ");
                SecureString password = GetSecurePassword();

                if (password.Length == 0)
                {
                    Console.WriteLine("Password cannot be empty.");
                    Environment.Exit(1);
                    return;
                }

                // Run the command with the provided credentials
                RunCommandAsUser(command, arguments, username, password, outputFile);

                // Clear the password from memory
                password.Dispose();
            }
            catch (Exception ex)
            {
                string errorMsg = $"Error: {ex.Message}";
                Console.WriteLine(errorMsg);
                
                if (!string.IsNullOrEmpty(outputFile))
                {
                    try
                    {
                        File.AppendAllText(outputFile, errorMsg + Environment.NewLine);
                    }
                    catch { }
                }
                
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

        static void RunCommandAsUser(string command, string arguments, string username, SecureString password, string outputFile)
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

            string statusMsg = $"\nRunning '{command} {arguments}' as {username}...\n";
            Console.WriteLine(statusMsg);

            StreamWriter fileWriter = null;
            if (!string.IsNullOrEmpty(outputFile))
            {
                try
                {
                    fileWriter = new StreamWriter(outputFile, false, Encoding.UTF8);
                    fileWriter.WriteLine(statusMsg);
                    fileWriter.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not open output file '{outputFile}': {ex.Message}");
                    Console.WriteLine("Output will be displayed on console only.");
                }
            }

            try
            {
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
                            if (fileWriter != null)
                            {
                                fileWriter.WriteLine(e.Data);
                                fileWriter.Flush();
                            }
                            else
                            {
                                Console.WriteLine(e.Data);
                            }
                        }
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                        {
                            if (fileWriter != null)
                            {
                                fileWriter.WriteLine("ERROR: " + e.Data);
                                fileWriter.Flush();
                            }
                            else
                            {
                                Console.Error.WriteLine(e.Data);
                            }
                        }
                    };

                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    process.WaitForExit();

                    string exitMsg = $"\nProcess exited with code: {process.ExitCode}";
                    
                    if (fileWriter != null)
                    {
                        fileWriter.WriteLine(exitMsg);
                        fileWriter.Flush();
                    }
                    else
                    {
                        Console.WriteLine(exitMsg);
                    }
                    
                    Environment.Exit(process.ExitCode);
                }
            }
            finally
            {
                if (fileWriter != null)
                {
                    fileWriter.Close();
                    Console.WriteLine($"Output written to: {outputFile}");
                }
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
            Console.WriteLine("Usage: RunAsCmd.exe [-u username] [-o outputfile] <command> [arguments]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -u, -user, /u, /u:username    Username to run command as (DOMAIN\\user or user)");
            Console.WriteLine("                                 If omitted, you will be prompted for username");
            Console.WriteLine("  -o, --output, /o, /o:file     Output file path (writes output to file instead of console)");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\User cmd.exe /c dir");
            Console.WriteLine("  RunAsCmd.exe /u:DOMAIN\\User powershell.exe -Command \"Get-Process\"");
            Console.WriteLine();
            Console.WriteLine("Write output to file:");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\User -o output.txt cmd.exe /c dir");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\User /o:C:\\logs\\result.txt cmd.exe /c dir");
            Console.WriteLine();
            Console.WriteLine("Kill a process on a remote computer using psexec:");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\Admin -o result.txt psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /IM notepad.exe");
            Console.WriteLine();
            Console.WriteLine("Kill a process by PID on remote computer:");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\Admin psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /PID 1234");
            Console.WriteLine();
            Console.WriteLine("Kill multiple processes on remote computer:");
            Console.WriteLine("  RunAsCmd.exe -u DOMAIN\\Admin psexec.exe \\\\RemotePC -u DOMAIN\\User taskkill /F /IM notepad.exe /IM calc.exe");
        }
    }
}

