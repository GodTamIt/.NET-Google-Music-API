using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;


namespace GoogleMusicAPI
{
    public class GoogleHTTP : HTTP
    {
        public static CookieContainer AuthorizationCookieCont = new CookieContainer();
        public static CookieCollection AuthorizationCookies = new CookieCollection();

        public GoogleHTTP()
        {

        }

        public override HttpWebRequest SetupRequest(Uri address, String userAgent = null)
        {
            string xt = GoogleHTTP.GetCookieValue("xt", AuthorizationCookies); 
            if (xt != null)
            {
                address = new Uri(address.OriginalString + String.Format("?u=0&xt={0}", xt));
            }

            HttpWebRequest request = base.SetupRequest(address);

            request.CookieContainer = AuthorizationCookieCont;

            return request;
        }

        public static String GetCookieValue(String cookieName, CookieCollection cookieCollection)
        {
            foreach (Cookie cookie in cookieCollection)
            {
                if (cookie.Name.Equals(cookieName))
                    return cookie.Value;
            }

            return null;
        }

        public static void SetCookieData(CookieContainer cont, CookieCollection coll)
        {
            AuthorizationCookieCont = cont;
            AuthorizationCookies = coll;
        }
    }

    public class OAuth2HTTP : HTTP
    {
        public static String AccessToken = null;

        public OAuth2HTTP()
        {

        }

        public override HttpWebRequest SetupRequest(Uri address, String userAgent = null)
        {
            HttpWebRequest request = base.SetupRequest(address);

            if (AccessToken != null)
                request.Headers[HttpRequestHeader.Authorization] = String.Format("Bearer {0}", AccessToken);

            if (userAgent != null)
                request.UserAgent = userAgent;

            return request;
        }
    }
}
