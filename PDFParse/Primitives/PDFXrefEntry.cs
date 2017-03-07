using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFXrefEntry : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.XrefEntry; } }
        public bool InUse { get; set; }
        public int ID { get; set; }
        public int Generation { get; set; }
        public long Offset { get; set; }

        public static PDFXrefEntry Parse(Stack<IPDFToken> tokens, PDFTokenType type)
        {
            PDFXrefEntry entry = new PDFXrefEntry();
            entry.InUse = type == PDFTokenType.XrefEntryInUse;
            entry.Generation = (int)tokens.Pop<PDFInteger>().Value;
            entry.Offset = (long)tokens.Pop<PDFInteger>().Value;
            return entry;
        }
    }
}
