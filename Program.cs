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
                        
                    case "-p" :
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
                foreach (string file in files)
                {
                    if (File.Exists(file))
                    {
                        if (bRePAK && file.EndsWith(".json")) { fileList.Add(file); }
                        if (bExtract)
                        {
                            foreach (string badExt in Extensions)
                            {
                                if (file.EndsWith(badExt)) { ; }
                                else { fileList.Add(file); }
                            }
                        }
                        else { Err.Help(7); }
                    }
                }
            }

            foreach (string inputFile in fileList)
            {
                var IMGinfo = EXTfinder.IMGinfo(inputFile, bDebug);

                if (IMGinfo.isIMG)
                {
                    Console.WriteLine($"{IMGinfo.IMGtype} has been found!");
                    EXTfinder.ExtractIMG(inputFile, IMGinfo.IMGtype, IMGinfo.TM2count, bDebug);
                }
            
                else if (inputFile.EndsWith(".json")) { EXTfinder.RebuildPAK(inputFile, bDebug); }
            
                else { EXTfinder.ExtractPAK(inputFile, bDebug); }
            }
        }
    }
}
