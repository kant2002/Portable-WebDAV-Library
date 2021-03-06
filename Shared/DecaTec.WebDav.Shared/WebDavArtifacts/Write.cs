﻿using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DecaTec.WebDav.WebDavArtifacts
{
    /// <summary>
    /// Class representing an 'write' XML element for WebDAV communication.
    /// </summary>
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = "write", Namespace = "DAV:")]
    [XmlRoot(Namespace = "DAV:", IsNullable = false)]
    public class Write
    {
    }
}
