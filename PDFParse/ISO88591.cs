using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public class ISO88591
    {
        public static byte[] GetBytes(string str)
        {
            return str.Select(c => (byte)c).ToArray();
        }

        public static string GetString(byte[] data)
        {
            return new String(data.Select(c => (char)c).ToArray());
        }
    }
}
