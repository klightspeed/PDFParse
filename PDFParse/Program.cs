using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse
{
    class Program
    {
        static void Main(string[] args)
        {
            string docfile = args[0];
            PDFDocument doc = PDFDocument.ParseDocument(docfile);
        }
    }
}
