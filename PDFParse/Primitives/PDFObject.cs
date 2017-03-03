using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PDFParse.Primitives
{
    public class PDFObject : IPDFElement, IPDFDictionary, IPDFList, IPDFStream, IPDFObjRef
    {
        public PDFTokenType TokenType { get { return PDFTokenType.Object; } }
        public int ID { get; set; }
        public int Version { get; set; }
        public IPDFElement Value { get; set; }
        public int RefCount { get; set; }
        public List<PDFObject> ReferencedBy { get; private set; }

        PDFStream IPDFStream.Stream { get { return Value as PDFStream; } }
        PDFDictionary IPDFDictionary.Dict { get { return Value is PDFStream ? ((PDFStream)Value).Options : Value as PDFDictionary; } }
        PDFList IPDFList.List { get { return Value as PDFList; } }
        PDFObjRef IPDFObjRef.ObjRef { get { return ToObjRef(); } }

        public PDFObject()
        {
            ReferencedBy = new List<PDFObject>();
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

        public static IEnumerable<PDFObject> FromObjStm(PDFStream sobj)
        {
            PDFInteger nobjsv;
            if (sobj.Options.TryGet("N", out nobjsv))
            {
                int nobjs = (int)nobjsv.Value;
                PDFObject[] objs = new PDFObject[nobjs];
                PDFTokenStack stack = new PDFTokenStack();
                PDFTokenizer tokens = new PDFTokenizer(new ByteStreamReader(sobj.Data));

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
