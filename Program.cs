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
                { ".json", ".txt", ".cfg", ".py", ".md", ".cs", ".mds", ".mot", ".tm2", ".bak", ".png",
                    ".pdb", "ger", ".exe", ".bin", ".lst", ".str", ".mes" };

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
                string slash = PathTool.GetSlashType();
                string unPAKdir = fName.Contains(slash) ?
                    $"{PathTool.rmName(fName)}{PathTool.BaseName(fName)}" : $"{cwd}{slash}{PathTool.BaseName(fName)}";
                if (bDebug) { Console.WriteLine($"Input File: {fName}"); }

                Archives.IMGinfo IMGinfo = Archives.getIMGinfo(fName, bDebug);

                if (IMGinfo.isIMG)
                {
                    Console.WriteLine($"{IMGinfo.IMGtype} has been found!");
                    Archives.ExtractIMG(IMGinfo, bDebug);
                }
            
                else if (fName.EndsWith(".json"))
                {
                    var jsonContents = Transcoding.JSONreader(fName);
                    
                    if (jsonContents.repackType == "PAK")
                    {
                        Console.WriteLine("Searching for IMG archives that were in this PAK...");
                        // Console.WriteLine($"Looking in: {unPAKdir}");
                        
                        List<string> unPAKedFiles = Directory.EnumerateFiles(@unPAKdir).ToList();

                        foreach (string file in unPAKedFiles)
                        {
                            string checkJson = file;
                            if (checkJson.EndsWith(".json"))
                            {
                                var PAKjsonContents = Transcoding.JSONreader(checkJson);

                                if (PAKjsonContents.repackType == "IMG")
                                {
                                    PathTool.BackupIMG(PAKjsonContents.contents, bDebug);
                                }
                            }
                        }
                        
                        Console.WriteLine("Repacking PAK archive...");
                        Archives.RebuildPAK(jsonContents.contents, bDebug);
                    }
                    else if (jsonContents.repackType == "IMG")
                    {
                        Archives.RebuildIMG(jsonContents.contents, bDebug);
                    }
                    else
                    {
                        Console.WriteLine("Do not have a repacker for this type.");
                    }
                }

                else
                {
                    // Aiming for a PAK archive here. If extracted, checks subdirectory
                    // for IMG files and extracts those as well
                    Archives.ExtractPAK(fName, bDebug);
                    
                    if (Directory.Exists(@unPAKdir))
                    {
                        Console.WriteLine("\nSearching for IMG archives that were in this PAK...\n");
                        List<string> unPAKedFiles = Directory.EnumerateFiles(@unPAKdir).ToList();

                        foreach (string file in unPAKedFiles)
                        {
                            string fileName = file;
                            Archives.IMGinfo unPAKisIMG = Archives.getIMGinfo(fileName, bDebug);

                            if (unPAKisIMG.isIMG)
                            {
                                Console.WriteLine($"{unPAKisIMG.IMGtype} has been found! Unpacking now...");
                                Archives.ExtractIMG(unPAKisIMG, bDebug);
                            }
                        }
                    }
                }
            }
        }
    }
}
