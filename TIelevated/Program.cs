using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using TIelevated.Common;

namespace TIelevated
{
    internal class Program
    {
        public static string? AppPath = Environment.ProcessPath;
        public static string? Version;
        public static string? Copyright;

        [STAThread]
        static void Main(string[] args)
        {
            var fvi = FileVersionInfo.GetVersionInfo(AppPath!);
            Version = fvi.FileVersion;
            Copyright = fvi.LegalCopyright;

            Output.WriteLine($"TIelevated [Version {Version}]");
            Output.WriteLine(Copyright!);
            //Output.WriteLine("Current AppPath: " + AppPath, Output.Style.Information);

            var uacFolder = Path.Combine(Path.GetTempPath(), "TIelevated");
            if (Directory.Exists(uacFolder))
            {
                var processes = Process.GetProcessesByName("msdt");
                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception ex)
                    {
                        Output.WriteLine($"Failed to kill msdt.exe: {ex.Message}", Output.Style.Warning);
                    }
                }

                // Remove folder from Path variable
                var currentPathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User);
                if (string.IsNullOrEmpty(currentPathVariable)) return;
                var pathEntries = currentPathVariable.Split([';'], StringSplitOptions.RemoveEmptyEntries);
                var newPathEntries = pathEntries.Where(p => !p.Equals(uacFolder, StringComparison.OrdinalIgnoreCase)).ToList();
                var newPathVariable = string.Join(";", newPathEntries);
                if (currentPathVariable != newPathVariable)
                {
                    Environment.SetEnvironmentVariable("Path", newPathVariable, EnvironmentVariableTarget.User);
                }
            }

            // Validate input
            if (args.Length > 0 && args[0].Equals("/uac", StringComparison.CurrentCultureIgnoreCase))
            {
                Output.WriteLine("Bypassing UAC prompt ...", Output.Style.Information);
                Output.WriteLine("Preparing environment ...", Output.Style.Information);
                UACBypass.Prepare(AppPath!);
                Output.WriteLine("Starting msdt.exe process ...", Output.Style.Information);
                UACBypass.RunPayload();
                Environment.Exit(0);
            }

            if (IsAdministrator() == false)
            {
                ElevateAsAdmin(AppPath!, string.Join(" ", args));
                Environment.Exit(740);
                return;
            }

            if (args.Length > 0 && args[0].Equals("/switchti", StringComparison.CurrentCultureIgnoreCase))
            {
                Output.WriteLine("Parsing command line ...", Output.Style.Information);
                ParseCmdLine(args);
            }
            else if (args.Length != 0 && File.Exists(args[0]))
            {
                Output.WriteLine("Running application as TrustedInstaller ...", Output.Style.Information);
                RunProcess(args);
            }
            else
            {
                Output.WriteLine("Initialize elevated command line prompt as TrustedInstaller ...", Output.Style.Information);
                LaunchWithParams(args);
            }
        }

        private static void RunProcess(IReadOnlyList<string>? args)
        {
            if (args == null || args.Count == 0)
            {
                Output.WriteLine("No arguments provided.", Output.Style.Danger);
                return;
            }

            var exe = args[0];
            string arguments;
            var dirPath = "";

            switch (Path.GetExtension(exe).ToLower())
            {
                case ".bat":
                case ".cmd":
                    arguments = "/c \"" + exe + "\"";
                    break;
                case ".lnk":
                    arguments = "/c start \"" + exe + "\"";
                    break;
                case ".exe":
                    exe = args[0];
                    var argumentBuilder = new StringBuilder();
                    for (var i = 1; i < args.Count; i++)
                    {
                        argumentBuilder.Append(args[i]);
                        argumentBuilder.Append(' ');
                    }
                    arguments = argumentBuilder.ToString().Trim();
                    break;
                default:
                    Output.WriteLine("Unknown file type.", Output.Style.Danger);
                    return;
            }
            if (args[0].ToLower().StartsWith("/wd:"))
            {
                dirPath = args[0].Replace("/wd:", "");
            }
            else if (string.IsNullOrWhiteSpace(dirPath) || !Directory.Exists(dirPath))
            {
                try
                {
                    dirPath = Environment.CurrentDirectory;
                }
                catch (Exception)
                {
                    Output.WriteLine("Unable to determine current directory.", Output.Style.Danger);
                    return;
                }
            }

            if (StartTiService())
            {
                Output.WriteLine($"Target application: {exe}", Output.Style.Information);
                var commandArguments = $"/switchti /Dir:\"{dirPath}\" /Run:\"{exe}\" {arguments}";
                if (AppPath != null)
                    LegendaryTrustedInstaller.RunWithTokenOf("winlogon.exe", true, AppPath, commandArguments);
                else
                    Output.WriteLine("Failed to execute winlogon for elevation: AppPath is empty or invalid.", Output.Style.Danger);
            }
        }

        private static bool StartTiService()
        {
            try
            {
                NativeMethods.TryStartService("TrustedInstaller");
                return true;
            }
            catch (Exception)
            {
                //hmm....
                return false;
            }
        }

        private static void ParseCmdLine(string[] args)
        {
            string exeToRun = "", arguments = "", workingDir = "";

            // args[] can't process DirPath and ExeToRun containing '\'
            // and that will influence the other argument too :(
            // so I need to do it myself :/
            var cmdLine = Environment.CommandLine;
            var iToRun = cmdLine.ToLower().IndexOf("/run:", StringComparison.Ordinal);
            if (iToRun != -1)
            {
                var toRun = cmdLine[(iToRun + 5)..].Trim();
                // Process toRun
                var iDQuote1 = toRun.IndexOf("\"", StringComparison.Ordinal);
                // If a pair of double quote is exist
                if (iDQuote1 != -1)
                {
                    toRun = toRun[(iDQuote1 + 1)..];
                    var iDQuote2 = toRun.IndexOf("\"", StringComparison.Ordinal);
                    if (iDQuote2 != -1)
                    {
                        // before 2nd double quote is ExeToRun, after is Arguments
                        exeToRun = toRun[..iDQuote2];
                        arguments = toRun[(iDQuote2 + 1)..];
                    }
                }
                else
                {
                    // before 1st Space is ExeToRun, after is Arguments
                    var firstSpace = toRun.IndexOf(" ", StringComparison.Ordinal);
                    if (firstSpace == -1) { exeToRun = toRun; }
                    else
                    {
                        exeToRun = toRun[..firstSpace];
                        arguments = toRun[(firstSpace + 1)..];
                    }
                }
            }

            // Process all optional arguments before toRun, '/' as separator
            if (iToRun != -1)
                cmdLine = cmdLine[..iToRun] + "/";

            var cmdline = cmdLine.ToLower();
            var iDir = cmdline.IndexOf("/dir:", StringComparison.Ordinal);
            if (iDir != -1)
            {
                var tmp = cmdLine.Substring(iDir + 5);
                var iNextSlash = tmp.IndexOf("/", StringComparison.Ordinal);
                if (iNextSlash != -1)
                {
                    tmp = tmp[..iNextSlash];
                    workingDir = tmp.Replace("\"", "").Trim();
                }
            }

            LegendaryTrustedInstaller.ForceTokenUseActiveSessionID = true;
            LegendaryTrustedInstaller.RunWithTokenOf("TrustedInstaller.exe", false,
                exeToRun, arguments, workingDir);
        }

        private static void LaunchWithParams(IReadOnlyList<string> args)
        {
            var appName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule!.FileName) + ".exe";
            var exe = "cmd.exe";
            var arguments = $"/k @cls&&@echo TIelevated [Version {Version}]&&@echo {Copyright}&&@echo.&&@echo [i] Spawned elevated process successfully!&&@echo | set /p dummyName=[i] Logged in as: && @whoami";
            var dirPath = "";

            if (args.Count > 0)
            {
                var firstArg = args[0].Replace("\"", "").ToLower();
                var extension = Path.GetExtension(firstArg).ToLower();

                if (firstArg.StartsWith("/wd:"))
                    dirPath = firstArg.Replace("/wd:", "");
                else if (extension.EndsWith(".bat") || extension.EndsWith(".cmd"))
                    arguments = "/c " + args[0];
                else if (extension.EndsWith(".lnk"))
                    arguments = "/c start " + args[0];
                else if (extension.EndsWith(".exe"))
                {
                    exe = args[0];
                    arguments = string.Join(" ", args);
                }
            }

            try
            {
                dirPath = !string.IsNullOrWhiteSpace(dirPath) && Directory.Exists(dirPath) ? dirPath : Environment.CurrentDirectory;
            }
            catch (Exception)
            {
                dirPath = "";
            }

            if (StartTiService())
            {
                LegendaryTrustedInstaller.RunWithTokenOf("winlogon.exe", true,
                    AppPath!, $" /SwitchTI /Dir:\"{dirPath.Replace("\"", "")}\" /Run:\"{exe}\" {arguments}"); //ARGUMENTS
            }
        }
        
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static void ElevateAsAdmin(string exeName, string? arguments = null)
        {
            var startInfo = new ProcessStartInfo(exeName)
            {
                Verb = "runas",
                UseShellExecute = true,
                Arguments = arguments
            };
            Process.Start(startInfo);
        }
    }
}
