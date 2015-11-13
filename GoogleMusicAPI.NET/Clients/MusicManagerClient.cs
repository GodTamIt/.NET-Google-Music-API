using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.IO;

using System.Net.Http;
using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using GoogleMusic.Net;

namespace GoogleMusic.Clients
{
    public class MusicManagerClient : IClient
    {

        #region Members
        private const string USER_AGENT = "Music Manager (1, 0, 55, 7425 HTTPS - Windows)";
        private const string ANDROID_URL = "https://android.clients.google.com/upsj/";
        private const string SJ_URL = "https://www.googleapis.com/sj/v1.1/";
        private const string PROTO_CONTENT_TYPE = "application/x-google-protobuf";
        private Random RANDOM = new Random();

        private Http http;
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of a MusicManagerClient.
        /// </summary>
        /// <param name="clientId">Required. The client ID of the program accessing the MusicManager API.</param>
        /// <param name="clientSecret">Required. The secret string of the program given by Google.</param>
        /// <exception cref="System.ArgumentException">Thrown when clientId is an empty string or null.</exception>
        public MusicManagerClient(string clientId, string clientSecret)
        {
            http = new Http();
            http.UserAgent = USER_AGENT;

            // Assign property to get error handling
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        #endregion

        #region Properties

        public bool IsLoggedIn
        {
            get { return !(String.IsNullOrEmpty(this.AccessToken) || String.IsNullOrEmpty(this.RefreshToken) || String.IsNullOrEmpty(this.AuthorizationCode)); }
        }

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId { get; set; }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Read-only. The authorization code retrieved by the user to authorize access from the MusicManagerClient.
        /// </summary>
        public string AuthorizationCode { get; protected set; }

        /// <summary>
        /// Read-only. The token used to renew the access token. To get the refresh token, see <see cref="MusicManagerClient.GetRefreshToken"/>.
        /// </summary>
        public string RefreshToken { get; protected set; }

        /// <summary>
        /// Read-only. The current access token. To renew, see <see cref="MusicManagerClient.GetRefreshToken"/>.
        /// </summary>
        public string AccessToken { get; protected set; }

        #endregion

        #region Login

        /// <summary>
        /// Gets the URL the user must visit to retrieve the ClientSecret.
        /// </summary>
        /// <returns>Returns the URL calculated.</returns>
        public string GetAuthorizationCodeUrl()
        {
            Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

            return String.Format("https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={0}&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope={1}",
                System.Web.HttpUtility.UrlEncode(this.ClientId, encoding), System.Web.HttpUtility.UrlEncode("https://www.googleapis.com/auth/musicmanager", encoding));
        }

        /// <summary>
        /// Asynchronously attempts to retrieve the refresh token from Google given the user's authorization code.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>Returns the result from Google's servers.</returns>
        public async Task<Result<string>> GetRefreshToken(string authorizationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.AuthorizationCode = authorizationCode;
            string response = null;

            try
            {
                using (var content = new MultipartFormDataContent("----------" + DateTime.Now.Ticks.ToString("x")))
                {
                    content.Add(new StringContent(this.AuthorizationCode), "code");
                    content.Add(new StringContent(this.ClientId), "client_id");
                    content.Add(new StringContent(this.ClientSecret), "client_secret");
                    content.Add(new StringContent("urn:ietf:wg:oauth:2.0:oob"), "redirect_uri");
                    content.Add(new StringContent("grant_type"), "authorization_code");

                    response = await (await http.Client.PostAsync("https://accounts.google.com/o/oauth2/token", content, cancellationToken)).Content.ReadAsStringAsync();
                }

                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(response);

                this.RefreshToken = json["refresh_token"];
            }
            catch (Exception e) { return new Result<string>(false, response, this, e); }

            return new Result<string>(true, this.RefreshToken, this);
        }

        /// <summary>
        /// Asynchronously renews the access token with the refresh token.
        /// </summary>
        /// <returns>Returns the new access token.</returns>
        public async Task<Result<string>> GetAccessToken(string refreshToken = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (refreshToken != null)
                this.RefreshToken = refreshToken;

            if (String.IsNullOrEmpty(RefreshToken))
                return new Result<string>(false, String.Empty, this);

            string response = null;

            try
            {
                using (var content = new MultipartFormDataContent("----------" + DateTime.Now.Ticks.ToString("x")))
                {
                    content.Add(new StringContent(this.RefreshToken), "refresh_token");
                    content.Add(new StringContent(this.ClientId), "client_id");
                    content.Add(new StringContent(this.ClientSecret), "client_secret");
                    content.Add(new StringContent("refresh_token"), "grant_type");

                    response = await (await http.Client.PostAsync("https://accounts.google.com/o/oauth2/token", content, cancellationToken)).Content.ReadAsStringAsync();
                }

                // Bytes -> String -> JSON
                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(response);

                this.AccessToken = json["access_token"];
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }

            return new Result<string>(true, this.AccessToken, this);
        }

        /// <summary>
        /// Asynchronously authorizes the client. Internally calls <code>GetRefreshToken</code> and <code>GetAccessToken</code>.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns></returns>
        public async Task<Result> Login(string authorizationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            var refreshTask = await GetRefreshToken(authorizationCode, cancellationToken);
            if (!refreshTask.Success)
                return new Result(refreshTask.Success, this, refreshTask.InnerException);

            var accessTask = await GetAccessToken(cancellationToken: cancellationToken);

            return new Result(accessTask.Success, this, accessTask.InnerException);
        }



        /// <summary>
        /// Deauthorizes the client from accessing an account.
        /// </summary>
        public void Logout()
        {
            this.AuthorizationCode = String.Empty;
            this.RefreshToken = String.Empty;
            this.AccessToken = String.Empty;
        }

        #endregion

        #region Uploading

        public async Task<Result<string>> AuthorizeUploader(string uploaderId, string uploaderReadableName)
        {
            return null;
        }

        #endregion

    }
}
