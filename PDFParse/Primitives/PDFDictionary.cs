using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFDictionary : Dictionary<string, IPDFElement>, IPDFElement, IPDFDictionary
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Dictionary; } }

        public PDFDictionary() { }

        public PDFDictionary(IEnumerable<KeyValuePair<string, IPDFElement>> collection)
        {
            foreach (KeyValuePair<string, IPDFElement> kvp in collection)
            {
                base.Add(kvp.Key, kvp.Value);
            }
        }

        PDFDictionary IPDFDictionary.Dict { get { return this; } }

        public bool TryGet<T>(string name, out T val)
            where T : IPDFElement
        {
            if (this.ContainsKey(name) && this[name] is T)
            {
                val = (T)this[name];
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
        }

        public T Get<T>(string name)
            where T : IPDFElement
        {
            T val;
            if (TryGet<T>(name, out val))
            {
                return val;
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        public static PDFDictionary Parse(Stack<IPDFToken> tokens)
        {
            Stack<KeyValuePair<string, IPDFElement>> kvps = new Stack<KeyValuePair<string, IPDFElement>>();

            while (!tokens.Has(PDFTokenType.StartDictionary))
            {
                IPDFElement elem = tokens.Pop<IPDFElement>();
                PDFName name = tokens.Pop<PDFName>();
                kvps.Push(new KeyValuePair<string, IPDFElement>(name.Name, elem));
            }

            tokens.Pop(PDFTokenType.StartDictionary);

            return new PDFDictionary(kvps);
        }

    }
}
