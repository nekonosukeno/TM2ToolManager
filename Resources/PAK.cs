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
     public static class EXTfinder
    {
        public struct PAKsubheader
        {
            // Top level
            public string PAKname {get; set;}
            public string footer {get; set;}
            // Core
            public string fName {get; set;}
            public int headerLength {get; set;}
            public int tillNext {get; set;}
            public int fromHeader {get; set;}
            public string header {get; set;}
            // public byte[] Data {get; set;} // buffer[headerLength..(fromHeader - headerLength)]
        }
        public static (int contains, byte[] buffer) isPAK(string input, bool debug)
        {
            // There is no header for a PAK archive, instead each file inside the archive has its own header
            // These headers contain four major points of data:
            //      File name
            //      Length of the file's header 
            //      The length to the next file from the beginning of the header 
            //      The length to the next file from the end of the header
            byte[] buffer = File.ReadAllBytes(@input);
            int hold = 0x00;
            int contains = 0;
            bool bPAK = false;
            int fSize = buffer.Length;
            // PS2intReader corrects the endian of the bytes and reads an int32
            int HeaderLength = HexTool.PS2intReader(buffer[0x40..0x44]);
            int tillNext = HexTool.PS2intReader(buffer[0x44..0x48]);
            int fromHeader = HexTool.PS2intReader(buffer[0x48..0x4C]);
            int endBytes = HexTool.PS2intReader(buffer[HeaderLength..(HeaderLength + 0x04)]);

            // Start trying to loop through headers
            for (int cursor = 0;;)
            {
                string printCursor = "Cursor @ " + HexTool.BigEndHex(cursor);
                
                hold = cursor;
                if ( cursor == fSize ) { bPAK = true; break; }

                HeaderLength = HexTool.PS2intReader(buffer[(hold + 0x40)..(hold + 0x44)]);
                tillNext = HexTool.PS2intReader(buffer[(hold + 0x44)..(hold + 0x48)]);
                fromHeader = HexTool.PS2intReader(buffer[(hold + 0x48)..(hold + 0x4C)]);
                
                // Prints
                if (debug)
                {
                    Console.WriteLine(printCursor);
                    Console.WriteLine("Till Next: " + HexTool.LitEndHex(tillNext));
                    Console.WriteLine("From Header: " + HexTool.LitEndHex(fromHeader) + "\n");
                }

                // Test if end of data
                if ( (tillNext == -1) || (fromHeader == -1) ) { bPAK = true; break; }
                if ((hold + HeaderLength + 0x04) <= fSize)
                {
                    endBytes = HexTool.PS2intReader(buffer[(hold + HeaderLength)..(hold + HeaderLength + 0x04)]);
                    if ( endBytes == 0x00 ) { bPAK = true; break; }
                }
                
                // Test to continue
                // Sometimes there's a gap between tillNext and fromHeader.
                // There could be all null bytes, a drive letter, a full path, or just random bytes...
                if ( (tillNext + HeaderLength) <= fromHeader )
                {
                    contains += 1;
                    cursor += fromHeader;
                }
                else { Array.Clear(buffer); }
            }
            // Reporting the result
            string result = $"You have a .pak file with {Convert.ToString(contains)} files inside\n";
            
            if ( (contains > 0) && bPAK ) { Console.WriteLine(result); }
            else { Err.Invalid(input, 2); }
            
            return (contains, buffer);
        }

        public static void ExtractPAK(string input, bool debug)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");
            
            var OS = Environment.OSVersion;
            string OpSys = Convert.ToString(OS.Platform);
            string slash = "/";
            if (OpSys.StartsWith("Win")) { slash = "\\"; }
            
            string PAKname = PathTool.BaseName(input);
            string cwd = Directory.GetCurrentDirectory();
            string unPAKdir = $"{cwd}{slash}{PAKname}";
            string unPAKjson = $"{unPAKdir}.json";
            
            var PAKfile = EXTfinder.isPAK(input, debug);

            if ((PAKfile.contains > 0) && (PAKfile.buffer.Length > 0x50))
            {
                // Constructing array of headers
                if (!Directory.Exists(unPAKdir))
                {
                    Directory.CreateDirectory(unPAKdir);
                }

                PAKsubheader[] sub = new PAKsubheader[PAKfile.contains];

                int cursor = 0x00;
                int hold;
                int fSize = PAKfile.buffer.Length;

                for (int i = 0; i < PAKfile.contains; i++)
                {
                    // Setting each header in the array of headers
                    hold = cursor;

                    sub[i].headerLength = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x40)..(hold + 0x44)]);
                    byte[] headerBytes = PAKfile.buffer[hold..(hold + sub[i].headerLength)];
                    sub[i].header = Convert.ToBase64String(headerBytes);
                    sub[i].tillNext = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x44)..(hold + 0x48)]);
                    sub[i].fromHeader = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x48)..(hold + 0x4C)]);
                    byte[] data = PAKfile.buffer[(hold + sub[i].headerLength)..(hold + sub[i].fromHeader)];
                    int firstNull = HexTool.IndexOfByte(headerBytes, "0x00");

                    if (firstNull > 1)
                    {
                        byte[] nameBytes = headerBytes[0x00..(firstNull)];
                        sub[i].fName = $"{unPAKdir}{slash}{sjis.GetString(nameBytes)}";
                    }

                    int printCursor = cursor;
                    string printFooter = null;
                    cursor += sub[i].fromHeader;

                    if (i == 0)
                    {
                        sub[i].PAKname = $"{cwd}{slash}{input}";
                    }

                    if (i == (PAKfile.contains - 1))
                    {
                        byte[] footerBytes = PAKfile.buffer[cursor..(fSize)];
                        sub[i].footer = Convert.ToBase64String(footerBytes);
                        printFooter = "Bytes in current footer: " + HexTool.BigEndHex(footerBytes.Length) + "\n";
                    }

                    // Print lines for debugging
                    if (debug)
                    {
                        Console.WriteLine($"\nCursor {i} @ {HexTool.BigEndHex(cursor)}");
                        if (i == 0) { Console.WriteLine("PAK Name: " + PathTool.FileName(sub[i].PAKname)); }
                        Console.WriteLine("File Name: " + PathTool.FileName(sub[i].fName));
                        Console.WriteLine("Header Length: " + HexTool.BigEndHex(sub[i].headerLength));
                        Console.WriteLine("Bytes in current header: " + HexTool.BigEndHex(headerBytes.Length));
                        Console.WriteLine("Till Next: " + HexTool.LitEndHex(sub[i].tillNext));
                        Console.WriteLine("From Header: " + HexTool.LitEndHex(sub[i].fromHeader));
                        Console.WriteLine("Bytes in file: " + HexTool.BigEndHex(data.Length));
                        if (i == (PAKfile.contains - 1)) { Console.WriteLine(printFooter); }
                    }

                    // Writing extracted files
                    if (data.Length > 0x50) { File.WriteAllBytes(sub[i].fName, data); }

                    // Clearing memory
                    Array.Clear(data);
                }
                
                Console.WriteLine("PAK has been extracted!\n");

                // Writing JaySun
                JsonSerializerOptions indented = new JsonSerializerOptions
                    { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                
                string JsonContent = JsonSerializer.Serialize(sub, indented);
                File.WriteAllText(unPAKjson, JsonContent);
                
                if (debug) { Console.WriteLine($"JSON has been saved to:\n {PathTool.rmName(unPAKjson)}"); }
            }
            else { Err.Invalid(input, 4); }
            
            // Clearing memory
            Array.Clear(PAKfile.buffer);
        }

        public static void RebuildPAK(string json, bool debug)
        {
            string input = PathTool.JSONreader(json);
            PAKsubheader[] oldSubs = JsonSerializer.Deserialize<PAKsubheader[]>(input);

            // I don't wanna deal with merging multiple arrays so I just calculate how big the new PAK will be
            int LengthNewPAK = 0;
            for (int i = 0; i < oldSubs.Length; i++)
            {
                int footerLength = 0x00;
                if (i == (oldSubs.Length - 1))
                {
                    byte[] footer = Convert.FromBase64String(oldSubs[i].footer);
                    footerLength += footer.Length;
                }
                FileInfo newFile = new FileInfo(oldSubs[i].fName);
                // This is okay as long as no one is crazy enough to try making a 2GB IM3 file for a 4GB game
                int fLength = Convert.ToInt32(newFile.Length);
                int fHeaderLength = oldSubs[i].headerLength;
                
                LengthNewPAK += fHeaderLength + fLength + footerLength;
            }

            // Constructing the data for the new PAK
            byte[] NewPAK = new byte[LengthNewPAK];
            int cursor = 0x00;
            string fSizeStr = $"\nRePAK size: {HexTool.BigEndHex(Convert.ToInt32(NewPAK.Length))}";
            if (debug) { Console.WriteLine(fSizeStr); }
            
            for (int i = 0; i < oldSubs.Length; i++)
            {
                if (File.Exists(oldSubs[i].fName)) 
                {
                    byte[] newData = File.ReadAllBytes(oldSubs[i].fName);
                    string found = $"\nFile: \'{PathTool.FileName(oldSubs[i].fName)}\' has been found!";
                    if (debug) { Console.WriteLine(found); }
                    
                    int newSize = Convert.ToInt32(newData.Length);
                    int newFromHeader = oldSubs[i].headerLength + newSize;
                    int newTillNext = (newFromHeader - oldSubs[i].fromHeader) + oldSubs[i].tillNext;
                    byte[] newFromHeaderBytes = HexTool.IntToBytesPS2(newFromHeader);
                    byte[] newTillNextBytes = HexTool.IntToBytesPS2(newTillNext);
                    
                    // reconstruct/append new header to PAK
                    byte[] oldHeader = Convert.FromBase64String(oldSubs[i].header);
                    HexTool.InsertBytes(NewPAK, oldHeader[0x00..0x44], cursor);
                    cursor += 0x44;
                    HexTool.InsertBytes(NewPAK, newTillNextBytes, cursor);
                    cursor += 0x04;
                    HexTool.InsertBytes(NewPAK, newFromHeaderBytes, cursor);
                    cursor += 0x04;
                    HexTool.InsertBytes(NewPAK, oldHeader[0x4C..oldHeader.Length], cursor);
                    cursor += (oldHeader.Length - 0x4C);

                    // Adding file to PAK
                    HexTool.InsertBytes(NewPAK, newData, cursor);
                    cursor += newData.Length;
                    
                    // Last subheader in each .json holds the footer
                    if (i == (oldSubs.Length - 1))
                    {
                        try
                        {
                            byte[] footer = Convert.FromBase64String(oldSubs[i].footer);
                            HexTool.InsertBytes(NewPAK, footer, cursor);
                            cursor += footer.Length;
                            if (debug) { Console.WriteLine(("\nFooter found!")); }
                        }
                        // Shout into the void if no footer
                        catch (Exception)
                        { ; }
                    }
                }
                else { Err.Invalid(input, 3); }
            }
            
            // Writing new PAK archive
            string oldPAK = oldSubs[0].PAKname;
            string PAKpath = PathTool.rmName(oldPAK);
            string newPAKname = PathTool.BaseName(oldPAK) + "_mod" + PathTool.GetExt(oldPAK);
            string newPAKpath = PAKpath + newPAKname;
            
            FileInfo oldFile = new FileInfo(oldPAK);
            int OldPAKLength = Convert.ToInt32(oldFile.Length);

            if (NewPAK.Length != OldPAKLength)
            {
                Console.WriteLine("\nWARNING: You are changing the size of the original PAK archive.");
                Console.WriteLine("         This type of change has not been tested in-game yet.");
            }
            
            File.WriteAllBytes(newPAKpath, NewPAK);

            if (File.Exists(newPAKpath)) {Console.WriteLine($"\nFile: \"{newPAKname}\" has been written to:\n{PAKpath}");}
        }

    }
}
