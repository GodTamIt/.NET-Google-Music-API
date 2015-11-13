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
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the Google Music API.
        /// </summary>
        /// <param name="clientId">The emulated ID of the program accessing the MusicManager API.</param>
        /// <param name="clientSecret">The secret string of the program given by Google.</param>
        public API(string clientId, string clientSecret)
        {
            this.WebClient = new WebClient();
            this.MusicManager = new MusicManagerClient(clientId, clientSecret);
        }

        #endregion

        #region Properties

        /// <summary>
        /// The underlying WebClient object of the current API instance.
        /// </summary>
        public WebClient WebClient { get; set; }

        /// <summary>
        /// The underlying MusicManagerClient object of the current API instance.
        /// </summary>
        public MusicManagerClient MusicManager { get; set; }

        /// <summary>
        /// The emulated ID of the program accessing the MusicManager API.
        /// </summary>
        /// <exception cref="System.ArgumentException">Thrown when property is set to empty string or null.</exception>
        public string ClientId
        {
            get { return this.MusicManager.ClientId; }
            set { this.MusicManager.ClientId = value; }
        }

        /// <summary>
        /// The secret string of the program given by Google.
        /// </summary>
        public string ClientSecret
        {
            get { return this.MusicManager.ClientSecret; }
            set { this.MusicManager.ClientSecret = value; }
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

        /// <summary>
        /// Returns whether the current instance of the API is logged in.
        /// </summary>
        public bool IsLoggedIn
        {
            get { return this.WebClient.IsLoggedIn && this.MusicManager.IsLoggedIn; }
        }

        #endregion

        #region Login/Logout

        /// <summary>
        /// Asynchronously attempts to log clients into the Google Music API.
        /// </summary>
        /// <param name="email">The email of the Google account.</param>
        /// <param name="password">The password of the Google account.</param>
        /// <param name="authorizationCode">The authorization code provided by the user.</param>
        /// <returns>Returns whether the login attempt was successful.</returns>
        public async Task<bool> Login(string email, string password, string authorizationCode)
        {
            Logout();
            Task<Result<string>> webLogin = this.WebClient.Login(email, password);

            //Result<string> musicManagerRefreshToken = await this.MusicManager.GetRefreshTokenAsync(authorizationCode);
            //Result<string> musicManagerAccessToken = await this.MusicManager.RenewAccessTokenAsync();

            Result<string> webResult = await webLogin;

            return (this.WebClient.IsLoggedIn && this.MusicManager.IsLoggedIn);
        }

        /// <summary>
        /// Logs out of and deauthorizes any access to accounts.
        /// </summary>
        public void Logout()
        {
            this.WebClient.Logout();
            this.MusicManager.Logout();
        }

        #endregion

        #region GetSongCount

        public async Task<Result<int>> GetSongCount()
        {
            return await this.WebClient.GetSongCount();
        }

        #endregion

        #region GetAllSongs



        #endregion

    }
}
