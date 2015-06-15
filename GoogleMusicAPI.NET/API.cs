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

    public class API
    {
        #region Members
        private WebClient _WebClient;
        private MusicManagerClient _MusicManager;

        #endregion

        #region Constructor

        public API(string clientId, string clientSecret)
        {
            _WebClient = new WebClient();
            _MusicManager = new MusicManagerClient(clientId, clientSecret);
        }

        #endregion

        #region Properties

        public WebClient WebClient
        {
            get { return _WebClient; }
            set { _WebClient = value; }
        }

        public MusicManagerClient MusicManager
        {
            get { return _MusicManager; }
            set { _MusicManager = value; }
        }

        #endregion

        #region Login/Logout

        /// <summary>
        /// Attempts to log clients into the Google Music API.
        /// </summary>
        /// <param name="email">The email of the Google account.</param>
        /// <param name="password">The password of the Google account.</param>
        /// <param name="authorizationCode">The authorization code</param>
        /// <returns></returns>
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
            _MusicManager.Deauthorize();
        }

        #endregion

        #region Properties

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
    }
}
