using System;
using System.IO;
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
        private static readonly Regex AUTH_USER_ID_REGEX = new Regex(@"window\['USER_ID'\] = '(?<USERID>(.*?))'", RegexOptions.IgnoreCase);
        private static readonly Regex GET_ALL_SONGS_REGEX = new Regex(@"window\.parent\['slat_process'\]\((?<SONGS>.+?)\);\s+?window", RegexOptions.Singleline);
        private static readonly Random RANDOM = new Random();

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

        public bool IsLoggedIn
        {
            get { return !String.IsNullOrEmpty(this.UserId) && !String.IsNullOrEmpty(this.SessionId); }
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
        /// <returns>
        /// A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the outcome of the request.
        /// The value of <code>Result</code> is a string that contains additional error information, if the login failed.
        /// </returns>
        public async Task<Result<string>> Login(string email, string password, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Step 1: Get GALX cookie
            string galx;
            try
            {
                await http.Client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://accounts.google.com/ServiceLogin"), cancellationToken);
                galx = http.Settings.CookieContainer.GetCookie("https://accounts.google.com", "GALX");
            }
            catch (Exception e) { return new Result<string>(false, "Failed to start the authorization process. Google may have changed the protocol.", this, e); }

            if (galx == null)
                return new Result<string>(false, "Failed to start the authorization process. Google may have changed the protocol.", this);

            // Step 2: Get authorization token and authorization cookie (thanks to 'continue' in form data)
            var form = new[]
                    {

                        new KeyValuePair<string, string>("GALX", galx),
                        new KeyValuePair<string, string>("continue", "https://play.google.com/music/listen"),
                        new KeyValuePair<string, string>("_utf8", "☃"),
                        new KeyValuePair<string, string>("service", "sj"),
                        new KeyValuePair<string, string>("bgresponse", "js_disabled"),
                        new KeyValuePair<string, string>("pstMsg", "1"),
                        new KeyValuePair<string, string>("dnConn", String.Empty),
                        new KeyValuePair<string, string>("checkConnection", String.Format("youtube:{0}:1", RANDOM.Next(100, 1000))),
                        new KeyValuePair<string, string>("checkDomains", "youtube"),
                        new KeyValuePair<string, string>("Email", email),
                        new KeyValuePair<string, string>("Passwd", password),
                        new KeyValuePair<string, string>("signIn", "Sign in"),
                        new KeyValuePair<string, string>("PersistentCookie", "yes"),
                        new KeyValuePair<string, string>("rmShown", "1")
                    };
            try
            {
                string response;
                using (var formContent = new FormUrlEncodedContent(form))
                {
                    response = await (await http.Client.PostAsync("https://accounts.google.com/ServiceLoginAuth", formContent, cancellationToken)).Content.ReadAsStringAsync();
                }

                this.UserId = AUTH_USER_ID_REGEX.Match(response).Groups["USERID"].Value;

                if ((http.Settings.CookieContainer.GetCookie("https://play.google.com/music/listen", "xt")) == null)
                    return new Result<string>(false, "Unable to retrieve the proper authorization cookies from Google.", this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }


            if (cancellationToken.IsCancellationRequested)
                return new Result<string>(false, "Request cancelled.", this);


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
            this.UserId = null;
            this.SessionId = null;
        }

        #endregion

        #region GetSongCount

        /// <summary>
        /// Asynchronously retrieves the number of songs in the current Google Music library.
        /// </summary>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the number of songs.</returns>
        public async Task<Result<int>> GetSongCount()
        {
            if (!this.IsLoggedIn)
                return new Result<int>(false, -1, this, new ClientNotAuthorizedException(this));

            string response;
            try
            {
                using (var formContent = new FormUrlEncodedContent(new[] { new KeyValuePair<string, string>("json", String.Format("{{\"sessionId\":\"{0}\"}}", this.SessionId)) }))
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
        /// <param name="progress">Optional. The object to report progress to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the resulting collection of songs, if successful.</returns>
        public async Task<Result<IDictionary<Guid, Song>>> GetAllSongs(IDictionary<Guid, Song> results = null, object lockResults = null, IProgress<double> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!this.IsLoggedIn)
                return new Result<IDictionary<Guid, Song>>(false, results, this, new ClientNotAuthorizedException(this));
            
            // Step 1: Get response from Google
            var response = await GetAllSongs_Request(progress, cancellationToken);
            if (!response.Success)
                return new Result<IDictionary<Guid, Song>>(response.Success, results, response.Client, response.InnerException);

            // Step 2: Asynchronously parse result
            var parse = await Task.Run(() => GetAllSongs_Parse(response.Value, ref results, lockResults, progress), cancellationToken);

            return new Result<IDictionary<Guid, Song>>(parse.Success, results, this, parse.InnerException);
        }

        private async Task<Result<string>> GetAllSongs_Request(IProgress<double> progress, CancellationToken cancellationToken, double progressMin = 0.0, double progressMax = 80.0)
        {
            string url = AppendXt(
                String.Format(@"https://play.google.com/music/services/streamingloadalltracks?json={{""tier"":1,""requestCause"":1,""requestType"":1,""sessionId"":""{0}""}}&format=jsarray",
                this.SessionId));
            try
            {
                var responseContent = (await http.Client.GetAsync(url, cancellationToken)).Content;

                StringBuilder builder = await (await responseContent.ReadAsStreamAsync()).CopyToAsync(new StringBuilder(), responseContent.Headers.ContentLength, progress, progressMin, progressMax, cancellationToken);

                return new Result<string>(true, builder.ToString(), this);
            }
            catch (Exception e) { return new Result<string>(false, String.Empty, this, e); }
        }

        private Result GetAllSongs_Parse(string response, ref IDictionary<Guid, Song> results, object lockResults, IProgress<double> progress, double progressMin = 80.0, double progressMax = 100.0)
        {
            var matches = GET_ALL_SONGS_REGEX.Matches(response);
            
            if (results == null)
            {
                // Accurately assess how big our dictionary should be (each match is a max of 1000 songs)
                results = new Dictionary<Guid, Song>(matches.Count * 1000);
                // Note: Lock object only if results weren't null. Otherwise, it is an unnecessary overhead (since caller doesn't have access to results)
                lockResults = null;
            }

            Result parseResult = new Result(true, this);

            if (matches.Count > 5)
            {
                // If: Over 5k songs, parallelize
                var parsedArray = new List<Song>[matches.Count];

                Parallel.For(0, matches.Count, (index) =>
                    {
                        parsedArray[index] = new List<Song>(1000);

                        var tempResult = ParseSongs(0, matches[index].Groups["SONGS"].Value, (song) => parsedArray[index].Add(song), null);
                        if (!tempResult.Success)
                            parseResult = tempResult;
                    });

                // Exit if there was a failure
                if (!parseResult.Success)
                    return parseResult;

                if (progress != null)
                {
                    double report = (progressMax - progressMin) * 0.5 + progressMin;
                    progress.Report(report);
                    // Update progressMin to make calculations easier when copying to dictionary
                    progressMin = report;
                }

                // Add everything to the dictionary
                bool lockWasTaken = false;
                try
                {
                    if (lockResults != null)
                        Monitor.Enter(lockResults, ref lockWasTaken);

                    for (int i = 0; i < parsedArray.Length; ++i)
                    {
                        foreach (var element in parsedArray[i])
                            results.Add(element.ID, element);

                        if (progress != null)
                            progress.Report((progressMax - progressMin) * ((double)(i + 1) / (double)parsedArray.Length) + progressMin);
                    }
                }
                finally { if (lockWasTaken) Monitor.Exit(lockResults); }
            }
            else
            {
                var derefResults = results;
                // Else: 5k songs or under, probably not worth the overhead of parallelization
                for (int i = 0; i < matches.Count; ++i)
                {
                    // GetAllSongs is located in 0th index
                    parseResult = ParseSongs(0, matches[i].Groups["SONGS"].Value, (song) => derefResults.Add(song.ID, song), lockResults);

                    if (progress != null)
                        progress.Report((progressMax - progressMin) * ((double)(i + 1) / (double)matches.Count) + progressMin);

                    if (!parseResult.Success)
                        return parseResult;
                }
            }

            return parseResult;
        }

        #endregion

        #region GetDeletedSongs

        /// <summary>
        /// Asynchronously retrieves the deleted songs from the Google Music library.
        /// </summary>
        /// <param name="results">Optional. The collection to add the songs to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.Dictionary"/>.</param>
        /// <param name="lockResults">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <param name="progress">Optional. The object to report progress to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns></returns>
        public async Task<Result<IDictionary<Guid, Song>>> GetDeletedSongs(IDictionary<Guid, Song> results = null, object lockResults = null, IProgress<double> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!this.IsLoggedIn)
                return new Result<IDictionary<Guid, Song>>(false, results, this, new ClientNotAuthorizedException(this));
            else if (results == null)
            {
                results = new Dictionary<Guid, Song>();
                // Note: Lock object only if results weren't null. Otherwise, it is unnecessary overhead (since they don't have access to results)
                lockResults = null;
            }

            // Step 1: Request deleted songs (auto-playlist) JSArray
            var requestResult = await GetDeletedSongs_Request(progress, cancellationToken);
            if (!requestResult.Success)
                return new Result<IDictionary<Guid, Song>>(requestResult.Success, results, this, requestResult.InnerException);

            // Step 2: Parse streamed JSArray
            Result parseResult;
            using (requestResult.Value)
                parseResult = await Task.Run(() => ParseSongs(1, requestResult.Value, (song) => results.Add(song.ID, song), lockResults, progress, 25.0, 100.0), cancellationToken);

            return new Result<IDictionary<Guid, Song>>(parseResult.Success, results, this, parseResult.InnerException);
        }

        private async Task<Result<Stream>> GetDeletedSongs_Request(IProgress<double> progress, CancellationToken cancellationToken, double progressMin = 0.0, double progressMax = 25.0)
        {
            string url = AppendXt("https://play.google.com//music/services/loadautoplaylist?format=jsarray");

            try
            {
                Stream responseStream;
                using (var stringContent = new StringContent(String.Format(@"[[""{0}"",1,""{1}""],[""auto-playlist-trash""]]", this.SessionId, this.UserId)))
                {
                    responseStream = await (await http.Client.PostAsync(url, stringContent, cancellationToken)).Content.ReadAsStreamAsync();
                }

                if (progress != null)
                    progress.Report(progressMax);

                return new Result<Stream>(true, responseStream, this);
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }
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
            
            // Step 1: Send request (request will build JSON on the fly)
            var requestResult = await DeleteSongs_Request(songs);
            if (!requestResult.Success)
                return new Result<IEnumerable<Guid>>(requestResult.Success, null, this, requestResult.InnerException);
            
            // Step 2: Parse response of successfully deleted songs
            return await Task.Run(() => ParseDeleteResponse(requestResult.Value, songs.Count()));
        }

        private void DeleteSongs_BuildJson(IEnumerable<Song> songs, Stream stream)
        {
            //string[] guids = new string[songs.Count()];
            //{
            //    int i = 0;
            //    foreach (Song song in songs)
            //        guids[i++] = song.ID.ToString();
            //}
            
            //var build = new Dictionary<string, object>(3);
            //build.Add("songIds", guids);
            //build.Add("entryIds", new string[] {""});
            //build.Add("listId", "all");
            //build.Add("sessionId", this.SessionId);

            //return JsonConvert.SerializeObject(build);
            
            using (var streamWriter = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                streamWriter.Write("json=");

                jsonWriter.WriteStartObject(); // {
                
                // songIds : ["Guid-here"]
                jsonWriter.WritePropertyName("songIds");
                jsonWriter.WriteStartArray();
                foreach (var song in songs) jsonWriter.WriteValue(song.ID);
                jsonWriter.WriteEndArray();

                // "entryIds" : [""]
                jsonWriter.WritePropertyName("entryIds");
                jsonWriter.WriteStartArray();
                jsonWriter.WriteValue(String.Empty);
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("listId");
                jsonWriter.WriteValue("all");

                jsonWriter.WritePropertyName("sessionId");
                jsonWriter.WriteValue(this.SessionId);

                jsonWriter.WriteEndObject();
            }
        }

        private async Task<Result<Stream>> DeleteSongs_Request(IEnumerable<Song> songs)
        {
            string url = AppendXt("https://play.google.com/music/services/deletesong");
            Stream response;
            try
            {
                using (var streamContent = new PushStreamContent(async (stream, httpContent, transportContext) => await Task.Run(() => DeleteSongs_BuildJson(songs, stream)), "application/x-www-form-urlencoded"))
                {
                    response = await (await http.Client.PostAsync(url, streamContent)).Content.ReadAsStreamAsync();
                }
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }

            return new Result<Stream>(true, response, this);
        }

        #endregion

        #region CreatePlaylist

        /// <summary>
        /// Asynchronously creates a playlist with the given specifics.
        /// </summary>
        /// <param name="title">Required. The name of the playlist.</param>
        /// <param name="description">Optional. A description of the contents of the playlist.</param>
        /// <param name="songs">Optional. The songs to initially add to the playlist.</param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the new playlist, if successful.</returns>
        public async Task<Result<Playlist>> CreatePlaylist(string title, string description = null, IEnumerable<Song> songs = null)
        {
            if (!this.IsLoggedIn)
                return new Result<Playlist>(false, null, this, new ClientNotAuthorizedException(this));
            else if (String.IsNullOrEmpty(title))
                return new Result<Playlist>(false, null, this, new ArgumentException("Argument cannot be null or empty.", "name"));

            // Step 1: Send request to server
            var requestResult = await CreatePlaylist_Request(title, description, songs);
            if (!requestResult.Success)
                return new Result<Playlist>(requestResult.Success, null, this, requestResult.InnerException);

            // Step 2: Parse result from server and create playlist
            using (requestResult.Value)
                return await Task.Run(() => CreatePlaylist_ParseResponse(title, description, requestResult.Value, songs));
        }

        private void CreatePlaylist_BuildJson(string title, string description, IEnumerable<Song> songs, Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.WriteStartArray(); // [

                // ["sessionId",1]
                jsonWriter.WriteStartArray();
                jsonWriter.WriteValue(this.SessionId);
                jsonWriter.WriteValue(1);
                jsonWriter.WriteEndArray();

                // [null, "Name", "Description",
                jsonWriter.WriteStartArray();
                jsonWriter.WriteNull();
                jsonWriter.WriteValue(title);
                jsonWriter.WriteValue(description);

                // [["Song-Guid", (int)songType], [...]]
                jsonWriter.WriteStartArray();
                if (songs != null)
                {
                    foreach (Song song in songs)
                    {
                        jsonWriter.WriteStartArray();
                        jsonWriter.WriteValue(song.ID);
                        jsonWriter.WriteValue(song.Type);
                        jsonWriter.WriteEndArray();
                    }
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndArray(); //]
                jsonWriter.WriteEndArray(); //]
            }
        }

        private async Task<Result<Stream>> CreatePlaylist_Request(string title, string description, IEnumerable<Song> songs)
        {
            string url = AppendXt("https://play.google.com/music/services/createplaylist?format=jsarray");
            Stream response;
            try
            {
                using (var streamContent = new PushStreamContent(async (stream, httpContent, transportContext) => await Task.Run(() => CreatePlaylist_BuildJson(title, description, songs, stream)), "application/x-www-form-urlencoded"))
                {
                    response = await (await http.Client.PostAsync(url, streamContent)).Content.ReadAsStreamAsync();
                }
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }

            return new Result<Stream>(true, response, this);
        }

        private Result<Playlist> CreatePlaylist_ParseResponse(string title, string description, Stream response, IEnumerable<Song> songs)
        {
            try
            {
                JArray deserialized;
                using (StreamReader streamReader = new StreamReader(response))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var jsonSerializer = new JsonSerializer();
                    deserialized = jsonSerializer.Deserialize<JArray>(jsonTextReader);
                }

                Playlist playlist = new Playlist();

                playlist.Title = title;
                playlist.Description = (description == null ? String.Empty : description);

                playlist.ID = deserialized[1][0].ToObject<Guid>();

                playlist.SharedToken = deserialized[1][1].ToObject<string>();
                int songCount = songs == null ? -1 : songs.Count();
                if (songCount > 0)
                {
                    // Initialize new list for songs
                    playlist.Songs = new List<Song>(songCount);

                    // Record all GUIDs in dictionary (hashtable) for fast lookup
                    var playlistIDs = new Dictionary<Guid, Guid>(songCount);
                    {
                        object[][] tracks = deserialized[1][2].ToObject<object[][]>();
                        foreach (var track in tracks)
                            playlistIDs.Add(new Guid(track[0].ToString()), new Guid(track[2].ToString()));
                    }

                    // Go over each song, add playlist ID, and then add it to the playlist
                    foreach (var song in songs)
                    {
                        Guid playlistId;
                        if (playlistIDs.TryGetValue(song.ID, out playlistId))
                        {
                            song.PlaylistEntryId = playlistId.ToString();
                            playlist.Songs.Add(song);
                        }
                    }
                }
                else
                {
                    playlist.Songs = new List<Song>();
                }

                // Timestamps
                {
                    long timestamp = deserialized[1][3].ToObject<long>();
                    playlist.CreationTimestampMicroseconds = timestamp;
                    playlist.LastModifiedTimestampMicroseconds = timestamp;
                    playlist.RecentTimestampMicrseconds = timestamp;
                }

                // Unlisted property
                playlist.Type = 1;

                return new Result<Playlist>(true, playlist, this);
            }
            catch (Exception e) { return new Result<Playlist>(false, null, this, e); }
        }

        #endregion

        #region DeletePlaylist

        /// <summary>
        /// Asynchronously deletes a playlist.
        /// </summary>
        /// <param name="title">Required. The playlist to delete.</param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the deleted playlist ID, if successful.</returns>
        public async Task<Result<Guid>> DeletePlaylist(Playlist playlist)
        {
            if (!this.IsLoggedIn)
                return new Result<Guid>(false, new Guid(), this, new ClientNotAuthorizedException(this));

            // Step 1: Send request to server
            var requestResult = await DeletePlaylist_Request(playlist);
            if (!requestResult.Success)
                return new Result<Guid>(requestResult.Success, new Guid(), this, requestResult.InnerException);

            // Step 2: Parse result from server and create playlist
            return await Task.Run(() => DeletePlaylist_ParseResponse(requestResult.Value));
        }

        private string DeletePlaylist_BuildJson(Playlist playlist)
        {
            var build = new Dictionary<string, object>(4);
            build.Add("id", playlist.ID);
            build.Add("requestCause", 1);
            build.Add("requestType", 1);
            build.Add("sessionId", this.SessionId);

            return "json="+ JsonConvert.SerializeObject(build);
        }

        private async Task<Result<String>> DeletePlaylist_Request(Playlist playlist)
        {
            string url = AppendXt("https://play.google.com/music/services/deleteplaylist");
            String response;
            try
            {
                using (var stringContent = new StringContent(DeletePlaylist_BuildJson(playlist), Encoding.UTF8, "application/x-www-form-urlencoded"))
                {
                    response = await (await http.Client.PostAsync(url, stringContent)).Content.ReadAsStringAsync();
                }
            }
            catch (Exception e) { return new Result<String>(false, null, this, e); }

            return new Result<String>(true, response, this);
        }

        private Result<Guid> DeletePlaylist_ParseResponse(String response)
        {
            try
            {
                Dictionary<string, string> deserialized = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);

                return new Result<Guid>(true, Guid.Parse(deserialized["deleteId"]), this);
            }
            catch (Exception e) { return new Result<Guid>(false, new Guid(), this, e); }
        }

        #endregion

        #region GetUserPlaylists

        /// <summary>
        /// Asynchronously gets the user-created playlists from the Google Music library. Note that the playlist's contents (songs) are not retrieved.
        /// </summary>
        /// <param name="results">Optional. The collection to add the playlists to. The recommended data structure in nearly all cases is <see cref="System.Collections.Generic.Dictionary"/>.</param>
        /// <param name="lockResults">Optional. The object to lock when making writes to <paramref name="results"/>. This is useful when <paramref name="results"/> is not thread-safe.</param>
        /// <param name="progress">Optional. The object to report progress to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the outcome of the request.</returns>
        public async Task<Result<IDictionary<Guid, Playlist>>> GetUserPlaylists(IDictionary<Guid, Playlist> results = null, object lockResults = null, IProgress<double> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!this.IsLoggedIn)
                return new Result<IDictionary<Guid, Playlist>>(false, results, this, new ClientNotAuthorizedException(this));
            else if (results == null)
                results = new Dictionary<Guid, Playlist>();

            // Step 1: Request servers for user playlists JSON
            var requestResult = await GetUserPlaylists_Request(progress, cancellationToken);
            if (!requestResult.Success)
                return new Result<IDictionary<Guid, Playlist>>(requestResult.Success, results, this, requestResult.InnerException);

            // Step 2: Parse streamed JSON
            Result parseResult;
            using (requestResult.Value)
                parseResult = await Task.Run(() => GetUserPlaylists_Parse(requestResult.Value, results, lockResults, progress), cancellationToken);

            return new Result<IDictionary<Guid, Playlist>>(parseResult.Success, results, this, parseResult.InnerException);
        }

        private async Task<Result<Stream>> GetUserPlaylists_Request(IProgress<double> progress, CancellationToken cancellationToken, double progressMin = 0.0, double progressMax = 25.0)
        {
            string url = AppendXt("https://play.google.com/music/services/loadplaylists");
            try
            {
                Stream responseStream;
                using (StringContent stringContent = new StringContent(String.Format(@"[[""{0}"",1],[]]", this.SessionId)))
                {
                    responseStream = await (await http.Client.PostAsync(url, stringContent, cancellationToken)).Content.ReadAsStreamAsync();
                }

                return new Result<Stream>(true, responseStream, this);
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }
        }

        private Result GetUserPlaylists_Parse(Stream jsonStream, IDictionary<Guid, Playlist> results, object lockResults, IProgress<double> progress, double progressMin = 25.0, double progressMax = 100.0)
        {
            try
            {
                dynamic deserialized;
                using (StreamReader streamReader = new StreamReader(jsonStream))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var jsonSerializer = new JsonSerializer();
                    deserialized = jsonSerializer.Deserialize(jsonTextReader);
                }

                Playlist[] playlists = deserialized["playlist"].ToObject<Playlist[]>();

                bool lockWasTaken = false;
                try
                {
                    if (lockResults != null)
                        Monitor.Enter(lockResults, ref lockWasTaken);

                    for (int i = 0; i < playlists.Length; ++i)
                    {
                        results.Add(playlists[i].ID, playlists[i]);
                        if (progress != null)
                            progress.Report((progressMax - progressMin) * ((double)(i + 1) / (double)playlists.Length) + progressMin);
                    }
                }
                finally { if (lockWasTaken) Monitor.Exit(lockResults); }

                return new Result(true, true, this);
            }
            catch (Exception e) { return new Result(false, false, this, e); }
        }

        #endregion

        #region GetPlaylistContent

        /// <summary>
        /// Asynchronously gets the contents (songs) of the specified playlist.
        /// </summary>
        /// <param name="playlist">Required. The playlist to retrieve songs for.</param>
        /// <param name="progress">Optional. The object to report progress to.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is <see cref="System.Threading.CancellationToken.None"/></param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with the list of songs (which are also set in the playlist).</returns>
        public async Task<Result<List<Song>>> GetPlaylistSongs(Playlist playlist, IProgress<double> progress = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!this.IsLoggedIn)
                return new Result<List<Song>>(false, null, this, new ClientNotAuthorizedException(this));
            else if (playlist == null)
                throw new ArgumentNullException("playlist");

            // Step 1: Get data from server
            var requestResult = await GetPlaylistSongs_Request(playlist.ID, progress, cancellationToken);
            if (!requestResult.Success)
                return new Result<List<Song>>(requestResult.Success, null, this, requestResult.InnerException);

            // Step 2: Initialize playlist songs if needed
            if (playlist.Songs == null)
                playlist.Songs = new List<Song>();

            // Step 3: Parse results from data
            Result parseResult;
            using (requestResult.Value)
                parseResult = await Task.Run(() => ParseSongs(1, requestResult.Value, (song) => playlist.Songs.Add(song), null, progress, 80.0, 100.0), cancellationToken);


            return new Result<List<Song>>(parseResult.Success, playlist.Songs, this, parseResult.InnerException);
        }

        private async Task<Result<Stream>> GetPlaylistSongs_Request(Guid guid, IProgress<double> progress, CancellationToken cancellationToken, double progressMin = 0.0, double progressMax = 80.0)
        {
            string url = AppendXt("https://play.google.com/music/services/loaduserplaylist?format=jsarray");
            try
            {
                Stream responseStream;
                using (StringContent stringContent = new StringContent(String.Format(@"[[""{0}"",1],[""{1}""]]", this.SessionId, guid.ToString())))
                {
                    responseStream = await (await http.Client.PostAsync(url, stringContent)).Content.ReadAsStreamAsync();
                }

                return new Result<Stream>(true, responseStream, this);
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }
        }

        #endregion

        #region AddToPlaylist

        /// <summary>
        /// Asynchronously adds songs to a Google Music playlist.
        /// </summary>
        /// <param name="title">Required. The name of the playlist.</param>
        /// <param name="description">Optional. A description of the contents of the playlist.</param>
        /// <returns>A task that represents the asynchronous request. The value of the <code>TResult</code> parameter contains a <code>Result</code> object with a dictionary mapping song IDs to playlist entry IDs.</returns>
        public async Task<Result<IDictionary<Guid, Guid>>> AddToPlaylist(Playlist playlist, IEnumerable<Song> songs = null)
        {
            if (!this.IsLoggedIn)
                return new Result<IDictionary<Guid, Guid>>(false, null, this, new ClientNotAuthorizedException(this));

            // Step 1: Send request to server
            var requestResult = await AddToPlaylist_Request(playlist, songs);
            if (!requestResult.Success)
                return new Result<IDictionary<Guid, Guid>>(requestResult.Success, null, this, requestResult.InnerException);

            // Step 2: Parse result from server and create playlist
            using (requestResult.Value)
                return await Task.Run(() => AddToPlaylist_ParseResponse(requestResult.Value));
        }

        private void AddToPlaylist_BuildJson(Playlist playlist, IEnumerable<Song> songs, Stream stream)
        {
            using (var streamWriter = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                jsonWriter.WriteStartArray(); // [

                // ["sessionId",1]
                jsonWriter.WriteStartArray();
                jsonWriter.WriteValue(this.SessionId);
                jsonWriter.WriteValue(1);
                jsonWriter.WriteEndArray();

                // ["Playlist-Guid",
                jsonWriter.WriteStartArray();
                jsonWriter.WriteValue(playlist.ID);

                // [["Song-Guid", (int)songType], [...]]
                jsonWriter.WriteStartArray();
                if (songs != null)
                {
                    foreach (Song song in songs)
                    {
                        jsonWriter.WriteStartArray();
                        jsonWriter.WriteValue(song.ID);
                        jsonWriter.WriteValue(song.Type);
                        jsonWriter.WriteEndArray();
                    }
                }
                jsonWriter.WriteEndArray();

                jsonWriter.WriteEndArray(); //]
                jsonWriter.WriteEndArray(); //]
            }
        }

        private async Task<Result<Stream>> AddToPlaylist_Request(Playlist playlist, IEnumerable<Song> songs)
        {
            string url = AppendXt("https://play.google.com/music/services/addtrackstoplaylist?format=jsarray");
            Stream response;
            try
            {
                using (var streamContent = new PushStreamContent(async (stream, httpContent, transportContext) => await Task.Run(() => AddToPlaylist_BuildJson(playlist, songs, stream)), "application/x-www-form-urlencoded"))
                {
                    response = await (await http.Client.PostAsync(url, streamContent)).Content.ReadAsStreamAsync();
                }
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }

            return new Result<Stream>(true, response, this);
        }

        private Result<IDictionary<Guid, Guid>> AddToPlaylist_ParseResponse(Stream response)
        {
            try
            {
                JArray deserialized;
                using (StreamReader streamReader = new StreamReader(response))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var jsonSerializer = new JsonSerializer();
                    deserialized = jsonSerializer.Deserialize<JArray>(jsonTextReader);
                }

                deserialized = (JArray)deserialized[1][1];

                Dictionary<Guid, Guid> dict = new Dictionary<Guid, Guid>(deserialized.Count);

                foreach (JToken entry in deserialized)
                {
                    dict.Add(entry[0].ToObject<Guid>(), entry[2].ToObject<Guid>());
                }

                return new Result<IDictionary<Guid, Guid>>(true, dict, this);
            }
            catch (Exception e) { return new Result<IDictionary<Guid, Guid>>(false, null, this, e); }
        }

        #endregion

        #region DeleteFromPlaylist

        /// <summary>
        /// Asynchronously deletes songs from a Google Music playlist.
        /// </summary>
        /// <param name="playlist">Required. The playlist to delete songs from.</param>
        /// <param name="songs">Required. The enumerable collection of songs to delete. Note that every <code>Song</code> must have its playlist entry ID defined.</param>
        /// <returns>Returns a <see cref="Result"/> containing an enumerable collection of GUIDs that were successfully deleted.</returns>
        public async Task<Result<IEnumerable<Guid>>> DeleteFromPlaylist(Playlist playlist, IEnumerable<Song> songs)
        {
            if (!this.IsLoggedIn)
                return new Result<IEnumerable<Guid>>(false, new Guid[0], this, new ClientNotAuthorizedException(this));
            if (songs == null || songs.Count() < 1)
                return new Result<IEnumerable<Guid>>(true, new Guid[0], this);

            // Step 1: Send request (request will build JSON on the fly)
            var requestResult = await DeleteFromPlaylist_Request(songs, playlist);
            if (!requestResult.Success)
                return new Result<IEnumerable<Guid>>(requestResult.Success, null, this, requestResult.InnerException);

            // Step 2: Parse response of successfully deleted songs
            return await Task.Run(() => ParseDeleteResponse(requestResult.Value, songs.Count()));
        }

        private void DeleteFromPlaylist_BuildJson(IEnumerable<Song> songs, Stream stream, Playlist playlist)
        {
            using (var streamWriter = new StreamWriter(stream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                streamWriter.Write("json=");

                jsonWriter.WriteStartObject(); // {

                // songIds : ["Guid-here"]
                jsonWriter.WritePropertyName("songIds");
                jsonWriter.WriteStartArray();
                foreach (var song in songs) jsonWriter.WriteValue(song.ID);
                jsonWriter.WriteEndArray();

                // "entryIds" : [""]
                jsonWriter.WritePropertyName("entryIds");
                jsonWriter.WriteStartArray();
                foreach (var song in songs) jsonWriter.WriteValue(song.PlaylistEntryId);
                jsonWriter.WriteEndArray();

                jsonWriter.WritePropertyName("listId");
                jsonWriter.WriteValue(playlist.ID.ToString());

                jsonWriter.WritePropertyName("sessionId");
                jsonWriter.WriteValue(this.SessionId);

                jsonWriter.WriteEndObject();
            }
        }

        private async Task<Result<Stream>> DeleteFromPlaylist_Request(IEnumerable<Song> songs, Playlist playlist)
        {
            string url = AppendXt("https://play.google.com/music/services/deletesong");
            Stream response;
            try
            {
                using (var streamContent = new PushStreamContent(async (stream, httpContent, transportContext) => await Task.Run(() => DeleteFromPlaylist_BuildJson(songs, stream, playlist)), "application/x-www-form-urlencoded"))
                {
                    response = await (await http.Client.PostAsync(url, streamContent)).Content.ReadAsStreamAsync();
                }
            }
            catch (Exception e) { return new Result<Stream>(false, null, this, e); }

            return new Result<Stream>(true, response, this);
        }

        #endregion

        #region Parsing

        private static Result ParseSongs(int arrayIndex, string javascriptData, Action<Song> addResult, object lockResults, IProgress<double> progress = null, double progressMin = 0.0, double progressMax = 100.0)
        {
            return ParseSongs(arrayIndex, () => { return JsonConvert.DeserializeObject(javascriptData); }, addResult, lockResults, progress, progressMin, progressMax);
        }

        private static Result ParseSongs(int arrayIndex, Stream javascriptStream, Action<Song> addResult, object lockResults, IProgress<double> progress = null, double progressMin = 0.0, double progressMax = 100.0)
        {
            return ParseSongs(arrayIndex, () =>
                {
                    using (StreamReader streamReader = new StreamReader(javascriptStream))
                    using (var jsonTextReader = new JsonTextReader(streamReader))
                    {
                        var jsonSerializer = new JsonSerializer();
                        return jsonSerializer.Deserialize(jsonTextReader);
                    }
                }, 
                    addResult, lockResults, progress, progressMin, progressMax);
        }

        private static Result ParseSongs(int arrayIndex, Func<dynamic> dynamicFunc, Action<Song> addResult, object lockResults, IProgress<double> progress, double progressMin, double progressMax)
        {
            JArray trackArray;
            try
            {
                dynamic jsonData = dynamicFunc.Invoke();

                //dynamic jsonData = JsonConvert.DeserializeObject("{stringArray:" + javascriptData + "}");
                //trackArray = (JArray)jsonData.stringArray[arrayIndex];

                trackArray = (JArray)jsonData[arrayIndex];

                while (trackArray[0] is JArray && trackArray[0][0] is JArray)
                    trackArray = (JArray)trackArray[0];

            }
            catch (Exception e) { return new Result(false, false, null, e); }

            if (progress != null)
            {
                double report = (progressMax - progressMin) * 0.5 + progressMin;
                progress.Report(report);
                progressMin = report;
            }

            bool lockWasTaken = false;
            try
            {
                if (lockResults != null)
                    Monitor.Enter(lockResults);

                for (int i = 0; i < trackArray.Count; ++i)
                {
                    try
                    {
                        Song song = Song.Build(trackArray[i]);

                        if (song != null)
                            addResult.Invoke(song);
                    }
                    catch { }

                    if (progress != null)
                        progress.Report((progressMax - progressMin) * ((double)(i + 1) / (double)trackArray.Count) + progressMin);
                }
            }
            finally { if (lockWasTaken) Monitor.Exit(lockResults); }

            return new Result(true, true, null);
        }

        private Result<IEnumerable<Guid>> ParseDeleteResponse(Stream response, int originalCount)
        {
            try
            {
                dynamic deserialized;
                using (StreamReader streamReader = new StreamReader(response))
                using (var jsonTextReader = new JsonTextReader(streamReader))
                {
                    var jsonSerializer = new JsonSerializer();
                    deserialized = jsonSerializer.Deserialize(jsonTextReader);
                }

                string[] deleted_string = deserialized["deleteIds"].ToObject<string[]>();
                Guid[] deleted = new Guid[deleted_string.Length];
                for (int i = 0; i < deleted_string.Length; i++)
                {
                    int index = deleted_string[i].IndexOf('_');

                    deleted[i] = Guid.Parse(index > -1 ? deleted_string[i].Substring(0, index) : deleted_string[i]);
                }
                    

                
                return new Result<IEnumerable<Guid>>(deleted.Length == originalCount, deleted, this);
            }
            catch (Exception e) { return new Result<IEnumerable<Guid>>(false, new Guid[0], this, e); }
        }

        #endregion

    }
}
