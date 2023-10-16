using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TM2toolmanager
{
    public class Err
    {
        public static List<string> Codes = new List<string>
        {
            "Error: \"", "Invalid argument or cannot find file.", "\" is not supported.",
            "\" could not be found.", "\" is not valid."
        };

        public static void Invalid(string file, int reason)
        {
            string fName = PathTool.FileName(file);
            Console.WriteLine($"{Codes[0]}{fName}{Codes[reason]}");
            Environment.Exit(1);
        }

        public static void Help(int code)
        {
            if ( code > 0 ) { Console.WriteLine("\nError: " + Err.Codes[code]); }
            Console.WriteLine("\nTM2 Tool Manager v0.1a by Nekonosuke");
            Console.WriteLine("A tool for unpacking and repacking Level 5 game files");
            Console.WriteLine("\nOptions:");
            Console.WriteLine("    -d, --debug       Enable debug messages");
            Console.WriteLine("    -h, --help        Display this help message\n");
            Console.WriteLine("Useage:");
            Console.WriteLine("Add tool to Path or move game file to same directory as tool\n");
            Console.WriteLine("Unpack PAK archive (.chr, .pac, .pak, etc):");
            Console.WriteLine("./TM2toolmanager [-d || --debug] <MyFile.PAK>\n");
            Console.WriteLine("Rebuild PAK archive (.json):");
            Console.WriteLine("./TM2toolmanager [-d || --debug] <MyFile.json>\n");
            Console.WriteLine("Progress:");
            Console.WriteLine("This tool is currently under construction.");
            Console.WriteLine("Here are its current functionalities:");
            Console.WriteLine("    -Unpack PAK archive (.chr, .pac, .pak, etc)");
            Console.WriteLine("    -Rebuild PAK archive (.json)\n");
            Environment.Exit(1);
        }
    }
}
