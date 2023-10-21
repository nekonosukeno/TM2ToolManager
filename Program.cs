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
    class Program
    {
        static void Main(string[] args)
        {
            
            bool bDebug = false;
            bool bHelp = false;
            bool bBatch = false;
            bool bExtract = false;
            bool bRePAK = false;

            List<string> fileList = new List<string>();
            
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-d":
                    case "--debug":
                        bDebug = true; break;
                    
                    case "-h":
                    case "--help":
                        bHelp = true; break;
                        
                    case "-b":
                    case "--batch":
                        bBatch = true; break;
                        
                    case "-e":
                    case "--extract":
                        bExtract = true; break;
                    
                    case "-p":
                    case "--repack":
                        bRePAK = true; break;
                        
                    default:
                        if (File.Exists(arg)) { fileList.Add(arg); }
                        else { Err.Help(1); }
                        break;
                }
            }
            
            if (bHelp || args.Length == 0) { Err.Help(0); }
            
            string cwd = Directory.GetCurrentDirectory();
            List<string> files = Directory.EnumerateFiles(cwd).ToList();
            List<string> Extensions = new List<string>
                { ".json", ".txt", ".cfg", ".py", ".md", ".cs", ".mds", ".mot", ".tm2", ".bak", ".png", ".pdb", "ger" };

            if (bBatch)
            {
                fileList.Clear();
                if (!bRePAK && !bExtract) { Err.Help(7); }
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        if (bRePAK && file.EndsWith(".json")) { fileList.Add(file); }
                        if (bExtract)
                        {
                            bool bAdd = true;
                            foreach (string badExt in Extensions)
                            {
                                if (file.EndsWith(badExt)) { bAdd = false; }
                            }
                            if (bAdd) {fileList.Add(file);}
                        }
                    }
                }
            }

            foreach (string inputFile in fileList)
            {
                string fName = inputFile;
                if (bDebug) { Console.WriteLine($"Input File: {fName}"); }
                
                var IMGinfo = EXTfinder.IMGinfo(fName, bDebug);

                if (IMGinfo.isIMG)
                {
                    Console.WriteLine($"{IMGinfo.IMGtype} has been found!");
                    EXTfinder.ExtractIMG(fName, IMGinfo.IMGtype, IMGinfo.TM2count, bDebug);
                }
            
                else if (fName.EndsWith(".json"))
                {
                    var jsonContents = Transcoding.JSONreader(fName);
                    if (jsonContents.repackType == "PAK")
                    {
                        EXTfinder.RebuildPAK(jsonContents.contents, bDebug);
                    }
                    else
                    {
                        Console.WriteLine("Do not have a repacker for this type.");
                    }
                }
            
                else { EXTfinder.ExtractPAK(fName, bDebug); }
            }
        }
    }
}
