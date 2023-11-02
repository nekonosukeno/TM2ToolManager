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
     public static partial class Archives
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
            public string fNameBytes {get; set;}
            public int headerLength {get; set;} // 0x40 (IM3) 0x30 (IM2)
            public int chunkSize {get; set;} // 0x20 from start of subheader (IM3 only)
            public int TM2offset {get; set;} // 0x24 from subheader for IM3; 0x20 for IM2
            public string unknowns {get; set;} // 3 Int32 starting 0x28 (IM3) 0x24 (IM2) (from subheader)
            public int TM2size {get; set;} // 0x44 from subheader (IM3 only)
            public string footer {get; set;} // Last 0x08 bytes of IM3 subheader
            // Texture animation configs stored as text files:
            public bool isText {get; set;}
            public bool CRLF {get; set;}
            public string txtFooter {get; set;}
        }

        public struct IMGinfo
        {
            public string IMGname {get; set;}
            public bool isIMG {get; set;}
            public string IMGtype {get; set;}
            public int totalFiles {get; set;}
        }

        public static IMGinfo getIMGinfo(string input, bool debug)
        {
            IMGinfo thisIMG = new IMGinfo();

            thisIMG.IMGname = input;
            
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
                    thisIMG.isIMG = true;
                    thisIMG.IMGtype = "IM2";
                    thisIMG.totalFiles = countIM2;
                    break;
                case 3362121: // 0 x 49 4D 33 00
                    thisIMG.isIMG = true;
                    thisIMG.IMGtype = "IM3";
                    thisIMG.totalFiles = countIM3;
                    break;
                default:
                    thisIMG.isIMG = false;
                    thisIMG.IMGtype = "";
                    thisIMG.totalFiles = 0;
                    break;
            }

            return thisIMG;
        }

        public static void ExtractIMG(IMGinfo givenIMG, bool debug)
        {
            string input = givenIMG.IMGname;
            int totalCount = givenIMG.totalFiles;
            string IMGtype = givenIMG.IMGtype;
            int TM2count = 0;
            int TXTcount = 0;
            // Let me tell you... sneaking text files into image archives then rarely having two text files...
            // WITH THE SAME NAME. But that wasn't enough! No! There's also sometimes TWO SETS of files...
            // WITH THE SAME NAME! So we're gonna convert this counter to part of the file name.
            int counter = 0;
            
            string slash = PathTool.GetSlashType();
            string cwd = Directory.GetCurrentDirectory();
            string IMGname = PathTool.BaseName(input);
            string IMGpath = input.Contains(slash) ? PathTool.rmName(input) : $"{cwd}{slash}";
            string unIMGdir = $"{IMGpath}{IMGname}_IMG";
            string unIMGjson = $"{unIMGdir}.json";
            
            if (!Directory.Exists(unIMGdir)) { Directory.CreateDirectory(unIMGdir); }
            
            // Reading in file and initiating subheader array
            IMGsubheader[] sub = new IMGsubheader[totalCount];
            byte[] buffer = File.ReadAllBytes(@input);

            // Constants for the offsets these values are stored at
            // These fields are shared between IM2 and IM3
            int TM2OffOff = IMGtype == "IM3" ? 0x24 : 0x20;
            int UnknownOffs = IMGtype == "IM3" ? 0x28 : 0x24;
            sub[0].headerLength = IMGtype == "IM3" ? 0x40 : 0x30;
            // These are IM3 exclusive fields
            int ChunkOff = 0x20;
            int TM2sizeOff = IMGtype == "IM3" ? 0x34 : 0x50; // IM2 - Used to calculate size
            int footerOff = 0x38;
            int checkTM2Size = 0x00;
            // For navigating the buffer
            int cursor = 0x10;
            int hold = cursor;
            
            for (int i = 0; i < totalCount; i++)
            {
                hold = cursor;
                
                // Setting our values
                // Core values related to IMG file
                if (i == 0)
                {
                    sub[i].IMGname = input;
                    sub[i].IMGtype = IMGtype;
                    sub[i].IMGheader = Convert.ToBase64String(buffer[0x00..0x10]);
                    sub[i].contains = totalCount;
                    sub[i].TotalHeaderSize = (totalCount * sub[0].headerLength) + 0x10;

                    if (debug)
                    {
                        Console.WriteLine($"\nIMG File Name: {PathTool.FileName(sub[0].IMGname)}");
                        Console.WriteLine($"IMG Version: {sub[0].IMGtype} with {Convert.ToString(totalCount)} textures");
                        Console.WriteLine($"Header Length: {HexTool.BigEndHex(sub[0].headerLength)}");
                    }
                }

                // Prep for our file name
                string n = counter < 10 ? $"0{Convert.ToString(counter)}" : $"{Convert.ToString(counter)}";
                // Common values
                byte[] nameBytes = buffer[hold..(hold + 0x20)];
                var getName = Transcoding.TextFromBytes(nameBytes, debug);
                string fileName = $"{getName.text}_{n}.tm2";
                if (fileName.StartsWith("#"))
                {
                    TXTcount += 1;
                    fileName = $"{getName.text}_{n}.cfg";
                }
                counter += 1;
                
                if (!String.IsNullOrEmpty(fileName)) {sub[i].fName = $"{unIMGdir}{slash}{fileName}";}
                sub[i].fNameBytes = Convert.ToBase64String(nameBytes);
                sub[i].TM2offset = HexTool.PS2intReader(buffer[(hold + TM2OffOff)..(hold + TM2OffOff + 0x04)]);
                byte[] unknownBytes = buffer[(hold + UnknownOffs)..(hold + UnknownOffs + 0x0C)];
                sub[i].unknowns = Convert.ToBase64String(unknownBytes);
                
                // Type dependant values
                if (IMGtype == "IM3")
                {
                    sub[i].chunkSize = HexTool.PS2intReader(buffer[(hold + ChunkOff)..(hold + ChunkOff + 0x04)]);
                    sub[i].TM2size = HexTool.PS2intReader(buffer[(hold + TM2sizeOff)..(hold + TM2sizeOff + 0x04)]);
                    sub[i].footer = Convert.ToBase64String(buffer[(hold + footerOff)..(hold + footerOff + 0x08)]);
                    
                    int TM2off = hold + TM2OffOff + 0x40;
                    // If last loop, check size offset is now end of file
                    checkTM2Size = i < (totalCount - 1) ?
                        HexTool.PS2intReader(buffer[TM2off..(TM2off + 0x04)]) : buffer.Length;
                }
                else // if (IMGtype == "IM2")
                {
                    checkTM2Size = HexTool.PS2intReader(buffer[(hold + TM2sizeOff)..(hold + TM2sizeOff + 0x04)]);
                    sub[i].TM2size = i < (totalCount - 1) ?
                        checkTM2Size - sub[i].TM2offset : buffer.Length - sub[i].TM2offset;
                }
                
                // Next file's start offset minus the start offset of the current file == current file's size
                // If last file, length of file minus length of current file.
                // Size is a calculated value for IM2 so this check only applies to IM3
                checkTM2Size -= sub[i].TM2offset;
                bool verifiedTM2 = checkTM2Size == sub[i].TM2size;
                
                // Debug info
                if (debug)
                {
                    Console.WriteLine($"\nCursor {i} @ {HexTool.BigEndHex(cursor)}\n");
                    if (IMGtype == "IM3") {Console.WriteLine($"Chunk Size: {HexTool.LitEndHex(sub[i].chunkSize)}");}
                    Console.WriteLine($"TM2 Offset: {HexTool.LitEndHex(sub[i].TM2offset)}");

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
                sub[i].CRLF = false;
                byte[] TM2data = buffer[sub[i].TM2offset..(sub[i].TM2offset + sub[i].TM2size)];
                if (TM2data.Length > 0x40)
                {
                    if (getName.text.StartsWith("#"))
                    {
                        sub[i].isText = true;
                        var getText = Transcoding.TextFromBytes(TM2data, debug);

                        if (getText.footer.Length > 0x00)
                        {
                            byte[] sjisBuffer = TM2data[0x00..(TM2data.Length - getText.footer.Length)];
                            byte[] txtBuffer = Transcoding.ToUTF8(sjisBuffer);
                                
                            File.WriteAllBytes(@sub[i].fName, txtBuffer);
                            sub[i].txtFooter = Convert.ToBase64String(getText.footer);
                            sub[i].CRLF = Transcoding.CheckCRLF(sjisBuffer);
                                
                            Array.Clear(txtBuffer);
                        }
                        else
                        {
                            byte[] U8data = Transcoding.ToUTF8(TM2data);
                            File.WriteAllBytes(@sub[i].fName, U8data);
                                
                            byte[] empty = new byte[0];
                            sub[i].txtFooter = Convert.ToBase64String(empty);
                                
                            Array.Clear(U8data);
                        }
                    }
                    else // if fileName is NOT a text file
                    {
                        TM2count += 1;
                        byte[] empty = new byte[0];
                        sub[i].txtFooter = Convert.ToBase64String(empty);
                        sub[i].isText = false;
                        File.WriteAllBytes(@sub[i].fName, TM2data);
                    }
                }
                else { Err.Invalid(fileName, 8); }
                    
                Array.Clear(TM2data);

                cursor += sub[0].headerLength;
            }
            
            // Writing JaySun
            JsonSerializerOptions indented = new JsonSerializerOptions
                { WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
                
            string JsonContent = JsonSerializer.Serialize(sub, indented);
            JsonContent += "\nIMG";
            File.WriteAllText(@unIMGjson, JsonContent);
                
            if (debug) { Console.WriteLine($"\nJSON has been saved to:\n {PathTool.rmName(unIMGjson)}"); }

            string tm2files = " TIM2 file(s) and ";
            string ending = " TXT file(s) have been extracted!";
            Console.WriteLine($"\n{Convert.ToString(TM2count)}{tm2files}{Convert.ToString(TXTcount)}{ending}");
        }

        public static int GetReIMGsize(string jsonContent, bool debug)
        {
            // Read the notes on GetRePAKsize() if you're confused about what's going on here
            IMGsubheader[] oldSubs = JsonSerializer.Deserialize<IMGsubheader[]>(jsonContent);

            int LengthNewIMG = 0;
            int txtFooterTotal = 0;

            // Calculating each new file's size
            for (int i = 0; i < oldSubs.Length; i++)
            {
                if (!File.Exists(oldSubs[i].fName)) { Err.NotFoundWhileRepacking(oldSubs[i].fName); }
                bool isTXT = oldSubs[i].isText;
                PAKsubheader empty = new PAKsubheader();
                var fInfo = PathTool.FileReader(oldSubs[i], empty, "IMG", debug);

                int fLength = fInfo.contents.Length;
                int txtFooterLength = fInfo.footer.Length;
                txtFooterTotal += txtFooterLength;
                
                if (debug)
                {
                    if (isTXT)
                    {
                        Console.WriteLine($"!!  Using Text Mode  !!");
                        Console.WriteLine($"Text Footer #{Convert.ToString(i)} Length: {Convert.ToString(txtFooterLength)}");
                    }
                    Console.WriteLine($"New File Length: {Convert.ToString(fLength)}");
                }
                LengthNewIMG += fLength;
            }
            LengthNewIMG += oldSubs[0].TotalHeaderSize + txtFooterTotal;

            string oldIMG = oldSubs[0].IMGname;
            string newIMGname = PathTool.BaseName(oldIMG) + "_mod" + PathTool.GetExt(oldIMG);
            long A_LOT = 2 * ((1024 * 1024 * 1024) - 1); // 2GB
            
            if ((LengthNewIMG < 0x100) || (LengthNewIMG >= A_LOT )) {Err.Invalid(newIMGname, 6);}
            
            if (debug) {Console.WriteLine($"New IMG Length: {HexTool.BigEndHex(LengthNewIMG)}\n");}
            
            return LengthNewIMG;
        }
        
        public static void RebuildIMG(string jsonContent, bool debug)
        {
            IMGsubheader[] oldSubs = JsonSerializer.Deserialize<IMGsubheader[]>(jsonContent);
            
            int LengthNewIMG = GetReIMGsize(jsonContent, debug);
            byte[] NewIMG = new byte[LengthNewIMG];
            int cursor = 0x00;
            int TM2cursor = oldSubs[0].TotalHeaderSize;
            string fSizeStr = $"\nNew IMG size: {HexTool.BigEndHex(Convert.ToInt32(NewIMG.Length))}";
            if (debug) { Console.WriteLine(fSizeStr); }
            
            byte[] IMGheader = Convert.FromBase64String(oldSubs[0].IMGheader);
            HexTool.InsertBytes(NewIMG, IMGheader, cursor);
            cursor += IMGheader.Length;
            
            for (int i = 0; i < oldSubs.Length; i++)
            {
                if (debug) {Console.WriteLine($"Cursor @ {HexTool.LitEndHex(cursor)}");}
                bool bIM3 = oldSubs[0].IMGtype == "IM3";
                
                // Both: Name
                byte[] nameBytes = Convert.FromBase64String(oldSubs[i].fNameBytes);
                HexTool.InsertBytes(NewIMG, nameBytes, cursor);
                cursor += 0x20;
                
                // IM3: Chunk
                if (bIM3)
                {
                    byte[] chunk = HexTool.IntToBytesPS2(oldSubs[i].chunkSize);
                    HexTool.InsertBytes(NewIMG, chunk, cursor);
                    cursor += chunk.Length;
                }
                
                // Both: TM2 Offset, Unknown Int32[3]
                PAKsubheader empty = new PAKsubheader();
                var importInfo = PathTool.FileReader(oldSubs[i], empty, "IMG", debug);
                int newTM2size = importInfo.contents.Length + importInfo.footer.Length;
                byte[] TM2off = HexTool.IntToBytesPS2(TM2cursor);
                
                HexTool.InsertBytes(NewIMG, TM2off, cursor);
                int printNewOff = TM2cursor;
                cursor += TM2off.Length;
                TM2cursor += newTM2size;
                
                byte[] unknownBytes = Convert.FromBase64String(oldSubs[i].unknowns);
                HexTool.InsertBytes(NewIMG, unknownBytes, cursor);
                cursor += unknownBytes.Length;
                
                // IM3: Size, footer
                if (bIM3)
                {
                    byte[] TM2size = HexTool.IntToBytesPS2(newTM2size);
                    HexTool.InsertBytes(NewIMG, TM2size, cursor);
                    cursor += TM2size.Length;
                
                    byte[] footer = Convert.FromBase64String(oldSubs[i].footer);
                    HexTool.InsertBytes(NewIMG, footer, cursor);
                    cursor += footer.Length;
                }

                if (debug)
                {
                    Console.WriteLine($"File: {PathTool.FileName(oldSubs[i].fName)}");
                    Console.WriteLine($"TM2 Cursor @ {HexTool.LitEndHex(printNewOff)}");
                    if (oldSubs[i].TM2size != newTM2size)
                    {
                        Console.WriteLine($"New Size: {HexTool.BigEndHex(newTM2size)}");
                        Console.WriteLine(printNewOff);
                    }
                }
            }
            
            // Adding files to new IMG
            for (int i = 0; i < oldSubs.Length; i++)
            {
                string newFile = PathTool.FileName(oldSubs[i].fName);
                
                if (File.Exists(oldSubs[i].fName))
                {
                    if (debug) { Console.WriteLine($"\nFile: \'{newFile}\' has been found!"); }

                    PAKsubheader empty = new PAKsubheader();
                    var importInfo = PathTool.FileReader(oldSubs[i], empty, "IMG", debug);
                    
                    HexTool.InsertBytes(NewIMG, importInfo.contents, cursor);
                    cursor += importInfo.contents.Length;
                    
                    if (importInfo.footer.Length > 0)
                    {
                        HexTool.InsertBytes(NewIMG, importInfo.footer, cursor);
                        cursor += importInfo.footer.Length;
                    }
                }
                else { Err.Invalid(newFile, 3); }
            }
            
            // Writing new IMG archive
            string oldIMG = oldSubs[0].IMGname;
            PathTool.WriteArchive(oldIMG, NewIMG);
        }
    }
}
