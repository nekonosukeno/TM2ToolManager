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
    // Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    public static class Transcoding
    {
        // public static Encoding sjis = Encoding.GetEncoding("Shift_JIS");
        // public static Encoding utf8 = Encoding.UTF8;
        
        // Gets SJIS string from byte array, returns empty if first bytes are null
        public static (string text, byte[] footer) TextFromBytes(byte[] input, bool debug)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");

            string text = "";
            byte[] empty = new byte[0];
            int firstNull = HexTool.IndexOfByte(input, "0x00");
            if ((firstNull < 3) && (firstNull >= 0)) { Err.NullStart(); }
            if (firstNull == -1) { firstNull = input.Length; }

            byte[] textBytes = input[0x00..(firstNull)];

            try { text = sjis.GetString(textBytes); }
            catch (Exception) { if (debug) {Err.Encoder(textBytes);} }
            
            byte[] footer = firstNull > 0x03 ? input[firstNull..input.Length] : empty;
            
            // Debug
            if (debug && (firstNull < 0x21)) {Console.WriteLine($"First Null = {HexTool.BigEndHex(firstNull)} \"{text}\"");}
            else if (debug && (firstNull > 0x20)) {Console.WriteLine($"First Null = {HexTool.BigEndHex(firstNull)}");}
            
            return (text, footer);
        }
        
        // Convert Shift-JIS byte array to UTF-8 bytes
        public static byte[] ToUTF8(byte[] SJISbytes)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");
            Encoding utf8 = Encoding.UTF8;
        
            // Getting U8 String, then U8 Bytes
            string U8string = sjis.GetString(SJISbytes);
            byte[] U8hex = utf8.GetBytes(U8string);

            return U8hex;
        }

        // Convert UTF-8 byte array to Shift-JIS bytes
        public static byte[] ToSJIS(byte[] UTF8bytes)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding sjis = Encoding.GetEncoding("Shift_JIS");
            Encoding utf8 = Encoding.UTF8;
        
            // Getting U8 String, then SJIS Bytes
            string U8string = utf8.GetString(UTF8bytes);
            byte[] SJIShex = sjis.GetBytes(U8string);

            return SJIShex;
        }
        
        
        // Checks if byte[] ends with a carriage return; line feed (0 x 0D 0A)
        public static bool CheckCRLF(byte[] stream)
        {
            int antipenultimate = stream.Length - 2;
            int EoF = stream.Length;

            byte[] Last2B = stream[antipenultimate..EoF];
            byte[] getInt = { Last2B[1], Last2B[0], 0x00, 0x00 };
            int check = HexTool.PS2intReader(getInt);
            
            // Console.WriteLine($"CRLF Check: {Convert.ToString(check)}");

            bool bCRLF = check == 3338;
            return bCRLF;
        }
        
        // I store what type of repack to do as a code in the last line of the json
        // This reads that comment, removes it, reads the json, and  replaces it
        public static (string contents, string repackType) JSONreader(string file)
        {
            string[] lines = File.ReadAllLines(file);
            string repackType = lines[lines.Length - 1];
            File.WriteAllLines(file, lines.Take(lines.Length - 1).ToArray());
            
            string contents = "";
            using ( StreamReader readJSON = new StreamReader(file) )
            {
                contents += readJSON.ReadToEnd();
            }

            string oldContents = contents + repackType;
            File.WriteAllText(file, oldContents);

            return (contents, repackType);
        }
    }
}