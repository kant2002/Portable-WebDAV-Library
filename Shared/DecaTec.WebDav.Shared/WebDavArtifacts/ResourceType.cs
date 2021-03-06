﻿using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace DecaTec.WebDav.WebDavArtifacts
{
    /// <summary>
    /// Class representing an 'resourcetype' XML element for WebDAV communication.
    /// </summary>
    [DataContract]
    [DebuggerStepThrough]
    [XmlType(TypeName = "resourcetype", Namespace = "DAV:")]
    [XmlRoot(Namespace = "DAV:", IsNullable = false)]
    public class ResourceType
    {
        private Collection collectionField;

        /// <summary>
        /// Gets or sets the Collection.
        /// </summary>
        [XmlElement(ElementName = "collection")]
        public Collection Collection
        {
            get
            {
                return this.collectionField;
            }
            set
            {
                this.collectionField = value;
            }
        }
    }
}
