using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Http;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Collections;
using System.Threading.Tasks;

namespace GoogleMusic.Net
{
    internal class Http : IDisposable
    {
        #region Members
        public const string GET = "GET";
        public const string POST = "POST";
        public const string HEAD = "HEAD";
        public const string PUT = "PUT";
        public const string DELETE = "DELETE";
        #endregion

        #region Constructor

        public Http()
        {
            // Note: use System.Net.Http.WebRequestHandler for more features (import dll)
            this.Settings = new HttpClientHandler();

            this.Settings.UseCookies = true;
            this.Settings.CookieContainer = new CookieContainer();
            this.Settings.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            this.Settings.UseProxy = false;
            this.Settings.MaxRequestContentBufferSize = 8000000L;
            
            this.Client = new HttpClient(this.Settings, true);          
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets a value that indicates whether to use a proxy.
        /// </summary>
        public bool UseProxy { get; set; }

        /// <summary>
        /// Gets or sets the proxy to use if UseProxy is true.
        /// </summary>
        public WebProxy Proxy { get; set; }

        /// <summary>
        /// Returns a value that represents the last requested URL. This property is read-only.
        /// </summary>
        public string LastUrl { get; protected set; }

        /// <summary>
        /// Gets or sets the useragent being imitated.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Gets the class's HttpClient object.
        /// </summary>
        public HttpClient Client { get; protected set; }

        /// <summary>
        /// Gets the class's HttpClientHandler object to manage settings.
        /// </summary>
        public HttpClientHandler Settings { get; protected set; }

        #endregion

        public void Dispose()
        {
            this.Client.Dispose();
        }
    }

    internal static class CookieOps
    {
        #region Cookies

        /// <summary>
        /// Deletes all cookies associated with a specified URL.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The specified URL's cookies to clear.</param>
        public static void ClearCookies(this CookieContainer container, string url)
        {
            try
            {
                CookieCollection filtered = container.GetCookies(new Uri(url));
                foreach (Cookie Cookie in filtered)
                    Cookie.Expired = true;
            }
            catch { }
        }


