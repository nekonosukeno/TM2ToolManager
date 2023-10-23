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
            public bool isText {get; set;}
            public bool CRLF {get; set;}
            public string txtFooter {get; set;}
        }
        
        public static (int contains, byte[] buffer) PAKinfo(string input, bool debug)
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
            string slash = PathTool.GetSlashType();
            string cwd = Directory.GetCurrentDirectory();
            string PAKname = PathTool.BaseName(input);
            string PAKpath = input.Contains(slash) ? PathTool.rmName(input) : $"{cwd}{slash}";
            string unPAKdir = $"{PAKpath}{PAKname}";
            string unPAKjson = $"{unPAKdir}.json";
            
            var PAKfile = EXTfinder.PAKinfo(input, debug);

            if ((PAKfile.contains > 0) && (PAKfile.buffer.Length > 0x50))
            {
                if (!Directory.Exists(unPAKdir)) { Directory.CreateDirectory(unPAKdir); }
                
                // Constructing array of headers
                PAKsubheader[] sub = new PAKsubheader[PAKfile.contains];

                int cursor = 0x00;
                int hold;
                int fSize = PAKfile.buffer.Length;

                for (int i = 0; i < PAKfile.contains; i++)
                {
                    hold = cursor;
                    
                    // Setting each header in the array of headers
                    sub[i].headerLength = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x40)..(hold + 0x44)]);
                    byte[] headerBytes = PAKfile.buffer[hold..(hold + sub[i].headerLength)];
                    sub[i].header = Convert.ToBase64String(headerBytes);
                    sub[i].tillNext = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x44)..(hold + 0x48)]);
                    sub[i].fromHeader = HexTool.PS2intReader(PAKfile.buffer[(hold + 0x48)..(hold + 0x4C)]);
                    byte[] data = PAKfile.buffer[(hold + sub[i].headerLength)..(hold + sub[i].fromHeader)];
                    var getName = Transcoding.TextFromBytes(headerBytes, debug);
                    string fileName = getName.text;
                    if (!String.IsNullOrEmpty(fileName)) {sub[i].fName = $"{unPAKdir}{slash}{fileName}";}
                    
                    // debug values
                    int printCursor = cursor;
                    string startData = HexTool.BigEndHex((hold + sub[i].headerLength));
                    string endData = HexTool.BigEndHex((hold + sub[i].fromHeader));
                    // string printFooter = null;
                    
                    cursor += sub[i].fromHeader;

                    if (i == 0) { sub[i].PAKname = input; }

                    if (i == (PAKfile.contains - 1))
                    {
                        byte[] footerBytes = PAKfile.buffer[cursor..(fSize)];
                        sub[i].footer = Convert.ToBase64String(footerBytes);
                        // printFooter = "Bytes in current footer: " + HexTool.BigEndHex(footerBytes.Length) + "\n";
                    }

                    // Print lines for debugging
                    if (debug)
                    {
                        Console.WriteLine($"\nCursor {i} @ {HexTool.BigEndHex(printCursor)}");
                        if (i == 0) { Console.WriteLine("PAK Name: " + PathTool.FileName(sub[i].PAKname)); }
                        Console.WriteLine("File Name: " + PathTool.FileName(sub[i].fName));
                        Console.WriteLine("Header Length: " + HexTool.BigEndHex(sub[i].headerLength));
                        Console.WriteLine("Bytes in current header: " + HexTool.BigEndHex(headerBytes.Length));
                        Console.WriteLine("Till Next: " + HexTool.LitEndHex(sub[i].tillNext));
                        Console.WriteLine("From Header: " + HexTool.LitEndHex(sub[i].fromHeader));
                        Console.WriteLine("Bytes in file: " + HexTool.BigEndHex(data.Length));
                        // Console.WriteLine($"Start of Data @ {startData}");
                        // Console.WriteLine($"End of Data @ {endData}");
                        // if (i == (PAKfile.contains - 1)) { Console.WriteLine(printFooter); }
                    }

                    // Writing extracted files
                    if (data.Length > 0x50)
                    {
                        // Scary looking but in short if it's a text file I cut off the null bytes footer,
                        // and store it in the .json then convert the text to UTF-8 and save it.
                        // This has to get re-encoded to Shift_JIS on import.
                        bool hasCRLF = false;
                        if ( getName.text.EndsWith(".cfg") || getName.text.EndsWith(".info") ||
                             getName.text.EndsWith(".txt") || getName.text.StartsWith("#") )
                        {
                            sub[i].isText = true;
                            var getText = Transcoding.TextFromBytes(data, debug);

                            if (getText.footer.Length > 0x00)
                            {
                                byte[] sjisBuffer = data[0x00..(data.Length - getText.footer.Length)];
                                byte[] txtBuffer = Transcoding.ToUTF8(sjisBuffer);
                                
                                File.WriteAllBytes(sub[i].fName, txtBuffer);
                                sub[i].txtFooter = Convert.ToBase64String(getText.footer);
                                hasCRLF = Transcoding.CheckCRLF(sjisBuffer);
                                
                                Array.Clear(txtBuffer);
                            }
                            else
                            {
                                byte[] U8data = Transcoding.ToUTF8(data);
                                File.WriteAllBytes(sub[i].fName, U8data);
                                
                                byte[] empty = new byte[0];
                                sub[i].txtFooter = Convert.ToBase64String(empty);
                                
                                Array.Clear(U8data);
                            }
                        }
                        else // if fileName is NOT a text file
                        {
                            byte[] empty = new byte[0];
                            sub[i].txtFooter = Convert.ToBase64String(empty);
                            sub[i].isText = false;
                            
                            File.WriteAllBytes(sub[i].fName, data);
                        }
                        if (debug) Console.WriteLine(hasCRLF ? $"Found Existing CRLF" : "No CRLF found");
                        sub[i].CRLF = hasCRLF;
                    }
                    else { Err.Invalid(fileName, 8); }
                    
                    Array.Clear(data);
                }
                
                Console.WriteLine("PAK has been extracted!\n");

                // Writing JaySun + type of repack to do when reloaded
                JsonSerializerOptions indented = new JsonSerializerOptions
                    { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                
                string JsonContent = JsonSerializer.Serialize(sub, indented);
                JsonContent += "\nPAK";
                File.WriteAllText(unPAKjson, JsonContent);
                
                if (debug) { Console.WriteLine($"JSON has been saved to:\n {PathTool.rmName(unPAKjson)}"); }
            }
            else { Err.Invalid(input, 4); }
            
            Array.Clear(PAKfile.buffer);
        }

        public static int GetRePAKsize(string jsonContent, bool debug)
        {
            PAKsubheader[] oldSubs = JsonSerializer.Deserialize<PAKsubheader[]>(jsonContent);

            int LengthNewPAK = 0;
            int txtFooterTotal = 0;
            
            if (debug) {Console.WriteLine("Calculating New PAK size...\n");}
            
            // We have to loop through the subheaders to tally all the necessary data
            for (int i = 0; i < oldSubs.Length; i++)
            {
                FileInfo newFile = new FileInfo(oldSubs[i].fName);
                bool isTXT = oldSubs[i].isText;
                
                IMGsubheader empty = new IMGsubheader();
                var fInfo = PathTool.FileReader(empty, oldSubs[i], "PAK", debug);

                int fLength = Convert.ToInt32(fInfo.contents.Length);
                int fHeaderLength = oldSubs[i].headerLength;
                txtFooterTotal += fInfo.footer.Length;
                
                int footerLength = 0x00;
                if (i == (oldSubs.Length - 1))
                {
                    byte[] footer = Convert.FromBase64String(oldSubs[i].footer);
                    footerLength += footer.Length;
                }

                if (debug)
                {
                    if (isTXT)
                    {
                        Console.WriteLine($"!!  Using Text Mode  !!");
                        Console.WriteLine($"Text Footer #{Convert.ToString(i)} Length: {Convert.ToString(fInfo.footer.Length)}");
                    }
                    Console.WriteLine($"PAK Footer Length: {Convert.ToString(footerLength)}");
                    Console.WriteLine($"New File Length: {Convert.ToString(fLength)}");
                    Console.WriteLine($"Header Length: {Convert.ToString(fHeaderLength)}\n");
                }

                
                LengthNewPAK += fHeaderLength + fLength + footerLength;
            }
            LengthNewPAK += txtFooterTotal;
            
            string oldPAK = oldSubs[0].PAKname;
            string newPAKname = PathTool.BaseName(oldPAK) + "_mod" + PathTool.GetExt(oldPAK);
            long A_LOT = 2 * ((1024 * 1024 * 1024) - 1); // 2GB, in other words
            
            if ((LengthNewPAK < 0x100) || (LengthNewPAK >= A_LOT )) {Err.Invalid(newPAKname, 6);}

            if (debug) {Console.WriteLine($"New PAK Length: {HexTool.BigEndHex(LengthNewPAK)}\n");}
            return LengthNewPAK;
        }

        public static void RebuildPAK(string jsonContent, bool debug)
        {
            PAKsubheader[] oldSubs = JsonSerializer.Deserialize<PAKsubheader[]>(jsonContent);
            
            // Constructing the data for the new PAK
            int LengthNewPAK = GetRePAKsize(jsonContent, debug);
            byte[] NewPAK = new byte[LengthNewPAK];
            int cursor = 0x00;
            string fSizeStr = $"\nRePAK size: {HexTool.BigEndHex(Convert.ToInt32(NewPAK.Length))}";
            if (debug) { Console.WriteLine(fSizeStr); }
            
            for (int i = 0; i < oldSubs.Length; i++)
            {
                string newFile = PathTool.FileName(oldSubs[i].fName);
                
                if (File.Exists(oldSubs[i].fName))
                {
                    string found = $"\nFile: \'{newFile}\' has been found!";
                    
                    // Might be a text file that needs to be converted back to SJIS
                    // This text file might have a new CRLF mark that needs to be removed
                    IMGsubheader empty = new IMGsubheader();
                    var fInfo = PathTool.FileReader(empty, oldSubs[i], "PAK", debug);
                    
                    int fLength = fInfo.contents.Length + fInfo.footer.Length;
                    bool isTXT = oldSubs[i].isText;
                    
                    // Calculating
                    int newFromHeader = oldSubs[i].headerLength + fLength;
                    int newTillNext = (newFromHeader - oldSubs[i].fromHeader) + oldSubs[i].tillNext;
                    byte[] newFromHeaderBytes = HexTool.IntToBytesPS2(newFromHeader);
                    byte[] newTillNextBytes = HexTool.IntToBytesPS2(newTillNext);

                    // Debug prints
                    if (debug)
                    {
                        Console.WriteLine(found); 
                        if (isTXT) {Console.WriteLine($"  !!  Using Text Mode  !!");}
                        Console.WriteLine($"New Data Length: {HexTool.BigEndHex(fLength)}");
                        Console.WriteLine($"New From Header: {HexTool.LitEndHex(newFromHeader)}");
                        Console.WriteLine($"New Till Next: {HexTool.LitEndHex(newTillNext)}");
                    }
                    
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
                    HexTool.InsertBytes(NewPAK, fInfo.contents, cursor);
                    cursor += fInfo.contents.Length;
                    
                    // Always 0 unless it's both a text file and this text file has a footer
                    if (fInfo.footer.Length > 0)
                    {
                        HexTool.InsertBytes(NewPAK, fInfo.footer, cursor);
                        cursor += fInfo.footer.Length;
                    }
                    
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
                else { Err.Invalid(newFile, 3); }
            }
            
            // Writing new PAK archive
            string oldPAK = oldSubs[0].PAKname;
            
            PathTool.WriteArchive(oldPAK, NewPAK);
        }
    }
}
