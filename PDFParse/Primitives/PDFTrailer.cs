using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFTrailer : IPDFToken
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Trailer; } }
        public PDFXref Xref { get; set; }
        public PDFObject TrailerObject { get; set; }
        public PDFDictionary TrailerDictionary { get; set; }
        public long TrailerOffset { get; set; }

        public static PDFTrailer Parse(Stack<IPDFToken> tokens)
        {
            PDFTrailer trailer = new PDFTrailer();
            trailer.TrailerOffset = (long)tokens.Pop<PDFInteger>().Value;
            tokens.Pop(PDFTokenType.StartXref);

            if (tokens.Has(PDFTokenType.Dictionary))
            {
                trailer.TrailerDictionary = tokens.Pop<PDFDictionary>();
                trailer.Xref = tokens.Pop<PDFXref>();
            }
            else if (tokens.Has(PDFTokenType.Object))
            {
                trailer.TrailerObject = tokens.TryPop<PDFObject>();
                trailer.TrailerDictionary = trailer.TrailerObject.Dict;
            }
            else
            {
                throw new InvalidDataException();
            }

            return trailer;
        }

    }
}
