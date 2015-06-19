using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using System.Net.Http;
using System.Net.Http.Headers;

using GoogleMusic.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

//using wireless_android_skyjam;

namespace GoogleMusic.Clients
{
    public class WebClient : IClient
    {

        #region Members
        // Constants
        private static readonly Regex AUTH_REGEX = new Regex(@"Auth=(?<AUTH>(.*?))$", RegexOptions.IgnoreCase);
        private static readonly Regex AUTH_ERROR_REGEX = new Regex(@"Error=(?<ERROR>(.*?))$", RegexOptions.IgnoreCase);
        private static readonly Regex AUTH_USER_ID_REGEX = new Regex(@"window\['USER_ID'\] = '(?<USERID>(.*?))'", RegexOptions.IgnoreCase);
        private static readonly Regex GET_ALL_SONGS_REGEX = new Regex(@"window.parent\['slat_process'\]\((?<TRACKS>.*?)\);\nwindow.parent\['slat_progress'\]", RegexOptions.Singleline | RegexOptions.Compiled);

        private Http_Old http_old;
        private Http http;

        #endregion

        #region Constructor

        public WebClient()
        {
            http_old = new Http_Old();
            http = new Http();
        }

        #endregion

        #region Properties

        public string AuthorizationToken { get; protected set; }

        public bool IsLoggedIn
        {
            get { return !String.IsNullOrEmpty(this.AuthorizationToken) && !String.IsNullOrEmpty(this.SessionId); }
        }

        public string SessionId { get; set; }

        public string UserId { get; protected set; }

        #endregion

        #region Login

        /// <summary>
        /// Attempts to log into Google's Music service using Google's ClientLogin.
        /// </summary>
        /// <param name="email">Required. The email address of the Google account to log into.</param>
        /// <param name="password">Required. The password of the Google account to log into.</param>
        /// <returns>A Result object indicating whether the login was successful.</returns>
        public Result<bool> Login_OldClientLogin(string email, string password)
        {
            Http test = new Http();
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

                    response = http_old.Request(SetupWebRequest("https://www.google.com/accounts/ClientLogin", Http_Old.POST), form).ToUTF8();
                }

                Match regex = AUTH_REGEX.Match(response);

