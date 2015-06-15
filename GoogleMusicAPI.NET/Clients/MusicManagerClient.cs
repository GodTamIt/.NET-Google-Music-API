using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleMusic.Clients
{
    public class MusicManagerClient
    {

        #region Members
        private const string USER_AGENT = "Music Manager (1, 0, 55, 7425 HTTPS - Windows)";

        private string _ClientId;
        private string _ClientSecret;
        private string _AuthorizationCode;
        private string _RefreshToken;
        private string _AccessToken;

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

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId
        {
            get { return _ClientId; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("ClientId cannot be null or empty.", "value");

                _ClientId = value;
            }
        }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientSecret
        {
            get { return _ClientSecret; }
            set
            {
                if (String.IsNullOrEmpty(value))
                    throw new ArgumentException("ClientSecret cannot be null or empty.", "value");

                _ClientSecret = value;
            }
        }

        /// <summary>
        /// Read-only. The authorization code retrieved by the user to authorize access from the MusicManagerClient.
        /// </summary>
        public string AuthorizationCode
        {
            get { return _AuthorizationCode; }
            set { _AuthorizationCode = value; }
        }

        /// <summary>
        /// Read-only. The token used to renew the access token. To get the refresh token, <see cref="MusicManagerClient.GetRefreshToken"/>.
        /// </summary>
        public string RefreshToken
        {
            get { return _RefreshToken; }
        }

        /// <summary>
        /// Read-only. The current access token. To renew, <see cref="MusicManagerClient.GetRefreshToken"/>.
        /// </summary>
        public string AccessToken
        {
            get { return _AccessToken; }
        }

        #endregion

        #region Authorization/Deauthorization

        /// <summary>
        /// Gets the URL the user must visit to retrieve the ClientSecret.
        /// </summary>
        /// <returns>Returns the URL calculated.</returns>
        public string GetAuthorizationCodeUrl()
        {
            string encodedClientId = Http.UrlEncode(_ClientId, "ISO-8859-1");
            string encodedScope = Http.UrlEncode("https://www.googleapis.com/auth/musicmanager", "ISO-8859-1");

            return String.Format("https://accounts.google.com/o/oauth2/auth?response_type=code&client_id={0}&redirect_uri=urn:ietf:wg:oauth:2.0:oob&scope={1}",
                encodedClientId, encodedScope);
        }

        /// <summary>
        /// Attempts to retrieve the refresh token from Google given the user's authorization code.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <returns>Returns the result from Google's servers.</returns>
        public string GetRefreshToken(string authorizationCode)
        {
            _AuthorizationCode = authorizationCode;

            Dictionary<String, String> fieldsOA = new Dictionary<String, String>
            {
                {"code", authorizationCode},
                {"client_id",  ClientId},
                {"client_secret", ClientSecret},
                {"redirect_uri", "urn:ietf:wg:oauth:2.0:oob"},
                {"grant_type", "authorization_code"},
            };

            FormBuilder builderOA = new FormBuilder();
            builderOA.AddFields(fieldsOA);
            builderOA.Close();

            byte[] response = http.GetContent(http.UploadDataSync(SetupWebRequest("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes()));

            // Bytes -> String -> JSON
            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(Encoding.UTF8.GetString(response));

            _RefreshToken = json["refresh_token"];
            return _RefreshToken;
        }

        /// <summary>
        /// Asynchronously attempts to retrieve the refresh token from Google given the user's authorization code.
        /// </summary>
        /// <param name="authorizationCode">Required. The authorization code retrieved by the user to authorize access from the MusicManagerClient.</param>
        /// <returns>Returns a task the result from Google's servers.</returns>
        public async Task<string> GetRefreshTokenAsync(string authorizationCode)
        {
            _AuthorizationCode = authorizationCode;

            Dictionary<String, String> fieldsOA = new Dictionary<String, String>
            {
                {"code", authorizationCode},
                {"client_id",  ClientId},
                {"client_secret", ClientSecret},
                {"redirect_uri", "urn:ietf:wg:oauth:2.0:oob"},
                {"grant_type", "authorization_code"},
            };

            FormBuilder builderOA = new FormBuilder();
            builderOA.AddFields(fieldsOA);
            builderOA.Close();
            
            byte[] response = await http.GetContentAsync(await http.UploadDataAsync(SetupWebRequest("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes()));

            // Bytes -> String -> JSON
            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(Encoding.UTF8.GetString(response));

            _RefreshToken = json["refresh_token"];
            return _RefreshToken;
        }

        /// <summary>
        /// Renews the access token with the refresh token.
        /// </summary>
        /// <returns>Returns the new access token.</returns>
        public string RenewAccessToken()
        {
            if (String.IsNullOrEmpty(RefreshToken))
                throw new InvalidOperationException("The refresh token cannot be null or empty. The refresh token must be retrieved successfully before calling this function.");

            Dictionary<String, String> fieldsOA = new Dictionary<String, String>
            {
                {"refresh_token", RefreshToken},
                {"client_id",  ClientId},
                {"client_secret", ClientSecret},
                {"grant_type", "refresh_token"},
            };

            FormBuilder builderOA = new FormBuilder();
            builderOA.AddFields(fieldsOA);
            builderOA.Close();

            byte[] response = http.GetContent(http.UploadDataSync(SetupWebRequest("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes()));

            // Bytes -> String -> JSON
            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(Encoding.UTF8.GetString(response));

            _AccessToken = json["access_token"];
            return _AccessToken;
        }

        /// <summary>
        /// Asynchronously renews the access token with the refresh token.
        /// </summary>
        /// <returns>Returns the new access token.</returns>
        public async Task<string> RenewAccessTokenAsync()
        {
            if (String.IsNullOrEmpty(RefreshToken))
                throw new InvalidOperationException("The refresh token cannot be null or empty. The refresh token must be retrieved successfully before calling this function.");

            Dictionary<String, String> fieldsOA = new Dictionary<String, String>
            {
                {"refresh_token", RefreshToken},
                {"client_id",  ClientId},
                {"client_secret", ClientSecret},
                {"grant_type", "refresh_token"},
            };

            FormBuilder builderOA = new FormBuilder();
            builderOA.AddFields(fieldsOA);
            builderOA.Close();

            byte[] response = await http.GetContentAsync(await http.UploadDataAsync(SetupWebRequest("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes()));

            // Bytes -> String -> JSON
            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(Encoding.UTF8.GetString(response));

            _AccessToken = json["access_token"];
            return _AccessToken;
        }

        /// <summary>
        /// Deauthorizes the client from accessing an account.
        /// </summary>
        public void Deauthorize()
        {
            _AuthorizationCode = String.Empty;
            _RefreshToken = String.Empty;
            _AccessToken = String.Empty;
        }

        #endregion

        #region Web

        private HttpWebRequest SetupWebRequest(string address)
        {
            HttpWebRequest request = http.SetupRequest(address);

            if (_AccessToken != null)
                request.Headers[HttpRequestHeader.Authorization] = "Bearer " + _AccessToken;

            return request;
        }

        #endregion

    }
}
