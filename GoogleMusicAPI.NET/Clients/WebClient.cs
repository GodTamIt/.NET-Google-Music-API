using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using GoogleMusic.Net;

namespace GoogleMusic.Clients
{
    public class WebClient : IClient
    {

        #region Members
        private Regex AUTH_REGEX;
        private Regex AUTH_ERROR_REGEX;
        
        private Http http;


        private string _authorizationToken;
        private string _xt;

        #endregion

        #region Constructor

        public WebClient(bool validate = true, bool verifySSL = true)
        {
            http = new Http();

            AUTH_REGEX = new Regex(@"Auth=(?<AUTH>(.*?))$", RegexOptions.IgnoreCase);
            AUTH_ERROR_REGEX = new Regex(@"Error=(?<ERROR>(.*?))$", RegexOptions.IgnoreCase); 
        }

        #endregion

        #region Properties

        public string AuthorizationToken
        {
            get { return _authorizationToken; }
            set { _authorizationToken = value; }
        }

        public bool IsLoggedIn
        {
            get { return !String.IsNullOrEmpty(AuthorizationToken) && !String.IsNullOrEmpty(_xt); }
        }

        #endregion

        #region Login

        /// <summary>
        /// Attempts to log into Google's Music service using Google's ClientLogin.
        /// </summary>
        /// <param name="email">Required. The email address of the Google account to log into.</param>
        /// <param name="password">Required. The password of the Google account to log into.</param>
        /// <returns>A Result object indicating whether the login was successful.</returns>
        public Result<bool> Login(string email, string password)
        {
            // Step 1: Get authorization token
            try
            {
                string response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("service", "sj");
                    form.AddField("Email", email);
                    form.AddField("Passwd", password);
                    form.AddEnding();

                    response = http.Request(SetupWebRequest("https://www.google.com/accounts/ClientLogin", "POST"), form).ToUTF8();
                }

                Match regex = AUTH_REGEX.Match(response);

                if (regex.Success)
                {
                    _authorizationToken = regex.Groups["AUTH"].Value;
                }
                else
                {
                    this.Logout();
                    regex = AUTH_ERROR_REGEX.Match(response);
                    if (regex.Success)
                        return new Result<bool>(false, false, this, "Google returned the following error while trying to retrieve an authorization token:\r\n\r\n" + regex.Groups["ERROR"].Value);
                    else
                        return new Result<bool>(false, false, this, "An unknown error occurred.");
                }
            }
            catch (WebException e)
            {
                return new Result<bool>(false, false, this, e.ToString("A network error occurred while attempting to retrieve an authorization token."), e);
            }
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An unknown error occurred."), e);
            }

            // Step 2: Get authorization cookie
            try
            {
                CookieCollection cookies;
                using (HttpWebResponse response = http.Request(SetupWebRequest("https://play.google.com/music/listen?hl=en&u=0", "HEAD", false)))
                {
                    cookies = response.Cookies;
                }

                bool success = false;
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name.Equals("xt"))
                    {
                        success = true;
                        _xt = cookie.Value;
                        break;
                    }
                }

                if (success)
                    return new Result<bool>(true, true, this);
                else
                    return new Result<bool>(false, false, this, "Unable to retrieve the proper authorization cookies from Google.");
            }
            catch (WebException e)
            {
                return new Result<bool>(false, false, this, e.ToString("A network error occurred while attempting to retrieve authorization cookies."), e);
            }
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An unknown error occurred."), e);
            }
        }

        /// <summary>
        /// Asynchronously attempts to log into Google's Music service using Google's ClientLogin.
        /// </summary>
        /// <param name="email">Required. The email address of the Google account to log into.</param>
        /// <param name="password">Required. The password of the Google account to log into.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>A Result object indicating whether the login was successful.</returns>
        public async Task<Result<bool>> LoginAsync(string email, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Step 1: Get authorization token
            try
            {
                string response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("service", "sj");
                    form.AddField("Email", email);
                    form.AddField("Passwd", password);
                    form.AddEnding();

                    response = await (await http.RequestAsync(SetupWebRequest("https://www.google.com/accounts/ClientLogin", "POST"), form, cancellationToken)).ToUTF8Async();
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                Match regex = AUTH_REGEX.Match(response);

                if (regex.Success)
                {
                    _authorizationToken = regex.Groups["AUTH"].Value;
                }
                else
                {
                    this.Logout();
                    regex = AUTH_ERROR_REGEX.Match(response);
                    if (regex.Success)
                        return new Result<bool>(false, false, this, "Google returned the following error while trying to retrieve an authorization token:\r\n\r\n" + regex.Groups["ERROR"].Value);
                    else
                        return new Result<bool>(false, false, this, "An unknown error occurred.");
                }
            }
            catch (WebException e)
            {
                return new Result<bool>(false, false, this, e.ToString("A network error occurred while attempting to retrieve an authorization token."), e);
            }
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An unknown error occurred."), e);
            }

            // Step 2: Get authorization cookie
            try
            {
                CookieCollection cookies;
                using (HttpWebResponse response = await http.RequestAsync(SetupWebRequest("https://play.google.com/music/listen?hl=en&u=0", "HEAD", false)))
                {
                    cookies = response.Cookies;
                }

                bool success = false;
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name.Equals("xt"))
                    {
                        success = true;
                        _xt = cookie.Value;
                        break;
                    }
                }

                if (success)
                    return new Result<bool>(true, true, this);
                else
                    return new Result<bool>(false, false, this, "Unable to retrieve the proper authorization cookies from Google.");
            }
            catch (WebException e)
            {
                return new Result<bool>(false, false, this, e.ToString("A network error occurred while attempting to retrieve authorization cookies."), e);
            }
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An unknown error occurred."), e);
            }
        }

        public void Logout()
        {
            http.ClearCookies();
            _authorizationToken = null;
        }

        #endregion

        #region Web

        private HttpWebRequest SetupWebRequest(string address, string method = "GET", bool autoRedirect = true)
        {
            if (address.Contains("play.google.com/music/services/"))
                address += (address.Contains('?') ? '&' : '?') + "u=0&xt=" + _xt;
            

            HttpWebRequest request = http.CreateRequest(address, method);

            request.AllowAutoRedirect = autoRedirect;

            if (!String.IsNullOrEmpty(_authorizationToken))
                request.Headers[HttpRequestHeader.Authorization] = "GoogleLogin auth=" + _authorizationToken;

            return request;
        }

        #endregion
    }
}
