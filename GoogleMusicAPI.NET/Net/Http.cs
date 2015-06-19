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
            catch (Exception) { }
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
                return collection.OfType<Cookie>().FirstOrDefault(p => p.Name == cookieName).Value;

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
            catch (Exception)
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
            catch (Exception) { return null; }
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
            catch (Exception)
            {
            }
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
            catch (Exception) { }
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
            catch (Exception) { }
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
            catch (Exception) { return false; }
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
            catch (Exception) { return false; }
        }

        #endregion
    }

}


