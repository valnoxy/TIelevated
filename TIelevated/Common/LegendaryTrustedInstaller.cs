using System.Diagnostics;
using System.Runtime.InteropServices;
using TIelevated.Common;
using static TIelevated.NativeMethods;

// Readopted from https://github.com/Raymai97/SuperCMD

namespace TIelevated
{
    internal class LegendaryTrustedInstaller
    {
        static STARTUPINFO SI;
        static PROCESSINFO PI;
        static SECURITY_ATTRIBUTES dummySA = new SECURITY_ATTRIBUTES();
        static IntPtr hProc, hToken, hDupToken, pEnvBlock;
        public static bool ForceTokenUseActiveSessionID;

        public static void RunWithTokenOf(
            string processName,
            bool ofActiveSessionOnly,
            string exeToRun,
            string arguments,
            string workingDir = "")
        {
            var pids = new List<int>();
            foreach (var p in Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(processName)))
            {
                pids.Add(p.Id);
                break;
            }

            if (pids.Count == 0)
                return;

            RunWithTokenOf(pids[0], exeToRun, arguments, workingDir);
        }

        public static void RunWithTokenOf(
            int processId,
            string exeToRun,
            string arguments,
            string workingDir = "")
        {
            try
            {
                #region Process ExeToRun, Arguments and WorkingDir

                // If ExeToRun is not absolute path, then let it be
                exeToRun = Environment.ExpandEnvironmentVariables(exeToRun);
                if (!exeToRun.Contains("\\"))
                {
                    foreach (var path in Environment.ExpandEnvironmentVariables("%path%").Split(';'))
                    {
                        var guess = path + "\\" + exeToRun;
                        if (File.Exists(guess))
                        {
                            exeToRun = guess;
                            break;
                        }
                    }
                }

                if (!File.Exists(exeToRun)) return;

                // If WorkingDir not exist, let it be the dir of ExeToRun
                // ExeToRun no dir? Impossible, as I would GoComplain() already
                workingDir = Environment.ExpandEnvironmentVariables(workingDir);
                if (!Directory.Exists(workingDir)) workingDir = Path.GetDirectoryName(exeToRun);

                // If arguments exist, CmdLine must include ExeToRun as well
                arguments = Environment.ExpandEnvironmentVariables(arguments);
                string? cmdLine = null;
                if (arguments != "")
                {
                    if (exeToRun.Contains(" "))
                        cmdLine = "\"" + exeToRun + "\" " + arguments;
                    else
                        cmdLine = exeToRun + " " + arguments;
                }

                #endregion

                // Set privileges of current process
                Output.WriteLine("Set privileges to SeDebugPrivilege");
                var privs = "SeDebugPrivilege";
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ALL_ACCESS, out hToken))
                    return;

                foreach (var priv in privs.Split(','))
                {
                    if (!LookupPrivilegeValue("", priv, out var Luid))
                        return;

                    var tp = new TOKEN_PRIVILEGES
                    {
                        PrivilegeCount = 1,
                        Luid = Luid,
                        Attrs = SE_PRIVILEGE_ENABLED
                    };
                    if (!(AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero) &
                          Marshal.GetLastWin32Error() == 0))
                        return;
                }

                CloseHandle(hToken);

                // Open process by PID
                hProc = OpenProcess(ProcessAccessFlags.All, false, processId);

                // Open process token
                if (!OpenProcessToken(hProc, TOKEN_DUPLICATE | TOKEN_QUERY, out hToken)) return;
                Output.WriteLine($"Process with handle {hProc} and DesiredAccess {hToken} opened");

                // Duplicate to hDupToken
                if (!DuplicateTokenEx(hToken, TOKEN_ALL_ACCESS, ref dummySA,
                        SecurityImpersonationLevel.SecurityIdentification,
                        TokenType.TokenPrimary, out hDupToken))
                    return;
                Output.WriteLine("Token duplicated into memory: " + hDupToken);

                // Set session ID to make sure it shows in current user desktop
                // Only possible when SuperCMD running as SYSTEM!
                if (ForceTokenUseActiveSessionID)
                {
                    var SID = WTSGetActiveConsoleSessionId();
                    Output.WriteLine("Set session id: " + SID);
                    if (!SetTokenInformation(hDupToken, TOKEN_INFORMATION_CLASS_TokenSessionId, ref SID,
                            (uint)sizeof(uint)))
                        return;
                }

                // Create environment block
                if (!CreateEnvironmentBlock(out pEnvBlock, hToken, true))
                    return;
                Output.WriteLine($"Created environment block: {pEnvBlock}");

                // Create process with the token we "stole" ^^
                Output.WriteLine("Create process with stolen token");
                var dwCreationFlags = (NORMAL_PRIORITY_CLASS | CREATE_NEW_CONSOLE | CREATE_UNICODE_ENVIRONMENT);
                SI = new STARTUPINFO();
                SI.cb = Marshal.SizeOf(SI);
                SI.lpDesktop = "winsta0\\default";
                PI = new PROCESSINFO();
                Output.WriteLine("StartupInfo structure size set to: " + SI.cb + " bytes");
                Output.WriteLine("lpDesktop set to: " + SI.lpDesktop);
                Output.WriteLine("dwCreationFlags set to: " + dwCreationFlags);

                // CreateProcessWithTokenW doesn't work in Safe Mode
                // CreateProcessAsUserW works, but if the Session ID is different,
                // we need to set it via SetTokenInformation()
                if (!CreateProcessWithTokenW(hDupToken, LogonFlags.WithProfile, exeToRun, cmdLine,
                        dwCreationFlags, pEnvBlock, workingDir!, ref SI, out PI))
                {
                    if (!CreateProcessAsUserW(hDupToken, exeToRun, cmdLine, ref dummySA, ref dummySA,
                            false, dwCreationFlags, pEnvBlock, workingDir!, ref SI, out PI))
                    {
                        return;
                    }
                }

                CleanUp();
            }
            catch (Exception ex)
            {
                Output.WriteLine("An exception has occurred: " + ex.Message, Output.Style.Danger);
            }
        }

        private static void CleanUp()
        {
            Output.WriteLine("Cleaning up (Closing all handles) ...", Output.Style.Information);
            CloseHandle(SI.hStdError);
            SI.hStdError = IntPtr.Zero;

            CloseHandle(SI.hStdInput);
            SI.hStdInput = IntPtr.Zero;

            CloseHandle(SI.hStdOutput);
            SI.hStdOutput = IntPtr.Zero;

            CloseHandle(PI.hThread);
            PI.hThread = IntPtr.Zero;

            CloseHandle(PI.hProcess);
            PI.hThread = IntPtr.Zero;

            DestroyEnvironmentBlock(pEnvBlock);
            pEnvBlock = IntPtr.Zero;

            CloseHandle(hDupToken);
            hDupToken = IntPtr.Zero;

            CloseHandle(hToken);
            hToken = IntPtr.Zero;
        }
    }
}
