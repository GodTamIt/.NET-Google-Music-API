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
        private static readonly Regex GET_ALL_SONGS_REGEX = new Regex(@"window.parent\['slat_process'\]\((?<SONGS>.*?)\);\nwindow.parent\['slat_progress'\]", RegexOptions.Singleline);

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
        /// Asynchronously attempts to log into Google's Music service.
        /// </summary>
        /// <param name="email">Required. The email address of the Google account to log into.</param>
        /// <param name="password">Required. The password of the Google account to log into.</param>
        /// <param name="cancellationToken">Optional. The token to monitor for cancellation requests.</param>
        /// <returns>A Result object indicating whether the login was successful.</returns>
        public async Task<Result<string>> Login(string email, string password, CancellationToken cancellationToken = default(CancellationToken))
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
                        return new Result<string>(false, "Google returned the following error while trying to retrieve an authorization token:\r\n\r\n" + regex.Groups["ERROR"].Value, this);
                    else
                        return new Result<string>(false, "An unknown error occurred.", this);
                }
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }


            // Step 2: Get authorization cookie
            try
            {
                string response = await http.Client.GetStringAsync("https://play.google.com/music/listen?u=0");

                this.UserId = AUTH_USER_ID_REGEX.Match(response).Groups["USERID"].Value;

                if ((http.Settings.CookieContainer.GetCookie("https://play.google.com/music/listen", "xt")) == null)
                    return new Result<string>(false, "Unable to retrieve the proper authorization cookies from Google.", this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }


            // Step 3: Generate session ID
            this.SessionId = GenerateRandomSessionId();
            return new Result<string>(true, String.Empty, this);
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
            http.Settings.CookieContainer.ClearCookies("https://www.google.com/");
            http.Settings.CookieContainer.ClearCookies("https://www.music.google.com/");
            http.Client.DefaultRequestHeaders.Remove("Authorization");
            this.AuthorizationToken = null;
            this.SessionId = null;
        }

        #endregion

        #region GetSongCount

        /// <summary>
        /// Asynchronously retrieves the number of songs in the current Google Music library.
        /// </summary>
        /// <returns>Returns a</returns>
        public async Task<Result<int>> GetSongCount()
        {
            if (!this.IsLoggedIn)
                return new Result<int>(false, -1, this, new ClientNotAuthorizedException(this));

            var form = new[] { new KeyValuePair<string, string>("json", String.Format("{{\"sessionId\":\"{0}\"}}", this.SessionId)) };
            string response;

            try
            {
                using (var formContent = new FormUrlEncodedContent(form))
                {
                    response = await (await http.Client.PostAsync(AppendXt("https://play.google.com/music/services/getstatus"), formContent)).Content.ReadAsStringAsync();
                }

                return new Result<int>(true, JObject.Parse(response)["availableTracks"].ToObject<int>(), this);
            }
            catch (Exception e) { return new Result<int>(false, -1, this, e); }
        }

        #endregion

        #region GetAllSongs

        /// <summary>
        /// Asynchronously retrieves all songs in the current Google Music library.
        /// </summary>
        /// <param name="results">Optional. The collection to add the songs to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.Dictionary"/>.</param>
        /// <param name="lockResults">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <returns>Returns a Task containing a <see cref="Result"/> object with the resulting collection of songs if successful.</returns>
        public async Task<Result<ICollection<KeyValuePair<Guid, Song>>>> GetAllSongs(ICollection<KeyValuePair<Guid, Song>> results = null, object lockResults = null)
        {
            if (!this.IsLoggedIn)
                return new Result<ICollection<KeyValuePair<Guid, Song>>>(false, results, this);

            if (results == null)
            {
                results = new Dictionary<Guid, Song>();
                // Note: Lock object only if results weren't null. Otherwise, it is unnecessary overhead (since they don't have access to results)
                lockResults = null;
            }
            
            // Step 1: Get response from Google
            var response = await GetAllSongs_Request();
            if (!response.Success)
                return new Result<ICollection<KeyValuePair<Guid, Song>>>(response.Success, results, response.Client, response.InnerException);


            // Step 2: Asynchronously parse result
            var parse = await Task.Run(() =>
            {
                var match = GET_ALL_SONGS_REGEX.Match(response.Value);
                Result<bool> parseResult;

                while (match.Success)
                {
                    // GetAllSongs is located in 0th index
                    parseResult = ParseSongs(0, match.Groups["SONGS"].Value, results, lockResults);

                    if (parseResult.Success)
                        return parseResult;

                    match = match.NextMatch();
                }
                return new Result<bool>(true, true, this);
            });

            return new Result<ICollection<KeyValuePair<Guid, Song>>>(parse.Success, results, this, parse.InnerException);
        }

        private async Task<Result<string>> GetAllSongs_Request()
        {
            try
            {
                string url = AppendXt(
                    String.Format(@"https://play.google.com/music/services/streamingloadalltracks?json={{""tier"":1,""requestCause"":1,""requestType"":1,""sessionId"":""{0}""}}&format=jsarray",
                    this.SessionId));

                return new Result<string>(true, await http.Client.GetStringAsync(url), this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }
        }

        #endregion

        #region GetDeletedSongs

        /// <summary>
        /// Asynchronously retrieves the deleted songs from the Google Music library.
        /// </summary>
        /// <param name="results">Optional. The collection to add the songs to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.Dictionary"/>.</param>
        /// <param name="lockResults">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <returns></returns>
        public async Task<Result<ICollection<KeyValuePair<Guid, Song>>>> GetDeletedSongs(ICollection<KeyValuePair<Guid, Song>> results = null, object lockResults = null)
        {
            if (!this.IsLoggedIn)
                return new Result<ICollection<KeyValuePair<Guid, Song>>>(false, results, this, new ClientNotAuthorizedException(this));
            else if (results == null)
            {
                results = new Dictionary<Guid, Song>();
                // Note: Lock object only if results weren't null. Otherwise, it is unnecessary overhead (since they don't have access to results)
                lockResults = null;
            }

            var requestResult = await GetDeletedSongs_Request();
            if (!requestResult.Success)
                return new Result<ICollection<KeyValuePair<Guid, Song>>>(requestResult.Success, results, this, requestResult.InnerException);


            var parseResult = await Task.Run(() => ParseSongs(1, requestResult.Value, results, lockResults));

            return new Result<ICollection<KeyValuePair<Guid, Song>>>(parseResult.Success, results, this, parseResult.InnerException);
        }

        private async Task<Result<string>> GetDeletedSongs_Request()
        {
            string url = AppendXt("https://play.google.com//music/services/loadautoplaylist?format=jsarray");
            var stringContent = new StringContent(String.Format(@"[[""{0}"",1,""{1}""],[""auto-playlist-trash""]]", this.SessionId, this.UserId));

            try
            {
                return new Result<string>(true, await (await http.Client.PostAsync(url, stringContent)).Content.ReadAsStringAsync(), this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }
        }

        #endregion

        #region DeleteSongs

        /// <summary>
        /// Asynchronously deletes songs from the Google Music library. This delete can be undone (within 28 days of deleting).
        /// </summary>
        /// <param name="songs">Required. The enumerable collection of songs to delete.</param>
        /// <returns>Returns a <see cref="Result"/> containing an enumerable collection of GUIDs that were successfully deleted.</returns>
        public async Task<Result<IEnumerable<Guid>>> DeleteSongs(IEnumerable<Song> songs)
        {
            if (!this.IsLoggedIn)
                return new Result<IEnumerable<Guid>>(false, new Guid[0], this, new ClientNotAuthorizedException(this));
            if (songs == null || songs.Count() < 1)
                return new Result<IEnumerable<Guid>>(true, new Guid[0], this);

            string json = await Task.Run(() => DeleteSongs_BuildJson(songs));

            var requestResult = await DeleteSongs_Request(json);

            return await Task.Run(() => DeleteSongs_ParseResponse(requestResult.Value, songs.Count()));
        }

        private string DeleteSongs_BuildJson(IEnumerable<Song> songs)
        {
            string[] guids = new string[songs.Count()];
            {
                int i = 0;
                foreach (Song song in songs)
                    guids[i++] = song.ID.ToString();
            }

            var build = new Dictionary<string, object>(3);
            build.Add("songIds", guids);
            build.Add("entryIds", new string[] {""});
            build.Add("listId", "all");
            build.Add("sessionId", this.SessionId);

            return JsonConvert.SerializeObject(build);
        }

        private async Task<Result<string>> DeleteSongs_Request(string json)
        {
            string response;
            try
            {
                using (var formContent = new FormUrlEncodedContent(new[] {new KeyValuePair<string, string>("json", json)}))
                {
                    response = await (await http.Client.PostAsync(AppendXt("https://play.google.com/music/services/deletesong"), formContent)).Content.ReadAsStringAsync();
                }
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }

            return new Result<string>(true, response, this);
        }

        private Result<IEnumerable<Guid>> DeleteSongs_ParseResponse(string response, int originalCount)
        {
            try
            {
                dynamic results = JsonConvert.DeserializeObject(response);
                Guid[] deleted = results.deletedIds.ToObject<Guid[]>();
                return new Result<IEnumerable<Guid>>(deleted.Length == originalCount, deleted, this);
            }
            catch (Exception e) { return new Result<IEnumerable<Guid>>(false, new Guid[0], this, e); }
        }

        #endregion

        #region CreatePlaylist

        
        #endregion

        #region GetUserPlaylists

        /// <summary>
        /// Asynchronously gets the user-created playlists from the Google Music library. Note that the playlist's contents (songs) are not retrieved.
        /// </summary>
        /// <param name="results">Optional. The collection to add the playlists to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.Dictionary"/>.</param>
        /// <param name="lockResults">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <returns></returns>
        public async Task<Result<ICollection<KeyValuePair<Guid, Playlist>>>> GetUserPlaylists(ICollection<KeyValuePair<Guid, Playlist>> results = null, object lockResults = null)
        {
            if (!this.IsLoggedIn)
                return new Result<ICollection<KeyValuePair<Guid, Playlist>>>(false, results, this, new ClientNotAuthorizedException(this));
            else if (results == null)
                results = new Dictionary<Guid, Playlist>();

            var requestResult = await GetUserPlaylists_Request();
            if (!requestResult.Success)
                return new Result<ICollection<KeyValuePair<Guid, Playlist>>>(requestResult.Success, results, this, requestResult.InnerException);

            var parseResult = await Task.Run(() => GetUserPlaylists_Parse(requestResult.Value, results, lockResults));

            return new Result<ICollection<KeyValuePair<Guid, Playlist>>>(parseResult.Success, results, this, parseResult.InnerException); ;
        }

        private async Task<Result<string>> GetUserPlaylists_Request()
        {
            KeyValuePair<string, string>[] form = new[] { new KeyValuePair<string, string>(String.Format(@"[[""{0}"",1],[]]", this.SessionId), String.Empty) };
            try
            {
                string response;
                using (FormUrlEncodedContent formContent = new FormUrlEncodedContent(form))
                {
                    response = await (await http.Client.PostAsync(AppendXt("https://play.google.com/music/services/loadplaylists"), formContent)).Content.ReadAsStringAsync();
                }

                return new Result<string>(true, response, this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }
        }

        private Result<bool> GetUserPlaylists_Parse(string json, ICollection<KeyValuePair<Guid, Playlist>> results, object lockResults)
        {
            try
            {
                dynamic deserialized = JsonConvert.DeserializeObject(json);
                Playlist[] playlists = deserialized["playlist"].ToObject<Playlist[]>();

                if (lockResults == null)
                    foreach (Playlist playlist in playlists) results.Add(new KeyValuePair<Guid, Playlist>(playlist.ID, playlist));
                else
                    lock (lockResults) { foreach (Playlist playlist in playlists) results.Add(new KeyValuePair<Guid, Playlist>(playlist.ID, playlist)); }
                
                return new Result<bool>(true, true, this);
            }
            catch (Exception e) { return new Result<bool>(false, false, this, e); }
        }

        #endregion

        #region GetPlaylistContent



        #endregion

        #region Parsing

        private static Result<bool> ParseSongs(int arrayIndex, string javascriptData, ICollection<KeyValuePair<Guid, Song>> results, object lockResults)
        {
            if (javascriptData == null)
                return new Result<bool>(false, false, null);

            JArray trackArray;
            try
            {
                dynamic jsonData = JsonConvert.DeserializeObject(javascriptData);
                //dynamic jsonData = JsonConvert.DeserializeObject("{stringArray:" + javascriptData + "}");
                //trackArray = (JArray)jsonData.stringArray[arrayIndex];

                trackArray = (JArray)jsonData[arrayIndex];

                while (trackArray[0] is JArray && trackArray[0][0] is JArray)
                    trackArray = (JArray) trackArray[0];

            }
            catch (Exception e) { return new Result<bool>(false, false, null, e); }

            foreach (var track in trackArray)
            {
                try
                {
                    Song song = Song.Build(track);

                    if (song != null)
                    {
                        if (lockResults == null)
                            results.Add(new KeyValuePair<Guid, Song>(song.ID, song));
                        else
                            lock (lockResults) { results.Add(new KeyValuePair<Guid, Song>(song.ID, song)); }
                    }
                }
                catch (Exception) { }
            }

            return new Result<bool>(true, true, null);
        }

        #endregion

    }
}