        /// <summary>
        /// Retrieves all cookies stored in the wrapper and returns them in a CookieCollection.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <returns>A CookieCollection instance containing all the cookies in the wrapper.</returns>
        public static CookieCollection GetAllCookies(this CookieContainer container)
        {
            CookieCollection lstCookies = new CookieCollection();
            Hashtable table = (Hashtable)container.GetType().InvokeMember("m_domainTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.GetField | System.Reflection.BindingFlags.Instance, null, container, new object[] { });
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
        /// Finds a particular cookie's value associated with the specified URL and cookie name.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL associated with the cookie to search for.</param>
        /// <param name="cookieName">Required. The name of the cookie to search for.</param>
        /// <returns></returns>
        public static string GetCookie(this CookieContainer container, string url, string cookieName)
        {
            CookieCollection collection = GetCollection(container, url);
            if (collection != null)
                return collection.OfType<Cookie>().First(p => p.Name == cookieName).Value;

            return null;
        }

        /// <summary>
        /// Returns the HTTP cookie headers that contain the HTTP cookies that represent the System.Net.Cookie instances that are associated with the specified URL.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL of the cookies desired.</param>
        /// <returns></returns>
        public static string GetCookiesString(this CookieContainer container, string url)
        {
            try
            {
                return container.GetCookieHeader(new Uri(url));
            }
            catch 
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// Returns System.Net.CookieCollection that contains the System.Net.Cookie instances that associate with the specified URL.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL of the cookies desired.</param>
        /// <returns></returns>
        public static CookieCollection GetCollection(this CookieContainer container, string url)
        {
            try { return container.GetCookies(new Uri(url)); }
            catch { return null; }
        }


        /// <summary>
        /// Adds and associates a specified cookie with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL to associate the cookie with.</param>
        /// <param name="cookie">Required. The cookie to add to the CookieCollection.</param>
        public static void Add(this CookieContainer container, string url, Cookie cookie)
        {
            try
            {
                container.Add(new Uri(url), cookie);
            }
            catch { }
        }


        /// <summary>
        /// Adds and associates an array of cookies with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL to associate all cookies with.</param>
        /// <param name="cookieArray">Required. The array of cookies to add to the CookieCollection.</param>
        public static void Add(this CookieContainer container, string url, Cookie[] cookieArray)
        {
            try { foreach (Cookie c in cookieArray) container.Add(new Uri(url), c); }
            catch { }
        }


        /// <summary>
        /// Sets and associates a specified CookieCollection with a URL in the wrapper's CookieCollection.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The URL to associate the CookieCollection with.</param>
        /// <param name="cookieCollection">Required. The CookieCollection to add to the wrapper's CookieCollection.</param>
        public static void Add(this CookieContainer container, string url, CookieCollection cookieCollection)
        {
            try { container.Add(new Uri(url), cookieCollection); }
            catch { }
        }


        /// <summary>
        /// Adds and associates System.Net.Cookie instances from an HTTP cookie header to the wrapper's CookieCollection with a specific URL.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="url">Required. The specific URL to associate the cookie instances with.</param>
        /// <param name="cookieString">Required. The string of cookies or cookie header to add.</param>
        public static void Add(this CookieContainer container, string url, string cookieString)
        {
            string[] strCookies = cookieString.Split(new string[] { "; " }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string strCookie in strCookies)
            {
                container.SetCookies(new Uri(url), strCookie);
            }
        }


        /// <summary>
        /// Clones the wrapper's cookies of a specific domain and associates them with a new domain.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="oldDomain">Required. The old domain to clone cookies from.</param>
        /// <param name="newDomain">Required. The new domain to clone cookies to.</param>
        /// <returns></returns>
        public static bool CloneCookies(this CookieContainer container, string oldDomain, string newDomain)
        {
            try { return CloneCookies(container, new Uri(oldDomain), new Uri(newDomain)); }
            catch { return false; }
        }


        /// <summary>
        /// Clones the wrapper's cookies of a specific domain and associates them with a new domain.
        /// </summary>
        /// <param name="container">Required. The jar of cookies to operate on.</param>
        /// <param name="oldDomain">Required. The old domain to clone cookies from.</param>
        /// <param name="newDomain">Required. The new domain to clone cookies to.</param>
        /// <returns></returns>
        public static bool CloneCookies(this CookieContainer container, Uri oldDomain, Uri newDomain)
        {
            try
            {
                foreach (System.Net.Cookie Cook in container.GetCookies(oldDomain))
                    container.SetCookies(newDomain, Cook.Name + "=" + Cook.Value + ((Cook.Expires != null) ? "; expires=" + Cook.Expires.ToString() : "") + (!(Cook.Path == string.Empty) ? "; path=" + Cook.Path : "" + "; domain=") + newDomain.Host + (Cook.HttpOnly ? "; HttpOnly" : ""));
                return true;
            }
            catch { return false; }
        }

        #endregion
    }

    internal static class StreamOps
    {

        /// <summary>
        /// Looks up an optimal buffer size given a content length. Attempts to balance memory usage, performance, and progress reporting reponsiveness.
        /// </summary>
        /// <param name="contentLength">Required. The size, in bytes, of the content being buffered.</param>
        /// <returns>Returns a 32-bit signed integer representing a suggested buffer size.</returns>
        static internal int GetOptimalBufferSize(long contentLength)
        {
            if (contentLength < 0L)
                return 16384; // No content length is typically smaller
            if (contentLength > 200000L)
                return 81920; // (Just before 85KB LOH cutoff)
            else if (contentLength > 131072L)
                return 65536;
            else if (contentLength > 65536L)
                return 32768;
            else
                return (int)contentLength; // Read all in one chunk
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<Stream> CopyToAsync(this Stream source, Stream destination, long totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (destination == null)
                throw new ArgumentNullException("destination");

            bool canReportProgress = progress != null && totalSize > 0;

            var totalDone = 0L;
            int bufferSize = GetOptimalBufferSize(totalSize);

            {
                byte[] buffer = new byte[bufferSize];
                int read;

                while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Task writeTask = destination.WriteAsync(buffer, 0, read, cancellationToken);

                    if (canReportProgress)
                        progress.Report(Math.Min(((double)totalDone) / ((double)totalSize) * (progressMax - progressMin) + progressMin, progressMax));

                    await writeTask;
                    totalDone += read;
                }
            }

            if (totalDone != totalSize)
                progress.Report(progressMax);

            return destination;
        }
        
        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to another stream, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="destination">The stream to which the contents of the current stream will be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<Stream> CopyToAsync(this Stream source, Stream destination, long? totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await CopyToAsync(source, destination, (totalSize.HasValue ? totalSize.Value : -1L), progress, progressMin, progressMax, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to a <code>StringBuilder</code> object, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="destination">The <code>StringBuilder</code> to which the contents of the current stream will be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<StringBuilder> CopyToAsync(this Stream source, StringBuilder destination, long totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (destination == null)
                destination = new StringBuilder((int)totalSize);

            bool canReportProgress = progress != null && totalSize > 0;

            var totalDone = 0L;
            int bufferSize = GetOptimalBufferSize(totalSize);

            byte[] buffer = new byte[bufferSize];
            int read;

            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                destination.Append(Encoding.UTF8.GetString(buffer));
                totalDone += read;

                if (canReportProgress)
                    progress.Report(Math.Min(((double)totalDone) / ((double)totalSize) * (progressMax - progressMin) + progressMin, progressMax));

            }

            if (totalDone != totalSize)
                progress.Report(progressMax);

            return destination;
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to a <code>StringBuilder</code> object, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="destination">The <code>StringBuilder</code> to which the contents of the current stream will be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<StringBuilder> CopyToAsync(this Stream source, StringBuilder destination, long? totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await CopyToAsync(source, destination, (totalSize.HasValue ? totalSize.Value : -1L), progress, progressMin, progressMax, cancellationToken);
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to a byte array, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<byte[]> ToArrayAsync(this Stream source, long totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool canReportProgress = progress != null && totalSize > 0;
            byte[] result;
            var totalDone = 0L;

            if (totalSize > 0)
            {
                int bufferSize = GetOptimalBufferSize(totalSize);

                result = new byte[totalSize];
                int read;

                while ((read = await source.ReadAsync(result, (int)totalDone, bufferSize, cancellationToken)) > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (canReportProgress)
                        progress.Report(Math.Min(((double)totalDone) / ((double)totalSize) * (progressMax - progressMin) + progressMin, progressMax));
                    totalDone += read;
                }
            }
            else
            {
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await source.CopyToAsync(memoryStream, totalSize, progress, progressMin, progressMax, cancellationToken);
                    result = memoryStream.ToArray();
                }
            }

            if (totalDone != totalSize)
                progress.Report(progressMax);

            return result;
        }

        /// <summary>
        /// Asynchronously reads the bytes from the current stream and writes them to a byte array, updating the specified progress and monitoring the specified cancellation token.
        /// </summary>
        /// <param name="source">The stream to be copied.</param>
        /// <param name="totalSize">The size, in bytes, of the current stream.</param>
        /// <param name="progress">The object to update when progress is made.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous copy operation. The value of the <code>TResult</code> parameter contains a reference to the <paramref name="destination"/>.</returns>
        public async static Task<byte[]> ToArrayAsync(this Stream source, long? totalSize, IProgress<double> progress, double progressMin = 0.0, double progressMax = 100.0, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await ToArrayAsync(source, (totalSize.HasValue ? totalSize.Value : -1L), progress, progressMin, progressMax, cancellationToken);
        }

    }

}