                if (regex.Success)
                {
                    this.AuthorizationToken = regex.Groups["AUTH"].Value;
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
                using (HttpWebResponse response = http_old.Request(SetupWebRequest("https://play.google.com/music/listen?hl=en&u=0", Http_Old.HEAD, false)))
                {
                    cookies = response.Cookies;
                }

                bool success = false;
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name.Equals("xt"))
                    {
                        success = true;
                        //_xt = cookie.Value;
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
        public async Task<Result<bool>> LoginAsync_Old(string email, string password, CancellationToken cancellationToken = default(CancellationToken))
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

                    response = await (await http_old.RequestAsync(SetupWebRequest("https://www.google.com/accounts/ClientLogin", Http_Old.POST), form, cancellationToken)).ToUTF8Async();
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                Match regex = AUTH_REGEX.Match(response);

                if (regex.Success)
                {
                    this.AuthorizationToken = regex.Groups["AUTH"].Value;
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
                using (HttpWebResponse response = await http_old.RequestAsync(SetupWebRequest("https://play.google.com/music/listen?hl=en&u=0", Http_Old.HEAD, false)))
                {
                    cookies = response.Cookies;
                }

                bool success = false;
                foreach (Cookie cookie in cookies)
                {
                    if (cookie.Name.Equals("xt"))
                    {
                        success = true;
                        //_xt = cookie.Value;
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
        /// Asynchronously attempts to log into Google's Music service.
        /// </summary>
        /// <param name="email">Required. The email address of the Google account to log into.</param>
        /// <param name="password">Required. The password of the Google account to log into.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>A Result object indicating whether the login was successful.</returns>
        public async Task<Result<bool>> Login(string email, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Step 1: Get authorization token
            var form = new[]
                    {
                        new KeyValuePair<string, string>("service", "sj"),
                        new KeyValuePair<string, string>("Email", email),
                        new KeyValuePair<string, string>("Passwd", password)
                    };
            try
            {
                string response;
                using (var formContent = new FormUrlEncodedContent(form))
                {
                    response = await (await http.Client.PostAsync("https://www.google.com/accounts/ClientLogin", formContent, cancellationToken)).Content.ReadAsStringAsync();
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                Match regex = AUTH_REGEX.Match(response);
                
                if (regex.Success)
                {
                    AuthorizationToken = regex.Groups["AUTH"].Value;
                    http.Client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "GoogleLogin auth=" + AuthorizationToken);
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
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An error occurred while attempting to retrieve an authorization token."), e);
            }


            // Step 2: Get authorization cookie
            try
            {
                string response = await http.Client.GetStringAsync("https://play.google.com/music/listen?u=0");

                this.UserId = AUTH_USER_ID_REGEX.Match(response).Groups["USERID"].Value;

                if ((http.Settings.CookieContainer.GetCookie("https://play.google.com/music/listen", "xt")) == null)
                    return new Result<bool>(false, false, this, "Unable to retrieve the proper authorization cookies from Google.");
            }
            catch (Exception e)
            {
                return new Result<bool>(false, false, this, e.ToString("An error occurred while attempting to retrieve authorization cookies."), e);
            }


            // Step 3: Generate session ID
            this.SessionId = GenerateRandomSessionId();
            return new Result<bool>(true, true, this);
        }

        private static string GenerateRandomSessionId()
        {
            Random random = new Random();
            var chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            return new string(chars.Select(c => chars[random.Next(chars.Length)]).Take(12).ToArray());
        }

        private string AppendXt(string url)
        {
            string xt = http.Settings.CookieContainer.GetCookie("https://play.google.com/music/listen", "xt");
            if (!String.IsNullOrEmpty(xt))
                url += (url.Contains('?') ? '&' : '?') + "u=0&xt=" + xt;
            return url;
        }

        public void Logout()
        {
            http.Settings.CookieContainer = new CookieContainer();
            http.Client.DefaultRequestHeaders.Remove("Authorization");
            this.AuthorizationToken = null;
            this.SessionId = null;
        }


        #endregion

        #region Web

        private HttpWebRequest SetupWebRequest(string address, string method = Http_Old.GET, bool autoRedirect = true)
        {
            if (address.Contains("play.google.com/music/services/"))
                address += (address.Contains('?') ? '&' : '?') + "u=0&xt=";// +_xt;
            

            HttpWebRequest request = http_old.CreateRequest(address, method);

            request.AllowAutoRedirect = autoRedirect;

            if (!String.IsNullOrEmpty(this.AuthorizationToken))
                request.Headers[HttpRequestHeader.Authorization] = "GoogleLogin auth=" + this.AuthorizationToken;

            return request;
        }

        #endregion

        #region GetSongCount

        public async Task<int> GetSongCount()
        {
            var form = new[] { new KeyValuePair<string, string>("json", String.Format("{{\"sessionId\":\"{0}\"}}", this.SessionId)) };
            string response;

            try
            {
                using (var formContent = new FormUrlEncodedContent(form))
                {
                    response = await (await http.Client.PostAsync(AppendXt("https://play.google.com/music/services/getstatus"), formContent)).Content.ReadAsStringAsync();
                }

                return JObject.Parse(response)["availableTracks"].ToObject<int>();
            }
            catch (Exception) { return -1; }

            //HttpWebRequest request = SetupWebRequest("https://play.google.com/music/services/getstatus", Http_Old.POST);
            //return http_old.Request(request, "application/x-www-form-urlencoded", new byte[0]).ToUTF8();
        }

        #endregion

        #region GetAllSongs

        /// <summary>
        /// Retrieves all songs in the user's library.
        /// </summary>
        /// <param name="results">Optional. The collection to add the songs to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.HashSet"/>.</param>
        /// <param name="lockObject">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <returns>Returns a Task containing the data structure containing the new songs.</returns>
        public async Task<ICollection<Song>> GetAllSongs(ICollection<Song> results = null, object lockObject = null)
        {
            if (results == null)
            {
                results = new HashSet<Song>();
                // Note: Lock object only if results weren't null. Otherwise, it is unnecessary overhead (since they don't have access to results)
                lockObject = null;
            }

            string response = await GetAllSongs_Request();

            return await Task.FromResult(GetAllSongs_Parse(response, results, lockObject));
        }

        private async Task<string> GetAllSongs_Request()
        {
            string url = AppendXt(String.Format(@"https://play.google.com/music/services/streamingloadalltracks?json={{""tier"":1,""requestCause"":1,""requestType"":1,""sessionId"":""{0}""}}&format=jsarray",
                this.SessionId));
            return await http.Client.GetStringAsync(url);
        }

        private ICollection<Song> GetAllSongs_Parse(string javascriptData, ICollection<Song> results, object lockObject)
        {
            //Match match = GET_ALL_SONGS_REGEX.Match(javascriptData);
            var match = GET_ALL_SONGS_REGEX.Match(javascriptData);

            while (match.Success)
            {
                dynamic trackArray;
                try
                {
                    dynamic jsonData = JsonConvert.DeserializeObject("{stringArray:" + match.Groups["TRACKS"].Value + "}");

                    trackArray = jsonData.stringArray[0];

                    foreach (var track in trackArray)
                    {
                        Song song = Song.Build(track);

                        if (lockObject == null)
                            results.Add(song);
                        else
                            lock (lockObject) { results.Add(song); }
                    }
                }
                catch (Exception) { }

                match = match.NextMatch();
            }
            


            return results;
        }

        #endregion

        #region GetDeleted

        #endregion

    }
}
