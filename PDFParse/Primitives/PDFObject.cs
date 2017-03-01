using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFObject : IPDFElement
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Object; } }
        public int ID { get; set; }
        public int Version { get; set; }
        public IPDFElement Value { get; set; }
        public PDFStream Stream { get; set; }
        public int RefCount { get; set; }
        public List<PDFObject> ReferencedBy { get; private set; }
        public string StreamString { get { return Stream == null ? null : ISO88591.GetString(Stream.Data); } }
        public PDFObject Parent { get; set; }
        public List<PDFObject> Children { get; set; }

        public bool TryGet<T>(out T val)
            where T : IPDFElement
        {
            if (Value != null && Value is T)
            {
                val = (T)Value;
                return true;
            }
            else
            {
                val = default(T);
                return false;
            }
        }

        public T Get<T>()
            where T : IPDFElement
        {
            T val;
            if (TryGet(out val))
            {
                return val;
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        public bool TryGet<T>(string name, out T val)
            where T : IPDFElement
        {
            PDFDictionary dict;
            if (TryGet(out dict) && dict.TryGet(name, out val))
            {
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
            if (TryGet(name, out val))
            {
                return val;
            }
            else
            {
                throw new InvalidDataException();
            }
        }

        public PDFObject()
        {
            ReferencedBy = new List<PDFObject>();
            Children = new List<PDFObject>();
        }

        public PDFObjRef ToObjRef()
        {
            return new PDFObjRef { ID = this.ID, Version = this.Version };
        }

        public static PDFObject Parse(Stack<IPDFToken> tokens)
        {
            PDFObject obj = new PDFObject();

            obj.Stream = tokens.TryPop<PDFStream>();
            obj.Value = tokens.TryPop<IPDFElement>();

            tokens.Pop(PDFTokenType.StartObject);

            obj.Version = (int)tokens.Pop<PDFInteger>().Value;
            obj.ID = (int)tokens.Pop<PDFInteger>().Value;

            return obj;
        }
        
    }
}
