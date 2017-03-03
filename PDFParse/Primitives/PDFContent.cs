using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFContent : PDFStream
    {
        public List<IPDFToken> Tokens { get; set; }

        public string Text { get { return String.Join("\n", Tokens.OfType<PDFContentOperator>().Select(c => c.Text).Where(t => t != null)); } }

        public PDFContent(byte[] data)
        {
            this.Data = data;
            ByteStreamReader reader = new ByteStreamReader(Data);
            PDFTokenizer tokenizer = new PDFTokenizer(reader);

            PDFContentTokenStack stack = new PDFContentTokenStack();

            foreach (IPDFToken token in tokenizer)
            {
                stack.ProcessToken(token);
            }

            this.Tokens = stack.Reverse().ToList();
        }
    }
}
