using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TM2toolmanager
{
     public static partial class EXTfinder
    {
        public struct IMGsubheader
        {
            // Top level
            public string IMGheader {get; set;} // Capture whole header in base64string
            public string IMGname {get; set;} // Of the actual .img file
            public string IMGtype {get; set;} // First 0x04 bytes of file - IM2 or IM3
            public int contains {get; set;} // 0x04 - IM2; 0x08 - IM3. # of TM2 in IMG.
            public int TotalHeaderSize {get; set;} // Initial header plus all sub headers
            // Core
            public string fName {get; set;} // Name of TIM2, null terminated
            public int headerLength {get; set;} // 0x40 (IM3) 0x30 (IM2)
            public int chunkSize {get; set;} // 0x20 from start of subheader (IM3 only)
            public int TM2offset {get; set;} // 0x24 from subheader for IM3; 0x20 for IM2
            public int unk0 {get; set;} // 0x28 (IM3) 0x24 (IM2) (from subheader)
            public int unk1 {get; set;} // 0x2C (IM3) 0x28 (IM2)
            public int unk2 {get; set;} // 0x30 (IM3) 0x2C (IM2)
            public int TM2size {get; set;} // 0x44 from subheader (IM3 only)
            public long footer {get; set;} // Last 0x08 bytes of IM3 subheader
        }

        public static (bool isIMG, string IMGtype, int TM2count) IMGinfo(string input, bool debug)
        {
            bool isIMG;
            string IMGtype;
            int TM2count;
            byte[] IMGheader = new byte[0x10];
            
            using (var openFile = File.Open(input, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader hexReader = new BinaryReader(openFile))
                {
                    IMGheader = hexReader.ReadBytes(0x10);
                }
            }
            
            int magicInt = HexTool.PS2intReader(IMGheader[0x00..0x04]);
            int countIM2 = HexTool.PS2intReader(IMGheader[0x04..0x08]);
            int countIM3 = HexTool.PS2intReader(IMGheader[0x08..0x0C]);

            switch (magicInt)
            {
                case 3296585: // 0 x 49 4D 32 00
                    isIMG = true;
                    IMGtype = "IM2";
                    TM2count = countIM2;
                    break;
                case 3362121: // 0 x 49 4D 33 00
                    isIMG = true;
                    IMGtype = "IM3";
                    TM2count = countIM3;
                    break;
                default:
                    isIMG = false;
                    IMGtype = "";
                    TM2count = 0;
                    break;
            }

            if (debug) { Console.WriteLine("\nRandom secret debug info!\n"); }
            return (isIMG, IMGtype, TM2count);
        }

        public static void ExtractIMG(string input, string IMGtype, int TM2count, bool debug)
        {
            string slash = PathTool.GetSlashType();
            string cwd = Directory.GetCurrentDirectory();
            string IMGname = PathTool.BaseName(input);
            string IMGpath = input.Contains(slash) ? PathTool.rmName(input) : $"{cwd}{slash}";
            string unIMGdir = $"{IMGpath}{IMGname}";
            string unIMGjson = $"{unIMGdir}.json";
            
            if (!Directory.Exists(unIMGdir)) { Directory.CreateDirectory(unIMGdir); }
            
            // Reading in file and initiating subheader array
            IMGsubheader[] sub = new IMGsubheader[TM2count];
            byte[] buffer = File.ReadAllBytes(@input);

            // Constants for the offsets these values are stored at
            // These fields are shared between IM2 and IM3
            int TM2OffOff = IMGtype == "IM3" ? 0x24 : 0x20;
            int Unk0off = IMGtype == "IM3" ? 0x28 : 0x24;
            int Unk1off = IMGtype == "IM3" ? 0x2C : 0x28;
            int Unk2off = IMGtype == "IM3" ? 0x30 : 0x2C;
            sub[0].headerLength = IMGtype == "IM3" ? 0x40 : 0x30;
            // These are IM3 exclusive fields
            int ChunkOff = 0x20;
            int TM2sizeOff = IMGtype == "IM3" ? 0x34 : 0x50; // IM2 - Used to calculate size
            int footerOff = 0x38;
            int checkTM2Size = 0x00;
            // For navigating the buffer
            int cursor = 0x10;
            int hold = cursor;
            
            for (int i = 0; i < TM2count; i++)
            {
                hold = cursor;
                
                // Setting our values
                // Core values related to IMG file
                if (i == 0)
                {
                    sub[i].IMGname = input;
                    sub[i].IMGtype = IMGtype;
                    sub[i].IMGheader = Convert.ToBase64String(buffer[0x00..0x10]);
                    sub[i].contains = TM2count;
                    sub[i].TotalHeaderSize = (TM2count * sub[0].headerLength) + 0x10;

                    if (debug)
                    {
                        Console.WriteLine($"\nIMG File Name: {PathTool.FileName(sub[0].IMGname)}");
                        Console.WriteLine($"IMG Version: {sub[0].IMGtype} with {Convert.ToString(TM2count)} textures");
                        Console.WriteLine($"Header Length: {HexTool.BigEndHex(sub[0].headerLength)}");
                    }
                }

                // Common values
                var getName = Transcoding.TextFromBytes(buffer[hold..(hold + 0x20)], debug);
                string fileName = getName.text + ".tm2";
                if (!String.IsNullOrEmpty(fileName)) {sub[i].fName = $"{unIMGdir}{slash}{fileName}";}
                sub[i].TM2offset = HexTool.PS2intReader(buffer[(hold + TM2OffOff)..(hold + TM2OffOff + 0x04)]);
                sub[i].unk0 = HexTool.PS2intReader(buffer[(hold + Unk0off)..(hold + Unk0off + 0x04)]);
                sub[i].unk1 = HexTool.PS2intReader(buffer[(hold + Unk1off)..(hold + Unk1off + 0x04)]);
                sub[i].unk2 = HexTool.PS2intReader(buffer[(hold + Unk2off)..(hold + Unk2off + 0x04)]);
                
                // Type dependant values
                if (IMGtype == "IM3")
                {
                    sub[i].chunkSize = HexTool.PS2intReader(buffer[(hold + ChunkOff)..(hold + ChunkOff + 0x04)]);
                    sub[i].TM2size = HexTool.PS2intReader(buffer[(hold + TM2sizeOff)..(hold + TM2sizeOff + 0x04)]);
                    sub[i].footer = HexTool.PS2intReader(buffer[(hold + footerOff)..(hold + footerOff + 0x08)]);
                    
                    int TM2off = hold + TM2sizeOff + 0x40;
                    // If last loop, check size offset is now end of file
                    checkTM2Size = i < (TM2count - 1) ?
                        HexTool.PS2intReader(buffer[TM2off..(TM2off + 0x04)]) : buffer.Length - sub[i].TM2offset;
                }
                else // if (IMGtype == "IM2")
                {
                    checkTM2Size = HexTool.PS2intReader(buffer[(hold + TM2sizeOff)..(hold + TM2sizeOff + 0x04)]);
                    sub[i].TM2size = i < (TM2count - 1) ?
                        checkTM2Size - sub[i].TM2offset : buffer.Length - sub[i].TM2offset;
                }

                bool verifiedTM2 = checkTM2Size == sub[i].TM2size;
                
                // Debug info
                if (debug)
                {
                    Console.WriteLine($"\nCursor {i} @ {HexTool.BigEndHex(cursor)}\n");
                    if (IMGtype == "IM3") {Console.WriteLine($"Chunk Size: {HexTool.LitEndHex(sub[i].chunkSize)}");}
                    Console.WriteLine($"TM2 Offset: {HexTool.LitEndHex(sub[i].TM2offset)}");
                    Console.WriteLine($"Unkown0 Offset: {HexTool.LitEndHex(sub[i].unk0)}");
                    Console.WriteLine($"Unkown1 Offset: {HexTool.LitEndHex(sub[i].unk1)}");
                    Console.WriteLine($"Unkown2 Offset: {HexTool.LitEndHex(sub[i].unk2)}");
                    if (IMGtype == "IM3")
                    {
                        Console.WriteLine($"TM2 Size: {HexTool.LitEndHex(sub[i].TM2size)}");
                        Console.WriteLine($"Check Size: {HexTool.BigEndHex(checkTM2Size)}");
                        if (!verifiedTM2) {Console.WriteLine("\nWARNING!! Failed to verify TM2 size.");}
                        else {Console.WriteLine("TM2 Size verified as correct.");}
                    }
                    else {Console.WriteLine($"TM2 Size: {HexTool.BigEndHex(sub[i].TM2size)}");}
                }

                // Writing TM2 files
                byte[] TM2data = buffer[sub[i].TM2offset..(sub[i].TM2offset + sub[i].TM2size)];
                if (TM2data.Length > 0x40) { File.WriteAllBytes(sub[i].fName, TM2data); }

                // Clearing memory
                Array.Clear(TM2data);

                cursor += sub[0].headerLength;
            }
            
            // Writing JaySun
            JsonSerializerOptions indented = new JsonSerializerOptions
                { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                
            string JsonContent = JsonSerializer.Serialize(sub, indented);
            JsonContent += "\nIMG";
            File.WriteAllText(unIMGjson, JsonContent);
                
            if (debug) { Console.WriteLine($"\nJSON has been saved to:\n {PathTool.rmName(unIMGjson)}"); }
            Console.WriteLine($"\n{Convert.ToString(TM2count)} TIM2 files have been extracted!");
        }
    }
}
