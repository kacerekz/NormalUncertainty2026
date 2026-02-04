using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MyLibrary
{
    public class Util
    {
        public static void OpenWithMeshLab(string filename)
        {
            var pathToMeshlab = "C:\\Program Files\\VCG\\MeshLab\\meshlab.exe";
            Process.Start(pathToMeshlab, filename);
        }
    }
}
