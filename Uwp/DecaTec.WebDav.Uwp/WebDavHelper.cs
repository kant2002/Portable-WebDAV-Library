﻿using System;
using System.IO;
using Windows.Web.Http;

namespace DecaTec.WebDav
{
    /// <summary>
    /// Helper functions for WebDAV.
    /// </summary>
    public static partial class WebDavHelper
    {
        /// <summary>
        /// Gets a LockToken from a WebDavResponseMessage.
        /// </summary>
        /// <param name="responseMessage">The WebDavResponseMessage whose LockToken should be retrieved.</param>
        /// <returns>The LockToken of the WebDavResponseMessage or null if the WebDavResponseMessage does not contain a lock token.</returns>
        public static LockToken GetLockTokenFromWebDavResponseMessage(HttpResponseMessage responseMessage)
        {
            // Try to get lock token from response header.
            string lockTokenHeaderValue;
            var success = responseMessage.Headers.TryGetValue(WebDavRequestHeader.LockTocken, out lockTokenHeaderValue);

            if (!success)
                return new LockToken(lockTokenHeaderValue);

            // If lock token was not submitted by response header, it should be found in the response content.
            try
            {
                var prop = WebDavResponseContentParser.ParsePropResponseContentAsync(responseMessage.Content).Result;
                return new LockToken(prop.LockDiscovery.ActiveLock[0].LockToken.Href);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a stream from a string.
        /// </summary>
        /// <param name="s">The string to get the corresponding stream from.</param>
        /// <returns>The stream of the string.</returns>
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
