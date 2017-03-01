using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFList : List<IPDFElement>, IPDFElement
    {
        public PDFTokenType TokenType { get { return PDFTokenType.List; } }

        public PDFList() { }

        public PDFList(IEnumerable<IPDFElement> elems)
            : base(elems)
        {
        }

        public bool TryGet<T>(int index, out T val)
            where T : IPDFElement
        {
            if (index < this.Count && this[index] is T)
            {
                val = (T)this[index];
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
        }

        public T Get<T>(int index)
            where T : IPDFElement
        {
            T val;
            if (TryGet<T>(index, out val))
            {
                return val;
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        public static PDFList Parse(Stack<IPDFToken> tokens)
        {
            Stack<IPDFElement> list = new Stack<IPDFElement>();

            while (!tokens.Has(PDFTokenType.StartList))
            {
                list.Push(tokens.Pop<IPDFElement>());
            }

            tokens.Pop(PDFTokenType.StartList);

            return new PDFList(list);
        }
    }
}
