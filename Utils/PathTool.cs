using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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
        
        // Returns whether forward or back slash
        public static string GetSlashType()
        {
            var OS = Environment.OSVersion;
            string OpSys = Convert.ToString(OS.Platform);
            string slash = "/";
            if (OpSys.StartsWith("Win")) { slash = "\\"; }

            return slash;
        }

        public static (byte[] contents, byte[] footer) FileReader(Archives.IMGsubheader IMGsub,
            Archives.PAKsubheader PAKsub, string type, bool debug)
        {
            // This method differentiates whether the input file is a text file or binary data
            // It also determines and splits any trailing null bytes from the end of a text file

            // The overloads are super hacky on this. Make an empty header of the type you don't need
            // Idk how else to get this method to work for both header type structs...help me?

            bool bIMG = type.ToLower() == "img";

            byte[] dataBuffer = bIMG ? File.ReadAllBytes(IMGsub.fName) : File.ReadAllBytes(PAKsub.fName);
            byte[] txtFooterBytes = bIMG ? Convert.FromBase64String(IMGsub.txtFooter) : Convert.FromBase64String(PAKsub.txtFooter);
            int fLength = dataBuffer.Length;
            bool isTXT = bIMG ? IMGsub.isText : PAKsub.isText;
            byte[] textBuffer = isTXT ? Transcoding.ToSJIS(dataBuffer) : new byte[0];
            bool wasCRLF = bIMG ? IMGsub.CRLF : PAKsub.CRLF;
            bool nowCRLF = false;

            if (isTXT)
            {
                nowCRLF = Transcoding.CheckCRLF(textBuffer);
                fLength = textBuffer.Length;
            }

            if (!wasCRLF && nowCRLF)
            {
                fLength -= 2;
                if (debug) {Console.WriteLine("!! NEW CRLF FOUND !!");}
            }

            byte[] contents = isTXT ? textBuffer[0x00..fLength] : dataBuffer;

            return (contents, txtFooterBytes);
        }
        
        public static void WriteArchive(string oldFile, byte[] newData)
        {
            // Writes the given byte[] to a new file in the same location as
            // the old one. Uses the same name but with "_mod" appended.
            string slash = PathTool.GetSlashType();
            string cwd = Directory.GetCurrentDirectory();
            FileInfo oldFileFI = new FileInfo(oldFile);
            
            string newFileName = PathTool.BaseName(oldFile) + "_mod" + PathTool.GetExt(oldFile);
            string newFilePath = oldFile.Contains(slash) ? PathTool.rmName(oldFile) : $"{cwd}{slash}";
            string newFile = newFilePath + newFileName;
            int OldFileLength = Convert.ToInt32(oldFileFI.Length);

            if (newData.Length != OldFileLength)
            {
                Console.WriteLine("\nWARNING: You are changing the size of the original archive file.");
                Console.WriteLine("         This type of change has not been tested in-game yet.");
            }
            
            File.WriteAllBytes(newFile, newData);

            if (File.Exists(newFile))
            {
                Console.WriteLine($"\nFile: \"{newFileName}\" has been written to:\n{newFilePath}\n");
            }
            else {Err.Invalid(newFileName, 3);}
        }

        public static void BackupIMG(string jsonContent, bool debug)
        {
            // Call immediately before rePAKing to backup OG IMG
            // Detects if user manually reIMG'd, if not repacks IMG
            //
            // To make a backup we need the original IMG name, the .bak name
            // and the IMG_mod name. Rename OG to IMG.bak and IMG_mod to OG IMG name
            string JSON = jsonContent;
            Archives.IMGsubheader[] subs = JsonSerializer.Deserialize<Archives.IMGsubheader[]>(JSON);
            
            string oldIMG = subs[0].IMGname;
            string slash = PathTool.GetSlashType();
            string cwd = oldIMG.Contains(slash) ? PathTool.rmName(oldIMG) : oldIMG;
            
            // IMG.img
            string IMGog = oldIMG;
            // IMG.img.bak
            string IMGbak = $"{IMGog}.bak";
            // IMG_mod.img
            string newIMG = PathTool.BaseName(oldIMG) + "_mod" + PathTool.GetExt(oldIMG);
            string newIMGpath = oldIMG.Contains(slash) ? PathTool.rmName(oldIMG) : $"{cwd}{slash}";
            string IMGmod = newIMGpath + newIMG;
            
            // End User did NOT manually repack IMG
            if (!File.Exists(IMGmod))
            {
                Console.WriteLine("\nNo repacked IMG detected, repacking now...");
                Archives.RebuildIMG(jsonContent, debug);
            }
            
            // This will delete the original file if you mod the same IMG twice
            // Be sure to keep more substantial backups
            if (File.Exists(IMGmod))
            {
                Console.WriteLine("Backing up original IMG...\n");
                if (File.Exists(@IMGbak)) { File.Delete(@IMGbak); }
                File.Move(@IMGog, @IMGbak);
            
                if (File.Exists(@IMGog)) { File.Delete(@IMGog); }
                File.Move(@IMGmod, @IMGog);
            }
            else {Console.WriteLine("Error: could not find IMG to replace original!");}
        }
    }
}
