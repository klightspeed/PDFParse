using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Drawing;
using Ionic.Zlib;
using PDFParse.Primitives;

namespace PDFParse
{

    public class PDFDocument : PDFDocumentBase
    {
        public Dictionary<string, List<PDFObject>> ObjectByType = new Dictionary<string, List<PDFObject>>();
        public PDFObject Root { get { return Trailer.TrailerDictionary.Get<PDFObject>("Root"); } }
        public PDFObject Info { get { return Trailer.TrailerDictionary.Get<PDFObject>("Info"); } }

        public IEnumerable<PDFObject> Pages { get { return GetPages(Root.Get<PDFObject>("Pages")); } }

        protected static IEnumerable<PDFObject> GetPages(PDFObject root)
        {
            PDFName type;
            if (root.Value == null || (root.TryGet("Type", out type) && type.Name == "Pages"))
            {
                foreach (PDFObject node in root.Children)
                {
                    foreach (PDFObject leaf in GetPages(node))
                    {
                        yield return leaf;
                    }
                }
            }
            else
            {
                yield return root;
            }
        }

        protected static PDFList ResolveReferences(PDFDocument doc, PDFObject parent, PDFList list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] != null)
                {
                    list[i] = ResolveReferences(doc, parent, list[i]);
                }
            }

            return list;
        }

        protected static PDFDictionary ResolveReferences(PDFDocument doc, PDFObject parent, PDFDictionary dict)
        {
            string[] keys = dict.Keys.ToArray();

            foreach (string key in keys)
            {
                dict[key] = ResolveReferences(doc, parent, dict[key]);
            }

            return dict;
        }

        protected static PDFObject ResolveReferences(PDFDocument doc, PDFObject parent, PDFObjRef objref)
        {
            if (doc.Objects.ContainsKey(objref))
            {
                PDFObject obj = doc.Objects[objref];
                obj.RefCount++;
                obj.ReferencedBy.Add(parent);
                return obj;
            }
            else
            {
                PDFObject obj = new PDFObject
                {
                    ID = objref.ID,
                    Version = objref.Version
                };
                doc.Objects[objref] = obj;
                obj.RefCount++;
                obj.ReferencedBy.Add(parent);
                return obj;
            }
        }

        protected static IPDFElement ResolveReferences(PDFDocument doc, PDFObject parent, IPDFElement val)
        {
            if (val is PDFList)
            {
                return ResolveReferences(doc, parent, (PDFList)val);
            }
            else if (val is PDFDictionary)
            {
                return ResolveReferences(doc, parent, (PDFDictionary)val);
            }
            else if (val is PDFObjRef)
            {
                return ResolveReferences(doc, parent, (PDFObjRef)val);
            }
            else
            {
                return val;
            }
        }
        
        protected static void ResolveReferences(PDFDocument doc, PDFObject obj)
        {
            obj.Value = ResolveReferences(doc, obj, obj.Value);
        }

        protected static void AddToObjectTypes(PDFDocument doc, PDFObject obj)
        {
            PDFDictionary dict;
            if (obj.TryGet(out dict))
            {
                PDFName type;
                if (dict.TryGet("Type", out type))
                {
                    if (!doc.ObjectByType.ContainsKey(type.Name))
                    {
                        doc.ObjectByType[type.Name] = new List<PDFObject>();
                    }
                    doc.ObjectByType[type.Name].Add(obj);

                }
            }
        }

        protected static byte[] ApplyDCTDecodeFilter(byte[] data, PDFDictionary filterParams, PDFDictionary streamParams)
        {
            streamParams["Mimetype"] = new PDFString { Value = "image/jpeg" };
            return data;
        }

        protected static byte[] ApplyFlateDecodeFilter(byte[] data, PDFDictionary filterParams, PDFDictionary streamParams)
        {
            using (ZlibStream strm = new ZlibStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                byte[] outdata = new byte[1048576];
                int pos = 0;
                int len = 0;

                do
                {
                    Array.Resize(ref outdata, pos + 1048576);
                    len = strm.Read(outdata, pos, outdata.Length - pos);
                    pos += len;
                }
                while (len > 0);

                Array.Resize(ref outdata, pos);

                return outdata;
            }
        }

        protected static byte[] ApplyFilter(byte[] data, string filter, PDFDictionary filterParams, PDFDictionary streamParams)
        {
            switch (filter)
            {
                case "FlateDecode": return ApplyFlateDecodeFilter(data, filterParams, streamParams);
                case "DCTDecode": return ApplyDCTDecodeFilter(data, filterParams, streamParams);
                default: throw new NotImplementedException();
            }
        }

        protected static void ApplyFilters(PDFObject obj)
        {
            PDFDictionary streamParams;
            if (obj.TryGet(out streamParams))
            {
                PDFInteger length;
                if (streamParams.TryGet("Length", out length))
                {
                    byte[] data = new byte[length.Value];
                    Array.Copy(obj.Stream.Data, data, data.Length);

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
                            data = ApplyFilter(data, filtername, filterparams, streamParams);
                        }
                    }

                    streamParams.Remove("Filter");
                    streamParams.Remove("Length");
                    streamParams.Remove("DecodeParms");

                    obj.Stream.Data = data;
                }
            }
        }

        protected static PDFDocument ParseDocument(ByteStreamReader reader)
        {
            PDFDocument doc = new PDFDocument();
            doc.Load(reader);

            foreach (PDFObject obj in doc.Objects.Values.ToArray())
            {
                ResolveReferences(doc, obj);
                AddToObjectTypes(doc, obj);
            }

            if (doc.Trailer.TrailerDictionary != null)
            {
                doc.Trailer.TrailerDictionary = ResolveReferences(doc, null, doc.Trailer.TrailerDictionary);
            }

            foreach (PDFObject obj in doc.Objects.Values)
            {
                PDFDictionary dict;
                if (obj.TryGet(out dict))
                {
                    PDFList kids;
                    if (dict.TryGet("Kids", out kids))
                    {
                        for (int i = 0; i < kids.Count; i++)
                        {
                            PDFObject kid;
                            if (kids.TryGet(i, out kid))
                            {
                                obj.Children.Add(kid);
                                kid.Parent = obj;
                            }
                        }
                    }
                    PDFObject parent;
                    if (dict.TryGet("Parent", out parent))
                    {
                        obj.Parent = parent;
                        parent.Children.Add(obj);
                    }
                }
            }

            foreach (PDFObject obj in doc.Objects.Values)
            {
                ApplyFilters(obj);
            }

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
