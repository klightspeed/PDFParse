using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFXref : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Xref; } }
        public int Start { get; set; }
        public int Count { get; set; }
        public List<PDFXrefEntry> Entries { get; set; }

        public static PDFXref Parse(Stack<IPDFToken> tokens)
        {
            PDFXref xref = new PDFXref();
            Stack<PDFXrefEntry> entries = new Stack<PDFXrefEntry>();

            PDFXrefEntry entry = null;
            while (tokens.TryPop<PDFXrefEntry>(out entry))
            {
                entries.Push(entry);
            }

            xref.Entries = entries.ToList();
            xref.Count = (int)tokens.Pop<PDFInteger>().Value;
            xref.Start = (int)tokens.Pop<PDFInteger>().Value;
            tokens.Pop(PDFTokenType.Xref);

            return xref;
        }

    }
}
