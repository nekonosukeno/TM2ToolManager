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
    public static class HexTool
    {
        // Reads byte array, corrects endian, and returns int32
        public static int PS2intReader(byte[] raw)
        {
            // for (int i = 0; i < raw.Length; i++) { Console.WriteLine($"0 x {BitConverter.ToString(new[] {raw[i]})} "); }
            
            // Numeric values stored in PS2 binary files are always little endian
            if (!BitConverter.IsLittleEndian) { Array.Reverse(raw); }
            return BitConverter.ToInt32(raw);
        }
        
        public static string LitEndHex(int offset)
        // prints the given int32 in little endian because...reasons??
        {
            byte[] getHex = BitConverter.GetBytes(offset);
            if (!BitConverter.IsLittleEndian)
            {
                // Console.WriteLine("BigEndian");
                Array.Reverse(getHex);
            }

            string result = "0 x ";
            foreach (byte b in getHex)
            {
                result += BitConverter.ToString(new[]{b}) + " ";
            }
            return result;
        }
        
        public static string BigEndHex(int number)
        // Prints int32 to big endian for other reasons.
        {
            byte[] getHex = BitConverter.GetBytes(number);
            if (BitConverter.IsLittleEndian) { Array.Reverse(getHex); }

            string LeadIn = "0x";
            string result = "";
            for (int i = 0; i < getHex.Length; i++)
            {
                result += BitConverter.ToString( new[] { getHex[i] } );
            }

            while (result.StartsWith("0"))
            {
                // if (result.StartsWith("0")) { result = result.Substring(1); }
                result = result.Substring(1);
            }
            
            if (result.Length == 0) { result = "00"; }
            if (result.Length == 1) { result = "0" + result; }
            
            result = LeadIn + result;
            return result;
        }
        
        public static int IndexOfByte(byte[] bytes, string find)
            // finds index of specific byte cause this is a pain to do
        {
            find = find.Replace(" ", "");
            if (find.StartsWith("0x")) { find = find.Substring(2); }
            
            string[] hexString = new string[bytes.Length];
            int result = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString[i] = BitConverter.ToString( new[] {bytes[i]} );
            }
            result = Array.IndexOf(hexString, find);
            return result;
        }

        public static void InsertBytes(byte[] input, byte[] append, int cursor)
        {
            foreach (byte b in append)
            {
                input[cursor] = b;
                cursor += 1;
            }
        }

        public static byte[] IntToBytesPS2(int input)
        {
            byte[] getHex = BitConverter.GetBytes(input);
            if (!BitConverter.IsLittleEndian)
            {
                // Console.WriteLine("BigEndian");
                Array.Reverse(getHex);
            }
            return getHex;
        }
    }
}
