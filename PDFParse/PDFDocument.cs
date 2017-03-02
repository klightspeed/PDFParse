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

        protected void ApplyFilters(PDFObject obj)
        {
            PDFDictionary streamParams;
            if (obj.TryGet(out streamParams))
            {
                PDFInteger length;
                if (streamParams.TryGet("Length", out length))
                {
                    PDFStream data = new PDFStream { Data = new byte[length.Value] };
                    Array.Copy(obj.Stream.Data, data.Data, data.Data.Length);

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

                    obj.Stream = data;
                }
            }
        }

        protected static PDFDocument ParseDocument(ByteStreamReader reader)
        {
            PDFDocument doc = new PDFDocument();
            doc.Load(reader);

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
                doc.ApplyFilters(obj);
            }

            foreach (PDFObject page in doc.Pages)
            {
                IPDFElement content;
                if (page.TryGet<IPDFElement>("Contents", out content))
                {
                    if (content is PDFList)
                    {
                        foreach (IPDFElement elem in (PDFList)content)
                        {
                            if (elem is PDFObject)
                            {
                                PDFObject contentobj = (PDFObject)elem;
                                contentobj.Stream = new PDFContent(contentobj.Stream);
                            }
                        }
                    }
                    else if (content is PDFObject)
                    {
                        PDFObject contentobj = (PDFObject)content;
                        contentobj.Stream = new PDFContent(contentobj.Stream);
                    }
                }
            }

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
