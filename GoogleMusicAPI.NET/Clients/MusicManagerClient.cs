using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using GoogleMusic.Net;

namespace GoogleMusic.Clients
{
    public class MusicManagerClient : IClient
    {

        #region Members
        private const string USER_AGENT = "Music Manager (1, 0, 55, 7425 HTTPS - Windows)";

        private string _clientId;
        private string _clientSecret;
        private string _accessToken;

        private Http_Old http;
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
            http = new Http_Old();
            http.UserAgent = USER_AGENT;

            // Assign property to get error handling
            ClientId = clientId;
            ClientSecret = clientSecret;
        }

        #endregion

        #region Properties

        public bool IsLoggedIn
        {
            get { return !(String.IsNullOrEmpty(_accessToken) || String.IsNullOrEmpty(this.RefreshToken) || String.IsNullOrEmpty(this.AuthorizationCode)); }
        }

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId
        {
            get { return _clientId; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("ClientId cannot be null or empty.", "value");

                _clientId = value;
            }
        }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientSecret
        {
            get { return _clientSecret; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("ClientSecret cannot be null or empty.", "value");

                _clientSecret = value;
            }
        }

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
            string encodedClientId = System.Web.HttpUtility.UrlEncode(_clientId, Encoding.GetEncoding("ISO-8859-1"));
            string encodedScope = System.Web.HttpUtility.UrlEncode("https://www.googleapis.com/auth/musicmanager", Encoding.GetEncoding("ISO-8859-1"));

            return String.Format("https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={0}&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope={1}",
                encodedClientId, encodedScope);
        }

        /// <summary>
        /// Attempts to retrieve the refresh token from Google given the user's authorization code.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <returns>Returns the result from Google's servers.</returns>
        public Result<string> GetRefreshToken(string authorizationCode)
        {
            this.AuthorizationCode = authorizationCode;

            try
            {
                HttpWebResponse response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("code", authorizationCode);
                    form.AddField("client_id", ClientId);
                    form.AddField("client_secret", ClientSecret);
                    form.AddField("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
                    form.AddField("grant_type", "authorization_code");
                    form.AddEnding();

                    HttpWebRequest request = SetupWebRequest("https://accounts.google.com/o/oauth2/token", Http_Old.POST);
                    response = http.Request(request, form);
                }

                // Bytes -> String -> JSON
                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(response.ToUTF8());

                this.RefreshToken = json["refresh_token"];
                return new Result<string>(true, this.RefreshToken, this);
            }
            catch (WebException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("A network error occurred while attempting to retrieve an OAuth refresh token."), e);
            }
            catch (JsonException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An error occurred trying to parse the OAuth refresh token response."), e);
            }
            catch (Exception e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An unknown error occurred."), e);
            }
            
        }

        /// <summary>
        /// Asynchronously attempts to retrieve the refresh token from Google given the user's authorization code.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>Returns a task the result from Google's servers.</returns>
        public async Task<Result<string>> GetRefreshTokenAsync(string authorizationCode, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.AuthorizationCode = authorizationCode;           
            
            try
            {
                HttpWebResponse response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("code", authorizationCode);
                    form.AddField("client_id", ClientId);
                    form.AddField("client_secret", ClientSecret);
                    form.AddField("redirect_uri", "urn:ietf:wg:oauth:2.0:oob");
                    form.AddField("grant_type", "authorization_code");
                    form.AddEnding();

                    response = await http.RequestAsync(SetupWebRequest("https://accounts.google.com/o/oauth2/token", Http_Old.POST), form, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Bytes -> String -> JSON
                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(await response.ToUTF8Async(cancellationToken: cancellationToken));

                if (cancellationToken.IsCancellationRequested)
                    return null;

                this.RefreshToken = json["refresh_token"];
                return new Result<string>(true, this.RefreshToken, this);
            }
            catch (WebException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("A network error occurred while attempting to retrieve an OAuth refresh token."), e);
            }
            catch (JsonException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An error occurred trying to parse the OAuth refresh token response."), e);
            }
            catch (Exception e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An unknown error occurred."), e);
            }
        }

        /// <summary>
        /// Renews the access token with the refresh token.
        /// </summary>
        /// <returns>Returns the new access token.</returns>
        public Result<string> RenewAccessToken()
        {
            if (String.IsNullOrEmpty(RefreshToken))
                return new Result<string>(false, String.Empty, this, "OAuth: The refresh token cannot be null or empty when retrieving the access token.");

            try
            {
                HttpWebResponse response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("refresh_token", RefreshToken);
                    form.AddField("client_id", ClientId);
                    form.AddField("client_secret", ClientSecret);
                    form.AddField("grant_type", "refresh_token");
                    form.AddEnding();

                    response = http.Request(SetupWebRequest("https://accounts.google.com/o/oauth2/token", Http_Old.POST), form);
                }

                // Bytes -> String -> JSON
                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(response.ToUTF8());

                _accessToken = json["access_token"];
                return new Result<string>(true, _accessToken, this);
            }
            catch (WebException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("A network error occurred while attempting to retrieve an OAuth access token."), e);
            }
            catch (JsonException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An error occurred trying to parse the OAuth access token response."), e);
            }
            catch (Exception e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An unknown error occurred."), e);
            }
        }

        /// <summary>
        /// Asynchronously renews the access token with the refresh token.
        /// </summary>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>Returns the new access token.</returns>
        public async Task<Result<string>> RenewAccessTokenAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            if (String.IsNullOrEmpty(RefreshToken))
                return new Result<string>(false, String.Empty, this, "OAuth: The refresh token cannot be null or empty when retrieving the access token.");

            try
            {
                HttpWebResponse response;
                using (FormBuilder form = new FormBuilder())
                {
                    form.AddField("refresh_token", RefreshToken);
                    form.AddField("client_id", ClientId);
                    form.AddField("client_secret", ClientSecret);
                    form.AddField("grant_type", "refresh_token");
                    form.AddEnding();

                    response = await http.RequestAsync(SetupWebRequest("https://accounts.google.com/o/oauth2/token", Http_Old.POST), form, cancellationToken);
                }

                if (cancellationToken.IsCancellationRequested)
                    return null;

                // Bytes -> String -> JSON
                Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(await response.ToUTF8Async(cancellationToken: cancellationToken));

                if (cancellationToken.IsCancellationRequested)
                    return null;

                _accessToken = json["access_token"];
                return new Result<string>(true, _accessToken, this);
            }
            catch (WebException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("A network error occurred while attempting to retrieve an OAuth access token."), e);
            }
            catch (JsonException e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An error occurred trying to parse the OAuth access token response."), e);
            }
            catch (Exception e)
            {
                return new Result<string>(false, String.Empty, this, e.ToString("An unknown error occurred."), e);
            }
        }

        /// <summary>
        /// Deauthorizes the client from accessing an account.
        /// </summary>
        public void Logout()
        {
            this.AuthorizationCode = String.Empty;
            this.RefreshToken = String.Empty;
            _accessToken = String.Empty;
        }

        #endregion

        #region Web

        private HttpWebRequest SetupWebRequest(string address, string method = Http_Old.GET)
        {
            HttpWebRequest request = http.CreateRequest(address, method);

            if (_accessToken != null)
                request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _accessToken;
            
            return request;
        }

        #endregion

    }
}
