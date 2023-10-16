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
            string inputFile = "";
            
            // if (args.Length == 0) { Err.Help(0); }
            switch (args.Length)
            {
                case 0:
                    Err.Help(0); break;
                case 1:
                    if ( (args[0] == "-h") || (args[0] == "--help") ) { Err.Help(0); }
                    
                    if (!File.Exists(args[0])) { Err.Help(1); }
                    else { inputFile = args[0]; }
                    
                    break;
                case 2:
                    if (((args[0] == "-d") || (args[0] == "--debug")) && File.Exists(args[1]))
                    {
                        bDebug = true;
                        inputFile = args[1];
                    }
                    else { Err.Help(1); }
                    break;
                default:
                    if (File.Exists(args[args.Length-1])) { inputFile = args[args.Length-1]; }
                    break;
            }
            
            if (inputFile.EndsWith(".json")) { EXTfinder.RebuildPAK(inputFile, bDebug); }
            
            else { EXTfinder.ExtractPAK(inputFile, bDebug); }
        }
    }
}
