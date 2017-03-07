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
        public IPDFDictionary StructTreeRoot { get { return Root.Dict.ContainsKey("StructTreeRoot") ? Root.Dict.Get<IPDFDictionary>("StructTreeRoot") : null; } }
        public Dictionary<PDFObjRef, Dictionary<long, PDFContentBlock>> ContentBlocks { get; private set; }
        public PDFContentBlock StructTree { get; private set; }

        public IEnumerable<IPDFDictionary> Pages { get { return GetPages(Root.Dict.Get<IPDFDictionary>("Pages")); } }

        public PDFDocument()
        {
            this.ContentBlocks = new Dictionary<PDFObjRef,Dictionary<long,PDFContentBlock>>();
        }

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
                    foreach (IPDFElement elem in ((IPDFList)K).List)
                    {
                        if (elem is IPDFDictionary)
                        {
                            IPDFDictionary v = (IPDFDictionary)elem;
                            PDFName vtype;
                            if (v.Dict.TryGet("S", out vtype))
                            {
                                PDFContentBlock blk = ProcessTreeNode(v, vtype);
                                blk.Parent = cb;
                                cb.Content.Add(blk);
                            }
                        }
                        else if (elem is PDFInteger)
                        {
                            long mcid = ((PDFInteger)elem).Value;
                            IPDFObjRef objref;

                            if (node.Dict.TryGet("Pg", out objref))
                            {
                                if (ContentBlocks.ContainsKey(objref.ObjRef))
                                {
                                    Dictionary<long, PDFContentBlock> blocksByMcid = ContentBlocks[objref.ObjRef];
                                    if (blocksByMcid.ContainsKey(mcid))
                                    {
                                        PDFContentBlock blk = blocksByMcid[mcid];
                                        blk.Parent = cb;
                                        cb.Content.Add(blk);
                                    }
                                }
                            }
                        }
                    }
                }
                else if (K is IPDFDictionary && ((IPDFDictionary)K).Dict != null)
                {
                    PDFName vtype;
                    if (((IPDFDictionary)K).Dict.TryGet("S", out vtype))
                    {
                        PDFContentBlock blk = ProcessTreeNode((IPDFDictionary)K, vtype);
                        blk.Parent = cb;
                        cb.Content.Add(blk);
                    }
                    else
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                }
                else if (K is PDFInteger && node.Dict.ContainsKey("Pg"))
                {
                    long mcid = ((PDFInteger)K).Value;
                    IPDFObjRef objref;

                    if (node.Dict.TryGet("Pg", out objref))
                    {
                        if (ContentBlocks.ContainsKey(objref.ObjRef))
                        {
                            Dictionary<long, PDFContentBlock> blocksByMcid = ContentBlocks[objref.ObjRef];
                            if (blocksByMcid.ContainsKey(mcid))
                            {
                                PDFContentBlock blk = blocksByMcid[mcid];
                                blk.Parent = cb;
                                cb.Content.Add(blk);
                            }
                        }
                    }
                }
            }

            return cb;
        }

        protected void ProcessPageContentBlock(PDFContentBlock block, Dictionary<long, PDFContentBlock> blocksByMcid)
        {
            if (block.BlockOptions != null && block.BlockOptions.ContainsKey("MCID"))
            {
                PDFInteger mcid;
                if (block.BlockOptions.TryGet<PDFInteger>("MCID", out mcid))
                {
                    blocksByMcid[mcid.Value] = block;
                }
            }

            foreach (PDFContentBlock cblock in block.Content.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(cblock, blocksByMcid);
            }
        }

        protected void ProcessPageContentBlocks(PDFContent content, Dictionary<long, PDFContentBlock> blocksByMcid)
        {
            foreach (PDFContentBlock block in content.Tokens.OfType<PDFContentBlock>())
            {
                ProcessPageContentBlock(block, blocksByMcid);
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
                        Dictionary<long, PDFContentBlock> blocks = new Dictionary<long, PDFContentBlock>();
                        doc.ContentBlocks[((IPDFObjRef)page).ObjRef] = blocks;
                        doc.ProcessPageContentBlocks(pcontent, blocks);
                    }
                }
            }

            IPDFDictionary stree = doc.StructTreeRoot;
            if (stree != null)
            {
                doc.StructTree = doc.ProcessTreeNode(stree, (PDFName)stree.Dict["Type"]);
            }

            return doc;
        }

        public static PDFDocument ParseDocument(string filename)
        {
            return ParseDocument(new ByteStreamReader(File.ReadAllBytes(filename)));
        }
    }
}
