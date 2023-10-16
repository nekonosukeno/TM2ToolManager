using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TM2toolmanager
{
    public static class PathTool
    {
        // Returns path to a file but without the file name
        public static string rmName(string Path)
        {
            string[] PathSplit = Path.Split(new char[] { '/', '\\' });
            string NewPath;

            if (Path.Contains("/")) { NewPath = String.Join("/", PathSplit, 0, PathSplit.Length - 1) + "/"; }
            else { NewPath = String.Join("\\", PathSplit, 0, PathSplit.Length - 1) + "\\"; }

            return NewPath;
        }

        // Gets the base file name without the path
        public static string FileName(string Path)
        {
            string[] PathSplit = Path.Split(new char[] { '/', '\\' });
            string fName = PathSplit[PathSplit.Length - 1];
            return fName;
        }
        
        // Gets the base file name without the path or extension, preserving stupid periods.
        public static string BaseName(string Path)
        {
            string[] PathSplit = Path.Split(new char[] { '/', '\\' });
            string fName = PathSplit[PathSplit.Length - 1];
            string[] NameSplit = fName.Split(".", StringSplitOptions.RemoveEmptyEntries);
            string baseName = String.Join(".", NameSplit, 0, NameSplit.Length - 1);
            if (baseName.StartsWith(".")) { baseName = baseName.Remove(0, 1); }
            if (baseName.EndsWith(".")) { baseName = baseName.Remove(baseName.Length - 1, 1); }
            return baseName;
        }
        
        // Gets file's extension including the "." (dot)
        public static string GetExt(string Path)
        {
            string[] PathSplit = Path.Split(".", StringSplitOptions.RemoveEmptyEntries);
            string fileExt = "." + PathSplit[PathSplit.Length - 1];
            return fileExt;
        }

        // Unix to Windows/Wine. Assumes full path, not local
        public static string u2w(string Path)
        {
            string NewPath;
            if (!Path.StartsWith('/')) { NewPath = "Z:\\" + Path.Replace('/', '\\'); }
            else { NewPath = "Z:" + Path.Replace('/', '\\'); }

            return NewPath;
        }

        // Windows/Wine to Unix. Assumes full path, not local
        public static string w2u(string Path)
        {
            string SlashPath = Path.Replace('\\', '/');
            string NewPath = SlashPath.Substring(2, SlashPath.Length - 2);
            
            return NewPath;
        }

        // Does what it says on the can
        public static string JSONreader(string Path)
        {
            string result = "";
            using (StreamReader readJSON = new StreamReader(Path))
            {
                result += readJSON.ReadToEnd();
            }

            return result;
        }
    }
}
