using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace PDFParse.Primitives
{
    public class PDFContentBlock : PDFContentOperator
    {
        public PDFContentOperator StartMarker { get; set; }
        public List<PDFContentOperator> Content { get; set; }
        public PDFContentBlock Parent { get; set; }

        public override string Text { get { return String.Join("", Content.OfType<PDFContentOperator>().Select(c => c.Text).Where(t => t != null)); } }

        public PointF TextPos
        {
            get
            {
                PointF pt = new PointF { X = float.MaxValue, Y = float.MaxValue };
                bool haspos = false;

                foreach (PDFContentOperator Tm in Content.Where(c => c.Name == "Tm" || c is PDFContentBlock))
                {
                    double x, y;

                    if (Tm is PDFContentBlock)
                    {
                        PointF p = ((PDFContentBlock)Tm).TextPos;
                        x = p.X;
                        y = p.Y;
                    }
                    else
                    {
                        x = ((IPDFValue<double>)Tm.Arguments[4]).Value;
                        y = ((IPDFValue<double>)Tm.Arguments[5]).Value;
                    }

                    if (x != 0 && y != 0)
                    {
                        if (x < pt.X)
                        {
                            pt.X = (float)x;
                        }

                        if (y < pt.Y)
                        {
                            pt.Y = (float)y;
                        }

                        haspos = true;
                    }
                }

                if (haspos)
                {
                    return pt;
                }
                else
                {
                    return PointF.Empty;
                }
            }
        }

        public double Rot
        {
            get
            {
                PDFContentOperator Tm = Content.FirstOrDefault(c => c.Name == "Tm");
                if (Tm != null)
                {
                    double rotx = ((IPDFValue<double>)Tm.Arguments[0]).Value;
                    double roty = ((IPDFValue<double>)Tm.Arguments[1]).Value;
                    double size = Math.Sqrt(rotx * rotx + roty * roty);

                    if (size < 0.1)
                    {
                        return 0;
                    }
                    else
                    {
                        return Math.Atan2(roty, rotx) * 180.0 / Math.PI;
                    }
                }
                else
                {
                    return 0;
                }
            }
        }

        public RectangleF CropBox
        {
            get
            {
                RectangleF cropbox = new RectangleF(float.MaxValue, float.MaxValue, -1, -1);
                bool hascropbox = false;

                foreach (PDFContentOperator W in Content.Where(c => c.Name == "W" || c.Name == "re" || c is PDFContentBlock))
                {
                    PDFContentOperator re = W;
                    double x, y, w, h;

                    if (W.Name == "W")
                    {
                        re = W.Arguments.OfType<PDFContentOperator>().FirstOrDefault(c => c.Name == "re");
                    }

                    if (W is PDFContentBlock)
                    {
                        PDFContentBlock blk = (PDFContentBlock)W;
                        RectangleF r = blk.CropBox;
                        x = r.X;
                        y = r.Y;
                        w = r.Width;
                        h = r.Height;
                    }
                    else
                    {
                        x = ((IPDFValue<double>)re.Arguments[0]).Value;
                        y = ((IPDFValue<double>)re.Arguments[1]).Value;
                        w = ((IPDFValue<double>)re.Arguments[2]).Value;
                        h = ((IPDFValue<double>)re.Arguments[3]).Value;
                    }

                    if (w != 0 && h != 0)
                    {
                        if (x < cropbox.X)
                        {
                            if (cropbox.Width > 0)
                            {
                                cropbox.Width += cropbox.X - (float)x;
                            }

                            cropbox.X = (float)x;
                        }

                        if (y < cropbox.Y)
                        {
                            if (cropbox.Height > 0)
                            {
                                cropbox.Height += cropbox.X - (float)x;
                            }

                            cropbox.Y = (float)y;
                        }

                        if (x + w > cropbox.X + cropbox.Width)
                        {
                            cropbox.Width = (float)((x - cropbox.X) + w);
                        }

                        if (y + h > cropbox.Y + cropbox.Height)
                        {
                            cropbox.Height = (float)((y - cropbox.Y) + h);
                        }

                        hascropbox = true;
                    }
                }

                if (hascropbox)
                {
                    return cropbox;
                }
                else
                {
                    return RectangleF.Empty;
                }
            }
        }

        public PDFName BlockType { get { return StartMarker.Arguments.OfType<PDFName>().FirstOrDefault(); } }
        public PDFDictionary BlockOptions { get { return StartMarker.Arguments.OfType<PDFDictionary>().FirstOrDefault(); } }


        public override string ToString()
        {
            PointF pos = TextPos;
            double rot = Rot;
            PDFName blocktype = BlockType;

            return (blocktype.Name ?? "") + " @(" + pos.X.ToString() + ", " + pos.Y.ToString() + ") Rot(" + rot.ToString() + "): " + Text;
            //return String.Join(" ", new[] { StartMarker.ToString() }.Concat(Content.Select(s => s.ToString())).Concat(Arguments.Select(s => s.ToString()))) + " " + Name;
        }
    }
}
