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

            obj.Stream = tokens.TryPop<PDFStream>();
            obj.Value = tokens.TryPop<IPDFElement>();

            tokens.Pop(PDFTokenType.StartObject);

            obj.Version = (int)tokens.Pop<PDFInteger>().Value;
            obj.ID = (int)tokens.Pop<PDFInteger>().Value;

            obj.ApplyFilters();

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

        protected void ApplyFilters()
        {
            PDFDictionary streamParams;
            if (TryGet(out streamParams))
            {
                PDFInteger length;
                if (streamParams.TryGet("Length", out length))
                {
                    PDFStream data = new PDFStream { Data = new byte[length.Value], Object = this };
                    Array.Copy(Stream.Data, data.Data, data.Data.Length);

                    PDFList filters;
                    PDFName filter;

                    if (streamParams.TryGet("Filter", out filter))
                    {
                        filters = new PDFList { filter };
                    }
                    else
                    {
                        streamParams.TryGet("Filter", out filters);
                    }

                    if (filters != null)
                    {
                        PDFDictionary decodeparams;
                        PDFList decodeparamslist;

                        if (streamParams.TryGet("DecodeParams", out decodeparams))
                        {
                            decodeparamslist = new PDFList { decodeparams };
                        }
                        else
                        {
                            streamParams.TryGet("DecodeParams", out decodeparamslist);

                            if (decodeparamslist == null)
                            {
                                decodeparamslist = new PDFList();
                            }
                        }

                        for (int i = 0; i < filters.Count; i++)
                        {
                            string filtername = filters.Get<PDFName>(i).Name;
                            PDFDictionary filterparams;
                            decodeparamslist.TryGet<PDFDictionary>(i, out filterparams);
                            data = data.ApplyFilter(filtername, filterparams, streamParams);
                        }
                    }

                    streamParams.Remove("Filter");
                    streamParams.Remove("Length");
                    streamParams.Remove("DecodeParms");

                    Stream = data;
                }
            }
        }
    }
}
