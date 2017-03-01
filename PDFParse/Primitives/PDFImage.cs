using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.IO;

namespace PDFParse.Primitives
{
    public class PDFImage : PDFStream
    {
        public Image Image { get; set; }

        public PDFImage(PDFStream stream, PDFDictionary filterParams, PDFDictionary streamParams)
        {
            Data = stream.Data;

            try
            {
                Image = new Bitmap(new MemoryStream(Data));
            }
            catch
            {
            }
        }
    }
}
