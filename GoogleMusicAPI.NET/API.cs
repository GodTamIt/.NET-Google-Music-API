using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
//using wireless_android_skyjam;
using ProtoBuf;

using GoogleMusic.Clients;

namespace GoogleMusic
{
    /// <summary>
    /// The delegate type invoked when an asynchronous GoogleMusic function makes progress.
    /// </summary>
    /// <param name="progress">The new progress to report, in range 0.0 to 1.0.</param>
    public delegate void TaskProgressEventHandler(double progress);
    /// <summary>
    /// The delegate type invoked when an asynchronous GoogleMusic function finishes a task.
    /// </summary>
    public delegate void TaskCompleteEventHandler();

    /// <summary>
    /// A class that wraps the functionality of individual Google Music clients to provide a robust, full implementation of the Google Music API.
    /// </summary>
    public class API
    {
        #region Members
        private WebClient _WebClient;
        private MusicManagerClient _MusicManager;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Google Music API.
        /// </summary>
        /// <param name="clientId">The emulated ID of the program accessing the MusicManager API.</param>
        /// <param name="clientSecret">The secret string of the program given by Google.</param>
        public API(string clientId, string clientSecret)
        {
            _WebClient = new WebClient();
            _MusicManager = new MusicManagerClient(clientId, clientSecret);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The underlying WebClient object of the current API instance.
        /// </summary>
        public WebClient WebClient
        {
            get { return _WebClient; }
            set { _WebClient = value; }
        }

        /// <summary>
        /// The underlying MusicManagerClient object of the current API instance.
        /// </summary>
        public MusicManagerClient MusicManager
        {
            get { return _MusicManager; }
            set { _MusicManager = value; }
        }

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId
        {
            get { return _MusicManager.ClientId; }
            set { _MusicManager.ClientId = value; }
        }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        public string ClientSecret
        {
            get { return _MusicManager.ClientSecret; }
            set { _MusicManager.ClientSecret = value; }
        }

        /// <summary>
        /// Returns the current version of the plugin.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
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
                bool webResult = _WebClient.Login(email, password);

                _MusicManager.GetRefreshToken(authorizationCode);
                _MusicManager.RenewAccessToken();


                return (webResult && !String.IsNullOrEmpty(_MusicManager.AccessToken));
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
                Task<bool> webLogin = _WebClient.LoginAsync(email, password);

                await _MusicManager.GetRefreshTokenAsync(authorizationCode);
                await _MusicManager.RenewAccessTokenAsync();

                bool webResult = await webLogin;

                return (webResult && !String.IsNullOrEmpty(_MusicManager.AccessToken));
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
            _WebClient.Logout();
            _MusicManager.Logout();
        }

        #endregion

    }
}
