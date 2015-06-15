using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Cache;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Web;
using System.Collections;
using System.Threading.Tasks;

namespace GoogleMusic.Net
{
    internal class Http
    {
        #region Members
        private DecompressionMethods _DecompressionMethod = DecompressionMethods.GZip;
        private int _Timeout = 10000;
        private string _UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.124 Safari/537.36";
        private string _LastUrl = "http://www.google.com/";
        private bool _UseProxy = false;
        private WebProxy _Proxy;
        private bool _UsePipelining = false;
        private Encoding _Encoding = Encoding.UTF8;
        private int _BufferSize = 8192;
        private HttpRequestCacheLevel _CachingPolicy = HttpRequestCacheLevel.Revalidate;


        private CookieContainer _Cookies = new CookieContainer();
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that indicates whether to use a proxy.
        /// </summary>
        public bool UseProxy
        {
            get { return _UseProxy; }
            set { _UseProxy = value; }
        }

        /// <summary>
        /// Gets or sets the proxy to use if UseProxy is true.
        /// </summary>
        public WebProxy Proxy
        {
            get { return _Proxy; }
            set { _Proxy = value; }
        }

        /// <summary>
        /// Returns a value that represents the last requested URL. This property is read-only.
        /// </summary>
        public string LastUrl
        {
            get { return _LastUrl; }
        }

        /// <summary>
        /// Gets or sets the useragent being imitated.
        /// </summary>
        public string UserAgent
        {
            get { return _UserAgent; }
            set { _UserAgent = value; }
        }

        /// <summary>
        /// Gets or sets a value in milliseconds that indicates the time after initiating a request to wait before timing out.
        /// </summary>
        public int Timeout
        {
            get { return _Timeout; }
            set { _Timeout = Math.Abs(value); }
        }

        /// <summary>
        /// Gets or sets the decompression method to use. GZip is default.
        /// </summary>
        public DecompressionMethods DecompressionMethod
        {
            get { return _DecompressionMethod; }
            set { _DecompressionMethod = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to pipeline to each request.
        /// </summary>
        public bool UsePipelining
        {
            get { return _UsePipelining; }
            set { _UsePipelining = value; }
        }

        /// <summary>
        /// Gets or sets the cookies to use.
        /// </summary>
        public CookieContainer Cookies
        {
            get { return _Cookies; }
            set { _Cookies = value; }
        }

        /// <summary>
        /// Gets or sets the encoding to use.
        /// </summary>
        public Encoding Encoding
        {
            get { return _Encoding; }
            set { _Encoding = value; }
        }

        /// <summary>
        /// Gets or sets the buffer size to use when reading streams.
        /// </summary>
        public int BufferSize
        {
            get { return _BufferSize; }
            set { _BufferSize = value; }
        }

        /// <summary>
        /// Gets or sets the caching policy.
        /// </summary>
        public HttpRequestCacheLevel CachingPolicy
        {
            get { return _CachingPolicy; }
            set { _CachingPolicy = value; }
        }

        #endregion

        #region Non-Request Functions

        public static string UrlEncode(string encode, string encoding)
        {
            return HttpUtility.UrlEncode(encode, Encoding.GetEncoding(encoding));
        }

        public HttpWebRequest CreateRequest(string url, string method = "GET", string referer = "", string accept = "*/*")
        {
            HttpWebRequest request;

            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Proxy = (_UseProxy ? _Proxy : null);
            request.AutomaticDecompression = _DecompressionMethod;

            request.CachePolicy = new HttpRequestCachePolicy(_CachingPolicy);
            request.Accept = accept;
            request.Referer = referer;
            request.CookieContainer = _Cookies;
            request.UserAgent = _UserAgent;
            request.Pipelined = (_UsePipelining && method != "POST" && method != "PUT");

            return request;
        }

        public byte[] GetContent(HttpWebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    responseStream.CopyTo(memory, _BufferSize);
                    return memory.ToArray();
                }
            }
        }

        public async Task<byte[]> GetContentAsync(HttpWebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            {
                using (MemoryStream memory = new MemoryStream())
                {
                    await responseStream.CopyToAsync(memory, _BufferSize);
                    return memory.ToArray();
                }
            }
        }

        #endregion

        #region Upload

        public HttpWebResponse UploadDataSync(HttpWebRequest request, string contentType, byte[] data)
        {
            request.ContentLength = (data != null) ? data.Length : 0;

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            // Write to upload stream
            using (Stream uploadStream = request.GetRequestStream())
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    ms.CopyTo(uploadStream, _BufferSize);
                }
            }

            return (HttpWebResponse)request.GetResponse();
        }

        public async Task<HttpWebResponse> UploadDataAsync(HttpWebRequest request, string contentType, byte[] data, TaskProgressEventHandler progressHandler = null, TaskCompleteEventHandler completeHandler = null)
        {
            request.ContentLength = (data != null) ? data.Length : 0;

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            // Write to upload stream
            using (Stream uploadStream = await request.GetRequestStreamAsync())
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    byte[] buffer = new byte[Math.Min(_BufferSize, data.Length)];
                    long bytesRead = 0;
                    int chunk;

                    while ((chunk = await ms.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        // Start upload asynchronously
                        Task uploadTask = uploadStream.WriteAsync(buffer, 0, chunk);

                        // Calculate bytes
                        bytesRead += chunk;

                        // ProgressHandler: Begin asynchronous invoke
                        IAsyncResult progressHandlerResult = null;
                        if (progressHandler != null)
                            progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, bytesRead / ms.Length), null, null);

                        // Wait for chunk upload to finish
                        await uploadTask;

                        // End asynchronous ProgressHandler
                        if (progressHandler != null && progressHandlerResult != null)
                            progressHandler.EndInvoke(progressHandlerResult);
                    }
                    // No progress update version:
                    //await ms.CopyToAsync(uploadStream, _BufferSize);
                }
            }

            HttpWebResponse result;

            {
                // HttpWebResponse: Start task
                Task<WebResponse> resultTask = request.GetResponseAsync();

                // CompleteHandler: Begin asynchronous invoke
                IAsyncResult completeHandlerResult = null;
                if (completeHandler != null)
                    completeHandler.BeginInvoke(null, null);

                // HttpWebResponse: Await result
                result = (HttpWebResponse)(await resultTask);

                // CompleteHandler: End asynchronous invoke
                if (completeHandler != null && completeHandlerResult != null)
                    completeHandler.EndInvoke(completeHandlerResult);
            }

            return result;
        }

        #endregion

    }
}


