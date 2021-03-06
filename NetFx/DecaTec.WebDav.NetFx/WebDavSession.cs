﻿using DecaTec.WebDav.WebDavArtifacts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DecaTec.WebDav
{
    /// <summary>
    /// Class for WebDAV sessions.
    /// </summary>
    /// <remarks>
    /// <para>This class acts as an abstraction layer between the application and the <see cref="DecaTec.WebDav.WebDavClient"/>, which is used to communicate with the WebDAV server.</para>
    /// <para>If you want to communicate with the WebDAV server directly, you should use the <see cref="DecaTec.WebDav.WebDavClient"/>.</para>
    /// <para>The WebDavSession can be used with a base URL/<see cref="System.Uri"/>. If such a base URL/<see cref="System.Uri"/> is specified, all subsequent operations involving an 
    /// URL/<see cref="System.Uri"/> will be relative on this base URL/<see cref="System.Uri"/>.
    /// If no base URL/<see cref="System.Uri"/> is specified, all operations has the be called with an absolute URL/<see cref="System.Uri"/>.</para>
    /// </remarks>
    /// <example>See the following code to list the content of a directory with the WebDavSession:
    /// <code>
    /// // You have to add a reference to DecaTec.WebDav.NetFx.dll.
    /// //
    /// // Specify the user credentials and use it to create a WebDavSession instance.
    /// var credentials = new NetworkCredential("MyUserName", "MyPassword");
    /// var webDavSession = new WebDavSession(@"http://www.myserver.com/webdav/", credentials);
    /// var items = await webDavSession.ListAsync(@"MyFolder/");
    ///
    /// foreach (var item in items)
    /// {
    ///     Console.WriteLine(item.Name);
    /// }
    /// 
    /// // Dispose the WebDavSession when it is not longer needed.
    /// webDavSession.Dispose();
    /// </code>
    /// <para></para>
    /// See the following code which uses locking with a WebDavSession:
    /// <code>
    /// // Specify the user credentials and use it to create a WebDavSession instance.
    /// var credentials = new NetworkDavCredential("MyUserName", "MyPassword");
    /// var webDavSession = new WebDavSession(@"http://www.myserver.com/webdav/", credentials);
    /// await webDavSession.LockAsync(@"Test/");
    ///
    /// // Create new folder and delete it.
    /// // You DO NOT have to care about that the folder is locked (i.e. you do not have to submit a lock token).
    /// // This is all handled by the WebDavSession itself.
    /// await webDavSession.CreateDirectoryAsync("MyFolder/NewFolder");
    /// await webDavSession.DeleteAsync("MyFolder/NewFolder");
    ///
    /// // Unlock the folder again.
    /// await webDavSession.UnlockAsync(@"MyFolder/");
    ///
    /// // You should always call Dispose on the WebDavSession when it is not longer needed.
    /// // During Dispose, all locks held by the WebDavSession will be automatically unlocked.
    /// webDavSession.Dispose();
    /// </code>
    /// </example>
    /// <seealso cref="DecaTec.WebDav.WebDavClient"/>
    public partial class WebDavSession : IDisposable
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of WebDavSession with a default <see cref="System.Net.Http.HttpClientHandler"/>.
        /// </summary>
        /// <param name="networkCredential">The <see cref="System.Net.NetworkCredential"/> to use.</param>
        public WebDavSession(NetworkCredential networkCredential)
            : this(new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }

        /// <summary>
        /// Initializes a new instance of WebDavSession with the given base URL and a default <see cref="System.Net.Http.HttpClientHandler"/>.
        /// </summary>
        /// <param name="baseUrl">The base URL to use for this WebDavSession.</param>
        /// <param name="networkCredential">The <see cref="System.Net.NetworkCredential"/> to use.</param>
        public WebDavSession(string baseUrl, NetworkCredential networkCredential)
            : this(new Uri(baseUrl), new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }

        /// <summary>
        /// Initializes a new instance of WebDavSession with the given base URI and a default <see cref="System.Net.Http.HttpClientHandler"/>.
        /// </summary>
        /// <param name="baseUri">The base URI to use for this WebDavSession.</param>
        /// <param name="networkCredential">The <see cref="System.Net.NetworkCredential"/> to use.</param>
        public WebDavSession(Uri baseUri, NetworkCredential networkCredential)
            : this(baseUri, new HttpClientHandler() { PreAuthenticate = true, Credentials = networkCredential })
        {
        }

        /// <summary>
        /// Initializes a new instance of WebDavSession with the <see cref="System.Net.Http.HttpMessageHandler"/> specified.
        /// </summary>
        /// <param name="httpMessageHandler">The <see cref="System.Net.Http.HttpMessageHandler"/> to use with this WebDavSession.</param>
        /// <remarks>If credentials are needed, these are part of the <see cref="System.Net.Http.HttpMessageHandler"/> and are specified with it.</remarks>
        public WebDavSession(HttpMessageHandler httpMessageHandler)
            : this(string.Empty, httpMessageHandler)
        {
        }

        /// <summary>
        /// Initializes a new instance of WebDavSession with the given base URL and the <see cref="System.Net.Http.HttpMessageHandler"/> specified.
        /// </summary>
        /// <param name="baseUrl">The base URL to use for this WebDavSession.</param>
        /// <param name="httpMessageHandler">The <see cref="System.Net.Http.HttpMessageHandler"/> to use with this WebDavSession.</param>
        /// <remarks>If credentials are needed, these are part of the <see cref="System.Net.Http.HttpMessageHandler"/> and are specified with it.</remarks>
        public WebDavSession(string baseUrl, HttpMessageHandler httpMessageHandler)
            : this(string.IsNullOrEmpty(baseUrl) ? null : new Uri(baseUrl), httpMessageHandler)
        {
        }

        /// <summary>
        /// Initializes a new instance of WebDavSession with the given base URI and the <see cref="System.Net.Http.HttpMessageHandler"/> specified.
        /// </summary>
        /// <param name="baseUri">The base URI to use for this WebDavSession.</param>
        /// <param name="httpMessageHandler">The <see cref="System.Net.Http.HttpMessageHandler"/> to use with this WebDavSession.</param>
        /// <remarks>If credentials are needed, these are part of the <see cref="System.Net.Http.HttpMessageHandler"/> and are specified with it.</remarks>
        public WebDavSession(Uri baseUri, HttpMessageHandler httpMessageHandler)
        {
            this.permanentLocks = new ConcurrentDictionary<Uri, PermanentLock>();
            this.webDavClient = CreateWebDavClient(httpMessageHandler);
            this.BaseUri = baseUri;
        }

        #endregion Constructor

        #region Properties

        /// <summary>
        /// Gets or sets the <see cref="System.Net.IWebProxy"/> to use with this WebDavSession.
        /// </summary>
        public IWebProxy WebProxy
        {
            get;
            set;
        }

        #endregion Properties

        #region Public methods

        #region Download file

        /// <summary>
        ///  Downloads a file from the URI specified.
        /// </summary>
        /// <param name="uri">The URI of the file to download.</param>
        /// <param name="localStream">The stream to save the file to.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<bool> DownloadFileAsync(Uri uri, Stream localStream)
        {
            uri = UrlHelper.GetAbsoluteUriWithTrailingSlash(this.BaseUri, uri);
            var response = await this.webDavClient.GetAsync(uri);

            if (response.Content != null)
            {
                try
                {
                    var contentStream = await response.Content.ReadAsStreamAsync();
                    await contentStream.CopyToAsync(localStream);
                    return true;
                }
                catch (IOException)
                {
                    return false;
                }
            }
            else
                return false;
        }

        #endregion Download file

        #region List

        /// <summary>
        /// Retrieves a list of files and directories of the directory at the URI specified.
        /// </summary>
        /// <param name="uri">The URI of the directory which content should be listed.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<IList<WebDavSessionListItem>> ListAsync(Uri uri)
        {
            uri = UrlHelper.GetAbsoluteUriWithTrailingSlash(this.BaseUri, uri);

            // Do not use an allprop here because some WebDav servers will not return the expected results when using allprop.
            var propFind = PropFind.CreatePropFindWithEmptyProperties("ishidden", "displayname", "name", "getcontenttype", "creationdatespecified", "creationdate", "resourcetype", "getLastmodified", "getcontentlength");
            var response = await this.webDavClient.PropFindAsync(uri, WebDavDepthHeaderValue.One, propFind);

            if (response.StatusCode != WebDavStatusCode.MultiStatus)
                throw new WebDavException(string.Format("Error while executing ListAsync (wrong response status code). Expected status code: 207 (MultiStatus); actual status code: {0} ({1})", (int)response.StatusCode, response.StatusCode));

            var multistatus = await WebDavResponseContentParser.ParseMultistatusResponseContentAsync(response.Content);

            var itemList = new List<WebDavSessionListItem>();

            foreach (var responseItem in multistatus.Response)
            {
                var webDavSessionItem = new WebDavSessionListItem();

                Uri href = null;

                if (!string.IsNullOrEmpty(responseItem.Href))
                {
                    if (Uri.TryCreate(responseItem.Href, UriKind.RelativeOrAbsolute, out href))
                        webDavSessionItem.Uri = href;
                }

                // Skip the folder which contents were requested, only add children.
                if (href != null && uri.ToString().EndsWith(href.ToString(), StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var item in responseItem.Items)
                {
                    var propStat = item as Propstat;

                    if (propStat == null)
                        continue;

                    // Do not add hidden items.
                    if (propStat.Prop.IsHidden == "1")
                        continue;

                    // Naming priority:
                    // 1. displayname
                    // 2. name
                    // 3. (part of) URI.
                    webDavSessionItem.Name = propStat.Prop.DisplayName;

                    if (string.IsNullOrEmpty(webDavSessionItem.Name))
                        webDavSessionItem.Name = propStat.Prop.Name;

                    if (string.IsNullOrEmpty(webDavSessionItem.Name) && href != null)
                        webDavSessionItem.Name = href.ToString().Split('/').Last(x => !string.IsNullOrEmpty(x));

                    if (!string.IsNullOrEmpty(propStat.Prop.GetContentType))
                        webDavSessionItem.ContentType = propStat.Prop.GetContentType;

                    if (propStat.Prop.CreationDateSpecified && !string.IsNullOrEmpty(propStat.Prop.CreationDate))
                        webDavSessionItem.Created = DateTime.Parse(propStat.Prop.CreationDate, CultureInfo.InvariantCulture);

                    webDavSessionItem.IsDirectory = false;

                    if (propStat.Prop.ResourceType != null)
                        webDavSessionItem.IsDirectory = propStat.Prop.ResourceType.Collection != null;

                    if (!string.IsNullOrEmpty(propStat.Prop.GetLastModified))
                        webDavSessionItem.Modified = DateTime.Parse(propStat.Prop.GetLastModified, CultureInfo.InvariantCulture);

                    if (!string.IsNullOrEmpty(propStat.Prop.GetContentLength))
                        webDavSessionItem.Size = long.Parse(propStat.Prop.GetContentLength, CultureInfo.InvariantCulture);
                }

                itemList.Add(webDavSessionItem);
            }

            return itemList;
        }

        #endregion List

        #region Upload file

        /// <summary>
        /// Uploads a file to the URI specified.
        /// </summary>
        /// <param name="uri">The URI of the file to upload.</param>
        /// <param name="localStream">The stream containing the file to upload.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public async Task<bool> UploadFileAsync(Uri uri, Stream localStream)
        {
            uri = UrlHelper.GetAbsoluteUriWithTrailingSlash(this.BaseUri, uri);
            var lockToken = GetAffectedLockToken(uri);
            var content = new StreamContent(localStream);
            var response = await this.webDavClient.PutAsync(uri, content, lockToken);
            return response.IsSuccessStatusCode;
        }

        #endregion Upload file

        #endregion Public methods

        #region Private methods

        private static WebDavClient CreateWebDavClient(HttpMessageHandler messageHandler)
        {
            return new WebDavClient(messageHandler, false);
        }

        #endregion Private methods
    }
}
