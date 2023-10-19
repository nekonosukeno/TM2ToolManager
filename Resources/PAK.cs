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
            
            string PAKname = PathTool.BaseName(input);
            string cwd = Directory.GetCurrentDirectory();
            string unPAKdir = $"{cwd}{slash}{PAKname}";
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

                    if (i == 0)
                    {
                        sub[i].PAKname = $"{cwd}{slash}{input}";
                    }

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
            
            Array.Clear(PAKfile.buffer);
        }

        public static int GetRePAKsize(string json, bool debug)
        {
            // To get the total size we have a lot of things to consider:
            //     Each subheader length
            //     Length of PAK footer
            //     if we have a text file:
            //         Each text file may have a footer in the json
            //         Needs to be converted back to SJIS
            //             before tallying its length
            //         Editing a text file may add a CRLF that wasn't there
            //     else we can use the actual file's length
            string input = Transcoding.JSONreader(json);
            PAKsubheader[] oldSubs = JsonSerializer.Deserialize<PAKsubheader[]>(input);

            int LengthNewPAK = 0;
            int txtFooterTotal = 0;
            
            if (debug) {Console.WriteLine("Calculating New PAK size...\n");}
            
            // We have to loop through the subheaders to pull all the necessary data
            for (int i = 0; i < oldSubs.Length; i++)
            {
                FileInfo newFile = new FileInfo(oldSubs[i].fName);
                bool isTXT = oldSubs[i].isText;
                bool wasCRLF = oldSubs[i].CRLF;
                int bufferLength = 0;
                byte[] txtFooterBytes = Convert.FromBase64String(oldSubs[i].txtFooter);
                byte[] textBuffer = isTXT ? Transcoding.ToSJIS(File.ReadAllBytes(oldSubs[i].fName)) : new byte[0];
                bool nowCRLF = false;
                if (isTXT && (textBuffer.Length > 4)) { nowCRLF = Transcoding.CheckCRLF(textBuffer); }

                // Long to Int32 is okay as long as no one is crazy enough to make a 2GB+ IM3 file for a 4GB game
                long LengthLong = isTXT ? textBuffer.Length : newFile.Length;
                int fLength = Convert.ToInt32(LengthLong);
                if (!wasCRLF && nowCRLF)
                {
                    fLength -= 2;
                    if (debug) {Console.WriteLine("!! NEW CRLF FOUND !!");}
                }
                int fHeaderLength = oldSubs[i].headerLength;
                int txtFooterLength = txtFooterBytes.Length;
                txtFooterTotal += txtFooterLength;
                
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
                        Console.WriteLine($"Text Footer #{Convert.ToString(i)} Length: {Convert.ToString(txtFooterLength)}");
                    }
                    Console.WriteLine($"PAK Footer Length: {Convert.ToString(footerLength)}");
                    Console.WriteLine($"New File Length: {Convert.ToString(fLength)}");
                    Console.WriteLine($"Header Length: {Convert.ToString(fHeaderLength)}\n");
                }

                
                LengthNewPAK += fHeaderLength + fLength + footerLength + txtFooterTotal;
            }
            string oldPAK = oldSubs[0].PAKname;
            string newPAKname = PathTool.BaseName(oldPAK) + "_mod" + PathTool.GetExt(oldPAK);
            long A_LOT = 2 * ((1024 * 1024 * 1024) - 1); // 2GB, in other words
            
            if ((LengthNewPAK < 0x100) || (LengthNewPAK >= A_LOT )) {Err.Invalid(newPAKname, 6);}

            if (debug) {Console.WriteLine($"New PAK Length: {HexTool.BigEndHex(LengthNewPAK)}\n");}
            return LengthNewPAK;
        }

        public static void RebuildPAK(string json, bool debug)
        {
            string input = Transcoding.JSONreader(json);
            PAKsubheader[] oldSubs = JsonSerializer.Deserialize<PAKsubheader[]>(input);
            
            // Constructing the data for the new PAK
            int LengthNewPAK = GetRePAKsize(json, debug);
            byte[] NewPAK = new byte[LengthNewPAK];
            int cursor = 0x00;
            string fSizeStr = $"\nRePAK size: {HexTool.BigEndHex(Convert.ToInt32(NewPAK.Length))}";
            if (debug) { Console.WriteLine(fSizeStr); }
            
            for (int i = 0; i < oldSubs.Length; i++)
            {
                if (File.Exists(oldSubs[i].fName))
                {
                    string newFile = PathTool.FileName(oldSubs[i].fName);
                    string found = $"\nFile: \'{newFile}\' has been found!";
                    
                    // Might be a text file that needs to be converted back to SJIS
                    // This text file might have a new CRLF mark that needs to be removed
                    byte[] dataBuffer = File.ReadAllBytes(oldSubs[i].fName);
                    int fLength = dataBuffer.Length;
                    bool isTXT = oldSubs[i].isText;
                    byte[] textBuffer = isTXT ? Transcoding.ToSJIS(dataBuffer) : new byte[0];
                    bool wasCRLF = oldSubs[i].CRLF;
                    bool nowCRLF = false;
                    if (isTXT)
                    {
                        nowCRLF = Transcoding.CheckCRLF(textBuffer);
                        fLength = textBuffer.Length;
                    }

                    if (!wasCRLF && nowCRLF) { fLength -= 2; }
                    
                    byte[] newData = isTXT ? textBuffer[0x00..fLength] : dataBuffer;
                    
                    // Calculating
                    byte[] txtFooterBytes = Convert.FromBase64String(oldSubs[i].txtFooter);
                    int txtFootByteLength = txtFooterBytes.Length;
                    int newSize = Convert.ToInt32(newData.Length) + txtFootByteLength;
                    int newFromHeader = oldSubs[i].headerLength + newSize;
                    int newTillNext = (newFromHeader - oldSubs[i].fromHeader) + oldSubs[i].tillNext;
                    byte[] newFromHeaderBytes = HexTool.IntToBytesPS2(newFromHeader);
                    byte[] newTillNextBytes = HexTool.IntToBytesPS2(newTillNext);

                    // Debug prints
                    if (debug)
                    {
                        Console.WriteLine(found); 
                        if (isTXT) {Console.WriteLine($"  !!  Using Text Mode  !!");}
                        Console.WriteLine($"New Data Length: {HexTool.BigEndHex(newData.Length)}");
                        Console.WriteLine($"Repack File Size: {HexTool.BigEndHex(newSize)}");
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
                    HexTool.InsertBytes(NewPAK, newData, cursor);
                    cursor += newData.Length;
                    
                    // Always 0 unless it's both a text file and this text file has a footer
                    if (txtFootByteLength > 0)
                    {
                        HexTool.InsertBytes(NewPAK, txtFooterBytes, cursor);
                        cursor += txtFooterBytes.Length;
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
                else { Err.Invalid(input, 3); }
            }
            
            // Writing new PAK archive
            string oldPAK = oldSubs[0].PAKname;
            FileInfo oldFile = new FileInfo(oldPAK);
            
            string PAKpath = PathTool.rmName(oldPAK);
            string newPAKname = PathTool.BaseName(oldPAK) + "_mod" + PathTool.GetExt(oldPAK);
            string newPAKpath = PAKpath + newPAKname;
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
