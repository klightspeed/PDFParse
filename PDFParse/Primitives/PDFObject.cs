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
        public int RefCount { get; set; }
        public List<PDFObject> ReferencedBy { get; private set; }
        public PDFObject Parent { get; set; }
        public List<PDFObject> Children { get; set; }

        public PDFStream Stream { get { return Value as PDFStream; } }
        public PDFDictionary Dict { get { return Value is PDFStream ? ((PDFStream)Value).Options : Value as PDFDictionary; } }

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

        public PDFObject(int id, int gen) : this()
        {
            this.ID = id;
            this.Version = gen;
        }

        public PDFObject AddRef(PDFObject parent)
        {
            this.RefCount++;
            this.ReferencedBy.Add(parent);
            return this;
        }

        public PDFObjRef ToObjRef()
        {
            return new PDFObjRef { ID = this.ID, Version = this.Version };
        }

        public static PDFObject Parse(Stack<IPDFToken> tokens)
        {
            PDFObject obj = new PDFObject();

            obj.Value = tokens.TryPop<IPDFElement>();

            tokens.Pop(PDFTokenType.StartObject);

            obj.Version = (int)tokens.Pop<PDFInteger>().Value;
            obj.ID = (int)tokens.Pop<PDFInteger>().Value;

            return obj;
        }

        public static IEnumerable<PDFObject> FromObjStm(PDFObject sobj)
        {
            PDFInteger nobjsv;
            if (sobj.TryGet("N", out nobjsv))
            {
                int nobjs = (int)nobjsv.Value;
                PDFObject[] objs = new PDFObject[nobjs];
                PDFTokenStack stack = new PDFTokenStack();
                PDFTokenizer tokens = new PDFTokenizer(new ByteStreamReader(sobj.Stream.Data));

                foreach (IPDFToken token in tokens)
                {
                    stack.ProcessToken(token);
                }

                for (int i = 0; i < nobjs; i++)
                {
                    objs[i] = new PDFObject { Value = stack.Pop<IPDFElement>() };
                }

                for (int i = 0; i < nobjs; i++)
                {
                    int offset = (int)stack.Pop<PDFInteger>().Value;
                    objs[i].ID = (int)stack.Pop<PDFInteger>().Value;
                    objs[i].Version = 0;
                }

                return objs;
            }
            else
            {
                return new PDFObject[0];
            }
        }

    }
}
