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
        public IPDFDictionary Root { get { return Trailer.TrailerDictionary.Dict.Get<IPDFDictionary>("Root"); } }
        public IPDFDictionary Info { get { return Trailer.TrailerDictionary.Dict.Get<IPDFDictionary>("Info"); } }
        public IPDFDictionary StructTreeRoot { get { return Root.Dict.Get<IPDFDictionary>("StructTreeRoot"); } }
        public Dictionary<long, PDFContentBlock> ContentBlocks { get; private set; }
        public PDFContentBlock StructTree { get; private set; }

        public IEnumerable<IPDFDictionary> Pages { get { return GetPages(Root.Dict.Get<IPDFDictionary>("Pages")); } }

        protected static IEnumerable<IPDFDictionary> GetPages(IPDFDictionary root)
        {
            PDFName type;
            IPDFList kids;
            if (root.Dict.TryGet("Type", out type) && type.Name == "Pages" && root.Dict.TryGet("Kids", out kids))
            {
                foreach (IPDFDictionary node in kids.List.OfType<IPDFDictionary>())
                {
                    foreach (IPDFDictionary leaf in GetPages(node))
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

        protected PDFContentBlock ProcessTreeNode(IPDFDictionary node, PDFName type)
        {
            PDFContentBlock cb = new PDFContentBlock
            {
                StartMarker = new PDFContentOperator
                {
                    Name = "BDC",
                    Arguments = new List<IPDFToken>
                    {
                        type,
                        node
                    }
                },
                Content = new List<PDFContentOperator>()
            };

            if (node.Dict.ContainsKey("K"))
            {
                IPDFElement K = node.Dict["K"];
                if (K is IPDFList && ((IPDFList)K).List != null)
                {
                    foreach (IPDFDictionary v in ((IPDFList)K).List.OfType<IPDFDictionary>())
                    {
                        PDFName vtype;
                        v.Dict.TryGet("S", out vtype);
                        cb.Content.Add(ProcessTreeNode(v, vtype));
                    }
                }
                else if (K is IPDFDictionary && ((IPDFDictionary)K).Dict != null)
                {
                    PDFName vtype;
                    ((IPDFDictionary)K).Dict.TryGet("S", out vtype);
                    cb.Content.Add(ProcessTreeNode((IPDFDictionary)K, vtype));
                }
                else if (K is PDFInteger)
                {
                    long mcid = ((PDFInteger)K).Value;

                    if (ContentBlocks.ContainsKey(mcid))
                    {
                        cb.Content.Add(ContentBlocks[mcid]);
                    }
                }
            }

            return cb;
        }

        protected void ProcessPageContentBlock(PDFContentBlock block)
        {
            if (block.BlockOptions != null && block.BlockOptions.ContainsKey("MCID"))
            {
                PDFInteger mcid;
                if (block.BlockOptions.TryGet<PDFInteger>("MCID", out mcid))
                {
                    ContentBlocks[mcid.Value] = block;
                }
            }

            foreach (PDFContentBlock cblock in block.Content.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(cblock);
            }
        }

        protected void ProcessPageContentBlocks(PDFContent content)
        {
            foreach (PDFContentBlock block in content.Tokens.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(block);
            }
        }

        protected static PDFDocument ParseDocument(ByteStreamReader reader)
        {
            PDFDocument doc = new PDFDocument();
            doc.Load(reader);

            foreach (IPDFDictionary page in doc.Pages)
            {
                IPDFElement content;
                if (page.Dict.TryGet<IPDFElement>("Contents", out content))
                {
                    IPDFList clist = content as IPDFList;
                    IPDFStream cstream = content as IPDFStream;
                    PDFContent pcontent = null;

                    if (clist != null && clist.List != null)
                    {
                        List<byte> data = new List<byte>();
                        foreach (IPDFStream elem in clist.List.OfType<IPDFStream>())
                        {
                            if (elem.Stream != null)
                            {
                                data.AddRange(elem.Stream.Data);
                            }
                        }
                        pcontent = new PDFContent(data.ToArray(), page);
                    }
                    else if (cstream != null && cstream.Stream != null)
                    {
                        pcontent = new PDFContent(cstream.Stream.Data, page);
                    }

                    if (pcontent != null)
                    {
                        page.Dict["PageContent"] = pcontent;
                        doc.ProcessPageContentBlocks(pcontent);
                    }
                }
            }

            doc.StructTree = doc.ProcessTreeNode(doc.StructTreeRoot, (PDFName)doc.StructTreeRoot.Dict["Type"]);

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
