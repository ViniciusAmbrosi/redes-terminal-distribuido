using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Terminal_Distribuido.Terminal
{
    public class TerminalManager
    {
        public Process Process { get; set; }

        public TerminalManager()
        {
            this.Process = new System.Diagnostics.Process();

            ApplyOperatingSystemDependantAction(
                () => SetExecutable("cmd.exe"),
                () => SetExecutable("/bin/bash"));

            this.Process.StartInfo.RedirectStandardOutput = true;
            this.Process.StartInfo.RedirectStandardInput = true;
            this.Process.StartInfo.RedirectStandardError = true;
            this.Process.StartInfo.UseShellExecute = false;
            this.Process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        }

        public string ExecuteCommand(string command)
        {
            ApplyOperatingSystemDependantAction( 
                () => SetArguments("/C ", command),
                () => SetArguments("-c ", "\"" + command + "\""));

            this.Process.Start();

            string response = this.Process.StandardOutput.ReadToEnd();

            this.Process.Close();

            return response;
        }

        private void ApplyOperatingSystemDependantAction(Action windowsAction, Action linuxAction)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                windowsAction();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                linuxAction();
            }
        }

        private void SetExecutable(string executablePath)
        {
            this.Process.StartInfo.FileName = executablePath;
        }

        private void SetArguments(string argumentPrefix, string argument) {
            this.Process.StartInfo.Arguments = argumentPrefix + argument;
        }
    }
}
