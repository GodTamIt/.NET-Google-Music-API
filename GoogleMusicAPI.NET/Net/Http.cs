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
        private DecompressionMethods _decompressionMethod = DecompressionMethods.GZip;
        private int _timeout = 10000;
        private string _userAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.124 Safari/537.36";
        private string _lastUrl = "http://www.google.com/";
        private bool _useProxy = false;
        private WebProxy _proxy;
        private bool _usePipelining = false;
        private Encoding _encoding = Encoding.UTF8;
        private HttpRequestCacheLevel _cachingPolicy = HttpRequestCacheLevel.Revalidate;


        private CookieContainer _Cookies = new CookieContainer();
        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that indicates whether to use a proxy.
        /// </summary>
        public bool UseProxy
        {
            get { return _useProxy; }
            set { _useProxy = value; }
        }

        /// <summary>
        /// Gets or sets the proxy to use if UseProxy is true.
        /// </summary>
        public WebProxy Proxy
        {
            get { return _proxy; }
            set { _proxy = value; }
        }

        /// <summary>
        /// Returns a value that represents the last requested URL. This property is read-only.
        /// </summary>
        public string LastUrl
        {
            get { return _lastUrl; }
        }

        /// <summary>
        /// Gets or sets the useragent being imitated.
        /// </summary>
        public string UserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        /// <summary>
        /// Gets or sets a value in milliseconds that indicates the time after initiating a request to wait before timing out.
        /// </summary>
        public int Timeout
        {
            get { return _timeout; }
            set { _timeout = Math.Abs(value); }
        }

        /// <summary>
        /// Gets or sets the decompression method to use. GZip is default.
        /// </summary>
        public DecompressionMethods DecompressionMethod
        {
            get { return _decompressionMethod; }
            set { _decompressionMethod = value; }
        }

        /// <summary>
        /// Gets or sets a value that indicates whether to pipeline to each request.
        /// </summary>
        public bool UsePipelining
        {
            get { return _usePipelining; }
            set { _usePipelining = value; }
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
            get { return _encoding; }
            set { _encoding = value; }
        }

        /// <summary>
        /// Gets or sets the caching policy.
        /// </summary>
        public HttpRequestCacheLevel CachingPolicy
        {
            get { return _cachingPolicy; }
            set { _cachingPolicy = value; }
        }

        #endregion

        #region Cookies

        /// <summary>
        /// Deletes all cookies and logins.
        /// </summary>
        public void ClearCookies()
        {
            _Cookies = new CookieContainer();
        }


        /// <summary>
        /// Deletes all cookies associated with a specified URL.
        /// </summary>
        /// <param name="URL">Required. The specified URL's cookies to clear.</param>
        public void ClearCookies(string URL)
        {
            try
            {
                CookieCollection CC = _Cookies.GetCookies(new Uri(URL));
                foreach (Cookie Cookie in CC)
                {
                    Cookie.Expired = true;
                }
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Retrieves all cookies stored in the wrapper and returns them in a CookieCollection.
        /// </summary>
        /// <returns>A CookieCollection instance containing all the cookies in the wrapper.</returns>
        public CookieCollection GetAllCookies()
        {
            CookieCollection lstCookies = new CookieCollection();
            Hashtable table = (Hashtable)_Cookies.GetType().InvokeMember("m_domainTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance, null, _Cookies, new object[] { });
            foreach (object pathList in table.Values)
            {
                SortedList lstCookieCol = (SortedList)pathList.GetType().InvokeMember("m_list", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance, null, pathList, new object[] { });
                foreach (CookieCollection colCookies in lstCookieCol.Values)
                    foreach (Cookie c in colCookies)
                    {
                        lstCookies.Add(c);
                    }
            }
            return lstCookies;
        }


        /// <summary>
        /// Returns the HTTP cookie headers that contain the HTTP cookies that represent the System.Net.Cookie instances that are associated with the specified URL.
        /// </summary>
        /// <param name="URL">Required. The URL of the cookies desired.</param>
        /// <returns></returns>
        public string GetCookieString(string URL)
        {
            try
            {
                return _Cookies.GetCookieHeader(new Uri(URL));
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// Returns System.Net.CookieCollection that contains the System.Net.Cookie instances that associate with the specified URL
        /// </summary>
        /// <param name="URL">Required. The URL of the cookies desired.</param>
        /// <returns></returns>
        public CookieCollection GetCookieCollection(string URL)
        {
            try
            {
                return _Cookies.GetCookies(new Uri(URL));
            }
            catch (Exception)
            {
                return null;
            }
        }


        /// <summary>
        /// Adds and associates a specified cookie with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="URL">Required. The URL to associate the cookie with.</param>
        /// <param name="Cookie">Required. The cookie to add to the CookieCollection.</param>
        public void AddCookie(string URL, Cookie Cookie)
        {
            try
            {
                _Cookies.Add(new Uri(URL), Cookie);
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Adds and associates an array of cookies with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="URL">Required. The URL to associate all cookies with.</param>
        /// <param name="Cookie">Required. The array of cookies to add to the CookieCollection.</param>
        public void AddCookieArray(string URL, Cookie[] Cookie)
        {
            try
            {
                foreach (Cookie c in Cookie)
                {
                    _Cookies.Add(new Uri(URL), c);
                }
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Sets and associates a specified CookieCollection with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="URL">Required. The URL to associate the CookieCollection with.</param>
        /// <param name="CCollection">Required. The CookieCollection to add to the wrapper's CookieCollection.</param>
        public void AddCookieCollection(string URL, CookieCollection CCollection)
        {
            try
            {
                _Cookies.Add(new Uri(URL), CCollection);
            }
            catch (Exception)
            {
            }
        }


        /// <summary>
        /// Adds and associates System.Net.Cookie instances from an HTTP cookie header to the wrapper's CookieCollection with a specific URL.
        /// </summary>
        /// <param name="URL">Required. The specific URL to associate the cookie instances with.</param>
        /// <param name="CookieString">Required. The string of cookies or cookie header to add.</param>
        public void AddCookieString(string URL, string CookieString)
        {
            string[] strCookies = CookieString.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string strCookie in strCookies)
            {
                _Cookies.SetCookies(new Uri(URL), strCookie);
            }
        }


        /// <summary>
        /// Clones the wrapper's cookies of a specific domain and associates them with a new domain.
        /// </summary>
        /// <param name="OldDomain">Required. The old domain to clone cookies from.</param>
        /// <param name="NewDomain">Required. The new domain to clone cookies to.</param>
        /// <returns></returns>
        public bool CloneCookies(string OldDomain, string NewDomain)
        {
            try
            {
                return CloneCookies(new Uri(OldDomain), new Uri(NewDomain));
            }
            catch (Exception)
            {
                return false;
            }
        }


        /// <summary>
        /// Clones the wrapper's cookies of a specific domain and associates them with a new domain.
        /// </summary>
        /// <param name="OldDomain">Required. The old domain to clone cookies from.</param>
        /// <param name="NewDomain">Required. The new domain to clone cookies to.</param>
        /// <returns></returns>
        public bool CloneCookies(Uri OldDomain, Uri NewDomain)
        {
            try
            {
                string CookNewStr = string.Empty;
                foreach (System.Net.Cookie Cook in _Cookies.GetCookies(OldDomain))
                {
                    _Cookies.SetCookies(NewDomain, Cook.Name + "=" + Cook.Value + ((Cook.Expires != null) ? "; expires=" + Cook.Expires.ToString() : "") + (!(Cook.Path == string.Empty) ? "; path=" + Cook.Path : "" + "; domain=") + NewDomain.Host + (Cook.HttpOnly ? "; HttpOnly" : ""));
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #endregion

        #region Pre-Request

        /// <summary>
        /// Creates and prepares a new HttpWebRequest object to use.
        /// </summary>
        /// <param name="url">Required. The destination URL to visit.</param>
        /// <param name="method">Optional. The HTTP method of the request. The default value is GET.</param>
        /// <param name="referer">Optional. The Referer HTTP header to send with the request. The default is nothing.</param>
        /// <param name="accept">Optional. The Accept HTTP header to send with the request. The default is */*.</param>
        /// <returns></returns>
        public HttpWebRequest CreateRequest(string url, string method = "GET", string referer = "", string accept = "*/*")
        {
            HttpWebRequest request;

            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.Method = method;
            request.Proxy = (_useProxy ? _proxy : null);
            request.AutomaticDecompression = _decompressionMethod;

            request.CachePolicy = new HttpRequestCachePolicy(_cachingPolicy);
            request.Accept = accept;
            request.Referer = referer;
            request.CookieContainer = _Cookies;
            request.UserAgent = _userAgent;
            request.Pipelined = (_usePipelining && method != "POST" && method != "PUT");

            return request;
        }

        #endregion

        #region Request

        /// <summary>
        /// Performs an HttpWebRequest with request data.
        /// </summary>
        /// <param name="request">Required. The HttpWebRequest to represent the base request.</param>
        /// <param name="contentType">Required. The Content-Type HTTP header to send with the request.</param>
        /// <param name="data">Required. A byte array representing the request data to send with the request.</param>
        /// <returns>Returns a HttpWebResponse representing the response from the server.</returns>
        public HttpWebResponse Request(HttpWebRequest request, string contentType, byte[] data)
        {
            request.ContentLength = (data != null) ? data.Length : 0;

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            // Write to upload stream
            using (Stream uploadStream = request.GetRequestStream())
            {
                uploadStream.Write(data, 0, data.Length);
            }

            return (HttpWebResponse)request.GetResponse();
        }

        /// <summary>
        /// Asynchronously performs an HttpWebRequest with request data.
        /// </summary>
        /// <param name="request">Required. The HttpWebRequest to represent the base request.</param>
        /// <param name="contentType">Required. The Content-Type HTTP header to send with the request.</param>
        /// <param name="data">Required. A byte array representing the request data to send with the request.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <param name="progressHandler">Optional. The event handler to invoke when progress has changed.</param>
        /// <param name="completeHandler">Optional. The event handler to invoke when the request has finished.</param>
        /// <returns>Returns a HttpWebResponse representing the response from the server.</returns>
        public async Task<HttpWebResponse> RequestAsync(HttpWebRequest request, string contentType, byte[] data,
            CancellationToken cancellationToken = default(CancellationToken), TaskProgressEventHandler progressHandler = null, TaskCompleteEventHandler completeHandler = null)
        {
            request.ContentLength = (data != null) ? data.Length : 0;

            if (!String.IsNullOrEmpty(contentType))
                request.ContentType = contentType;

            // Write to upload stream
            using (Stream uploadStream = await request.GetRequestStreamAsync())
            {
                int bufferSize = GetOptimalBufferSize(data.Length);
                int bytesDone = new int();

                while (bytesDone < data.Length)
                {
                    // ProgressHandler: Begin asynchronous invoke
                    IAsyncResult progressHandlerResult = null;
                    if (progressHandler != null)
                        progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, (double)bytesDone / (double)data.Length), null, null);

                    // WriteTask: Start writing to stream asynchronously
                    int nextChunkSize = Math.Min(data.Length - bytesDone, bufferSize);
                    Task writeTask = uploadStream.WriteAsync(data, bytesDone, nextChunkSize, cancellationToken);                    

                    // End asynchronous ProgressHandler
                    if (progressHandler != null && progressHandlerResult != null)
                        progressHandler.EndInvoke(progressHandlerResult);

                    // WriteTask: Wait for chunk upload to finish
                    await writeTask;
                    bytesDone += nextChunkSize;
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

        /// <summary>
        /// Looks up an optimal buffer size given a content length. Attempts to balance memory usage, performance, and progress reporting reponsiveness.
        /// </summary>
        /// <param name="contentLength">Required. The size, in bytes, of the content being buffered.</param>
        /// <returns>Returns a 32-bit signed integer representing a suggested buffer size.</returns>
        static internal int GetOptimalBufferSize(long contentLength)
        {
            if (contentLength < 0L)
                return 65536; // Default 60 KB
            else if (contentLength > 33554432L)
                return 1048576; // 1 MB when content is > 32 MB
            else if (contentLength > 8388608L)
                return 524288; // 0.5 MB when content is > 32 MB
            else if (contentLength > 1048576L)
                return 131072;
            else if (contentLength > 131072L)
                return 32768;
            else
                return 8192;
        }

        #endregion

        #region Post-Request

        /// <summary>
        /// Reads the content of an HTTP response to a byte array.
        /// </summary>
        /// <param name="response">Required. The HttpWebResponse object representing the response to read.</param>
        /// <param name="disposeResponse">Optional. A boolean value determining whether to dispose of the response when finished. The default is true.</param>
        /// <returns>Returns a byte array representing the content of the response.</returns>
        public static byte[] ResponseToArray(HttpWebResponse response, bool disposeResponse = true)
        {
            byte[] result;
            if (response.ContentLength > 0)
            {
                result = new byte[response.ContentLength];
                using (Stream responseStream = response.GetResponseStream())
                {
                    responseStream.Read(result, 0, result.Length);
                }
            }
            else
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        responseStream.CopyTo(memoryStream, GetOptimalBufferSize(-1));
                    }
                    result = memoryStream.ToArray();
                }
            }
            

            if (disposeResponse)
                response.Close();

            return result;
        }

        /// <summary>
        /// Reads the content of an HTTP response to a byte array.
        /// </summary>
        /// <param name="response">Required. The HttpWebResponse object representing the response to read.</param>
        /// <param name="disposeResponse">Optional. A boolean value determining whether to dispose of the response when finished. The default is true.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <param name="progressHandler">Optional. The event handler to invoke when progress has changed.</param>
        /// <param name="completeHandler">Optional. The event handler to invoke when the response has been read.</param>
        /// <returns>Returns a byte array representing the content of the response.</returns>
        public async Task<byte[]> ResponseToArrayAsync(HttpWebResponse response, bool disposeResponse = true,
            CancellationToken cancellationToken = default(CancellationToken), TaskProgressEventHandler progressHandler = null, TaskCompleteEventHandler completeHandler = null)
        {
            byte[] result;
            if (response.ContentLength > 0)
            {
                long contentLength = response.ContentLength;
                result = new byte[contentLength];
                int bufferSize = GetOptimalBufferSize(contentLength);

                using (Stream responseStream = response.GetResponseStream())
                {
                    long bytesRead = 0;

                    // ReadTask: Start reading before first iteration.
                    int chunkRead = await responseStream.ReadAsync(result, 0, (int)Math.Min(contentLength - bytesRead, bufferSize));
                    bytesRead += chunkRead;

                    while (bytesRead < contentLength && chunkRead > 0)
                    {
                        // ProgressHandler: Begin asynchronous invoke
                        IAsyncResult progressHandlerResult = null;
                        if (progressHandler != null)
                            progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, (double)bytesRead / (double)contentLength), null, null);

                        // ReadTask: Start another read asynchronously
                        Task<int> readTask = responseStream.ReadAsync(result, 0, (int)Math.Min(contentLength - bytesRead, bufferSize), cancellationToken);

                        // ProgressHandler: End asynchronous invoke
                        if (progressHandler != null && progressHandlerResult != null)
                            progressHandler.EndInvoke(progressHandlerResult);

                        // WriteTask: Wait for chunk to finish
                        chunkRead = await readTask;
                        bytesRead += chunkRead;
                    }
                }
            }
            else
            {
                int bufferSize = GetOptimalBufferSize(-1);
                using (MemoryStream memory = new MemoryStream(bufferSize))
                {
                    using (Stream responseStream = response.GetResponseStream())
                    {
                        byte[] buffer = new byte[bufferSize];
                        long bytesRead = 0;
                        int chunkRead;

                        while ((chunkRead = await responseStream.ReadAsync(buffer, 0, bufferSize)) > 0)
                        {
                            // ProgressHandler: Begin asynchronous invoke
                            IAsyncResult progressHandlerResult = null;
                            if (progressHandler != null && response.ContentLength > 0)
                                progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, (double)bytesRead / (double)response.ContentLength), null, null);

                            // WriteTask: Start writing to memory asynchronously
                            Task writeTask = memory.WriteAsync(buffer, 0, chunkRead, cancellationToken);                          

                            // End asynchronous ProgressHandler
                            if (progressHandler != null && progressHandlerResult != null)
                                progressHandler.EndInvoke(progressHandlerResult);

                            // WriteTask: Wait for chunk to finish
                            await writeTask;
                            bytesRead += chunkRead;
                        }
                    }

                    result = memory.ToArray();
                }
            }

            return result;
        }

        #endregion

    }


    internal class FormBuilder : IDisposable
    {
        #region Members
        private string boundary = "----------" + DateTime.Now.Ticks.ToString("x");
        private List<Stream> streams = new List<Stream>();

        private long _length = new long();
        #endregion

        #region Properties

        public static FormBuilder Empty
        {
            get
            {
                return new FormBuilder();
            }
        }

        public String ContentType
        {
            get { return "multipart/form-data; boundary=" + boundary; }
        }

        public long Length
        {
            get { return _length; }
            set { _length = value; }
        }

        #endregion

        #region Add

        public void AddField(string key, string value)
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\";\r\n\r\n{1}", key, value);

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());

            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;
        }

        public void AddFields(Dictionary<string, string> fields)
        {
            foreach (KeyValuePair<string, string> key in fields)
                this.AddField(key.Key, key.Value);
        }

        public void AddFile(string name, string fileName, FileStream fileStream)
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("\r\n--{0}\r\n", boundary);
            sb.AppendFormat("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\n", name, fileName);

            sb.AppendFormat("Content-Type: {0}\r\n\r\n", "application/octet-stream");

            byte[] sbData = Encoding.UTF8.GetBytes(sb.ToString());
            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;

            streams.Add(fileStream);
            _length += fileStream.Length;
        }

        #endregion

        #region WriteTo

        /// <summary>
        /// Adds the appropriate ending data to the form. This must be called before writing the form to ensure it is valid.
        /// </summary>
        private void AddEnding()
        {
            if (streams.Count < 1 || !(streams[streams.Count - 1] is MemoryStream))
                streams.Add(new MemoryStream());

            byte[] sbData = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--\r\n");

            streams[streams.Count - 1].Write(sbData, 0, sbData.Length);
            _length += sbData.Length;
        }

        public void WriteTo(Stream destination, int bufferSize)
        {
            AddEnding();

            foreach (Stream s in streams)
            {
                if (s is MemoryStream)
                {
                    MemoryStream ms = (MemoryStream)s;
                    ms.WriteTo(destination);
                }
                else
                {
                    s.CopyTo(destination, bufferSize);
                }
            }
        }

        public async Task WriteToAsync(Stream destination, int bufferSize,
            CancellationToken cancellationToken = default(CancellationToken), TaskProgressEventHandler progressHandler = null, TaskCompleteEventHandler completeHandler = null)
        {
            AddEnding();

            byte[] buffer = null;

            foreach (Stream currentStream in streams)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                // Purely for progress purposes
                double totalRead = new double();
                
                if (currentStream is MemoryStream)
                {
                    // MemoryStream: Get underlying buffer
                    MemoryStream memoryStream = (MemoryStream)currentStream;

                    // MemoryStream: Check if underlying buffer is valid
                    bool bufferValid = false;
                    try
                    {
                        buffer = memoryStream.GetBuffer();
                        bufferValid = buffer != null;
                    }
                    catch (Exception) { }

                    if (bufferValid)
                    {
                        // MemoryStream Buffer: Buffer is valid and can be used
                        int readPosition = new int();

                        while (readPosition < memoryStream.Length && !cancellationToken.IsCancellationRequested)
                        {
                            // ProgressHandler: Begin asynchronous invoke
                            IAsyncResult progressHandlerResult = null;
                            if (progressHandler != null)
                                progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, totalRead / (double)_length), null, null);

                            // WriteTask: Start writing to stream asynchronously
                            int chunk = Math.Min(buffer.Length - readPosition, bufferSize);
                            Task writeTask = destination.WriteAsync(buffer, readPosition, chunk, cancellationToken);

                            // ProgressHandler: End asynchronous invoke
                            if (progressHandler != null && progressHandlerResult != null)
                                progressHandler.EndInvoke(progressHandlerResult);

                            // WriteTask: Wait for chunk upload to finish
                            await writeTask;
                            totalRead += chunk;
                            readPosition += chunk;
                        }
                        // Go onto next stream
                        continue;
                    }

                    // MemoryStream Buffer: Buffer was not valid - fall through to next case
                }

                if (buffer == null || buffer.Length < bufferSize)
                    buffer = new byte[bufferSize];

                int chunkRead;
                while ((chunkRead = await currentStream.ReadAsync(buffer, 0, bufferSize)) > 0 && !cancellationToken.IsCancellationRequested)
                {
                    // ProgressHandler: Begin asynchronous invoke
                    IAsyncResult progressHandlerResult = null;
                    if (progressHandler != null)
                        progressHandlerResult = progressHandler.BeginInvoke(Math.Min(1.0, (double)totalRead / (double)_length), null, null);

                    // WriteTask: Start writing to stream asynchronously
                    Task writeTask = destination.WriteAsync(buffer, 0, chunkRead, cancellationToken);

                    // ProgressHandler: End asynchronous invoke
                    if (progressHandler != null && progressHandlerResult != null)
                        progressHandler.EndInvoke(progressHandlerResult);

                    // WriteTask: Wait for write to finish
                    await writeTask;
                    totalRead += chunkRead;
                }
            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            foreach (Stream s in streams)
                s.Dispose();
        }

        #endregion

    }
}


