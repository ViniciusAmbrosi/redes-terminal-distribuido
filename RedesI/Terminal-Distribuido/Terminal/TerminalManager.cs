using System.Diagnostics;
using System.Text;

namespace Terminal_Distribuido.Terminal
{
    public class TerminalManager
    {
        public Process Process { get; set; }

        public TerminalManager()
        {
            this.Process = new System.Diagnostics.Process();
            this.Process.StartInfo.FileName = "/bin/bash";
            //this.Process.StartInfo.FileName = "cmd.exe";
            this.Process.StartInfo.RedirectStandardOutput = true;
            this.Process.StartInfo.RedirectStandardInput = true;
            this.Process.StartInfo.RedirectStandardError = true;
            this.Process.StartInfo.UseShellExecute = false;
            this.Process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        }

        public string ExecuteCommand(string command)
        {
            this.Process.StartInfo.Arguments = "-c " + "\"" + command + "\"";
            //this.Process.StartInfo.Arguments = "/C " + command ;
            this.Process.Start();

            string response = this.Process.StandardOutput.ReadToEnd();

            this.Process.Close();

            return response;
        }
    }
}
