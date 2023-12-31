using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TM2toolmanager
{
    public static class Err
    {
        public static string V = "v0.6rc";
        public static List<string> Codes = new List<string>
        {
            "Error: \"", "Invalid argument or cannot find file.", "\" is not supported.",
            "\" could not be found.", "\" is not valid.", "\" has a size that does not match its offset.", 
            "\" could not be rebuilt./n    New size either too large or too small to be valid.",
            "Batch mode specified but no mode selected.\n       Use extract, repack, or both.",
            "\" could not be extracted; file size is not valid"
        };
        
        public static void Invalid(string file, int reason)
        {
            string fName = PathTool.FileName(file);
            Console.WriteLine($"{Codes[0]}{fName}{Codes[reason]}");
            Environment.Exit(1);
        }
        
        // If Transcoding.TextFromBytes() has an error,
        // either display the bad bytes or log them to a file
        public static void Encoder(byte[] failed)
        {
            string cwd = Directory.GetCurrentDirectory();
            string result = "0 x";
            
            Console.WriteLine("File name or text file could not be encoded to SJIS");
            Console.WriteLine("This is likely an illegal byte sequence error.");

            if (failed.Length < 0x21)
            {
                for (int i = 0; i < failed.Length; i++)
                {
                    result += " " + BitConverter.ToString(new[] { failed[i] });
                }
                Console.WriteLine($"Offending bytes:\n{result}");
            }

            else
            {
                Console.WriteLine($"\nWriting log file to:\n{cwd}");
                File.WriteAllBytes("FailedToEncode.dat", failed);
            }
        }
        
        public static void NullStart()
        {
            Console.Write("!!WARNING Bytes offered for SJIS encoding start with a null");
            Console.Write("          Returning Empty. Will not be able to repack file.");
        }

        public static void Help(int code)
        {
            if ( code > 0 ) { Console.WriteLine("\nError: " + Err.Codes[code]); }
            Console.WriteLine($"\nTM2 Tool Manager {V} by Nekonosuke");
            Console.WriteLine("A tool for unpacking and repacking Level 5 game files");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("    -d, --debug       Enable debug messages");
            Console.WriteLine("    -h, --help        Display this help message");
            Console.WriteLine("    -b, --batch       Use batch processing");
            Console.WriteLine("    -e, --extract     Extract all files in directory");
            Console.WriteLine("    -p, --repack      Repacks all files in directory\n");
            Console.WriteLine("Useage:");
            Console.WriteLine("Unpack PAK or IMG archive (.img, .chr, .pac, .pak, etc):");
            Console.WriteLine("./TM2toolmanager [-d || --debug] <MyFile0.PAK> [MyFile1.IMG...]\n");
            Console.WriteLine("Repack PAK or IMG archive:");
            Console.WriteLine("./TM2toolmanager [-d || --debug] <MyFile0.json> [MyFile1.json...]\n");
            Console.WriteLine("Using batch mode:");
            Console.WriteLine("./TM2toolmanager <--batch> <--repack || --extract> [--debug]\n");
            Environment.Exit(1);
        }
    }
}
