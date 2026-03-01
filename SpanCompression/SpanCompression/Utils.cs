using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpanCompression
{
    public class Utils
    {
        public static void OpenFileExplorer()
        {
            OpenFileExplorer(Directory.GetCurrentDirectory());
        }

        public static void OpenFileExplorer(string pathToDirectory)
        {
            Process.Start("explorer.exe", pathToDirectory);
        }
        public static void OpenWithMeshLab(string filename)
        {
            var pathToMeshlab = "C:\\Program Files\\VCG\\MeshLab\\meshlab.exe";
            Process.Start(pathToMeshlab, filename);
        }

        public static string? RunExecutable(string executable, string arguments)
        {
            // Create the process start info
            ProcessStartInfo startInfo = new()
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                WorkingDirectory = Path.GetDirectoryName(executable)
            };

            try
            {
                using Process? process = Process.Start(startInfo);

                if (process == null)
                    return null;

                // Optional: read output
                var output = process.StandardOutput.ReadToEnd();
                var errors = process.StandardError.ReadToEnd();

                process.WaitForExit();

                //Console.WriteLine("Output:\n" + output);
                if (!string.IsNullOrEmpty(errors))
                    Console.WriteLine("Errors:\n" + errors);

                return output;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start process: " + ex.Message);
            }

            return null;
        }
    }
}
