using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using wireless_android_skyjam;
//using ProtoBuf;

using GoogleMusic.Clients;

namespace GoogleMusic
{


    /// <summary>
    /// A class that wraps the functionality of individual Google Music clients to provide a robust, full implementation of the Google Music API.
    /// </summary>
    public class API
    {
        #region Members
        private WebClient _webClient;
        private MusicManagerClient _musicManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Google Music API.
        /// </summary>
        /// <param name="clientId">The emulated ID of the program accessing the MusicManager API.</param>
        /// <param name="clientSecret">The secret string of the program given by Google.</param>
        public API(string clientId, string clientSecret)
        {
            _webClient = new WebClient();
            _musicManager = new MusicManagerClient(clientId, clientSecret);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The underlying WebClient object of the current API instance.
        /// </summary>
        public WebClient WebClient
        {
            get { return _webClient; }
            set { _webClient = value; }
        }

        /// <summary>
        /// The underlying MusicManagerClient object of the current API instance.
        /// </summary>
        public MusicManagerClient MusicManager
        {
            get { return _musicManager; }
            set { _musicManager = value; }
        }

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId
        {
            get { return _musicManager.ClientId; }
            set { _musicManager.ClientId = value; }
        }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        public string ClientSecret
        {
            get { return _musicManager.ClientSecret; }
            set { _musicManager.ClientSecret = value; }
        }

        /// <summary>
        /// Returns the current version of the plugin.
        /// </summary>
        public string Version
        {
            get
            {
                return String.Format("{0}.{1}",
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString(),
                    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString());
            }
        }

        #endregion

        #region Login/Logout

        /// <summary>
        /// Attempts to log clients into the Google Music API.
        /// </summary>
        /// <param name="email">The email of the Google account.</param>
        /// <param name="password">The password of the Google account.</param>
        /// <param name="authorizationCode">The authorization code provided by the user.</param>
        /// <returns>Returns whether the login was successful.</returns>
        public bool Login(string email, string password, string authorizationCode)
        {
            try
            {
                Result<bool> webResult = _webClient.Login(email, password);

                var result1 = _musicManager.GetRefreshToken(authorizationCode);
                var result2 = _musicManager.RenewAccessToken();

                return (webResult.Success && !String.IsNullOrEmpty(_musicManager.AccessToken));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Attempts to log clients into the Google Music API.
        /// </summary>
        /// <param name="email">The email of the Google account.</param>
        /// <param name="password">The password of the Google account.</param>
        /// <param name="authorizationCode">The authorization code provided by the user.</param>
        /// <returns>Returns whether the login was successful.</returns>
        public async Task<bool> LoginAsync(string email, string password, string authorizationCode)
        {
            try
            {
                Task<Result<bool>> webLogin = _webClient.LoginAsync(email, password);

                var result1 = await _musicManager.GetRefreshTokenAsync(authorizationCode);
                var result2 = await _musicManager.RenewAccessTokenAsync();

                Result<bool> webResult = await webLogin;
                
                return (webResult.Success && !String.IsNullOrEmpty(_musicManager.AccessToken));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Logs out of and deauthorizes any access to accounts.
        /// </summary>
        public void Logout()
        {
            _webClient.Logout();
            _musicManager.Logout();
        }

        #endregion

    }
}
