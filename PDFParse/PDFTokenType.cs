using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDFParse
{
    public enum PDFTokenType
    {
        None,
        Comment,
        Integer,
        Double,
        Boolean,
        Null,
        Name,
        String,
        HexString,
        StartList,
        EndList,
        List,
        StartDictionary,
        EndDictionary,
        Dictionary,
        Keyword,
        ObjectRef,
        StartObject,
        EndObject,
        Object,
        Stream,
        Xref,
        Trailer,
        StartXref,
        XrefEntryInUse,
        XrefEntryFree,
        XrefEntry,
        Version,
        EOF
    }
}
