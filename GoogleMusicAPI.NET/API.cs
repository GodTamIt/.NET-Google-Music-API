using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using wireless_android_skyjam;
using ProtoBuf;
using System.IO;

namespace GoogleMusicAPI
{
    public class API
    {
        #region Events
        public delegate void _GetAllSongsComplete();
        public _GetAllSongsComplete OnGetAllSongsComplete;

        public delegate void _GetChunkOfSongsComplete();
        public _GetChunkOfSongsComplete GetChunkOfSongsComplete;

        public delegate void _GetAllPlaylistsComplete();
        public _GetAllPlaylistsComplete OnGetAllPlaylistsComplete;

        public delegate void _GetAllPlaylistSongsComplete();
        public _GetAllPlaylistSongsComplete OnGetAllPlaylistSongsComplete;

        public delegate void _Error(Exception e);
        public _Error OnError;
        #endregion

        #region Members
        public static string Version;
        private static readonly Random RANDOM = new Random();

        public String DeviceId;
        public String DeviceFriendlyName;

        private GoogleHTTP client;
        private OAuth2HTTP oAuthClient;
        public SortedDictionary<String, GoogleMusicSong> trackContainer;
        public SortedDictionary<String, GoogleMusicPlaylist> playlistContainer;

        private string ClientId;
        private string ClientSecret;
        private string RefreshToken;
        private string AndroidUrl = "https://android.clients.google.com/upsj/";
        private string sjUrl = "https://www.googleapis.com/sj/v1.1/";
        private string UserAgent = "Music Manager (1, 0, 55, 7425 HTTPS - Windows)";
        private string ProtoBuffersContentType = "application/x-google-protobuf";
        #endregion

        #region Constructor
        public API()
        {
            string versionMajor = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Major.ToString();  // .net version
            string versionMinor = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor.ToString(); // plugin version
            string revision = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Build.ToString(); // number of days since 2000-01-01 at build time

            Version = versionMajor + "." + versionMinor + "." + revision;


            client = new GoogleHTTP();
            oAuthClient = new OAuth2HTTP();
            trackContainer = new SortedDictionary<String, GoogleMusicSong>();
            playlistContainer = new SortedDictionary<String, GoogleMusicPlaylist>();
        }
        #endregion

        #region Login
        public void RequestAuthCode(String clientId)
        {
            ClientId = clientId;

            string encodedClient_id = System.Web.HttpUtility.UrlEncode(clientId, Encoding.GetEncoding("ISO-8859-1"));
            string encodedScope = System.Web.HttpUtility.UrlEncode("https://www.googleapis.com/auth/musicmanager", Encoding.GetEncoding("ISO-8859-1"));

            string requestAuthCodeUrl = "https://accounts.google.com/o/oauth2/auth?";
            requestAuthCodeUrl += "response_type=code&";
            requestAuthCodeUrl += "client_id=" + encodedClient_id + "&";
            requestAuthCodeUrl += "redirect_uri=urn:ietf:wg:oauth:2.0:oob&";
            requestAuthCodeUrl += "scope=" + encodedScope + "";

            System.Diagnostics.Process.Start(requestAuthCodeUrl);
        }

        public String RequestRefreshToken(String authCode, String clientId, String clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;

            Dictionary<String, String> fieldsOA = new Dictionary<String, String>
            {
                {"code", authCode},
                {"client_id",  ClientId},
                {"client_secret", ClientSecret},
                {"redirect_uri", "urn:ietf:wg:oauth:2.0:oob"},
                {"grant_type", "authorization_code"},
            };

            FormBuilder builderOA = new FormBuilder();
            builderOA.AddFields(fieldsOA);
            builderOA.Close();

            string jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes(), UserAgent));

            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonData);

            return json["refresh_token"];
        }

        public void RefreshAccessToken()
        {
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

            string jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri("https://accounts.google.com/o/oauth2/token"), builderOA.ContentType, builderOA.GetBytes()));

            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonData);

            OAuth2HTTP.AccessToken = json["access_token"];
        }

        public string GetGALX()
        {
            var cookies = client.DownloadCookiesSync(new Uri("https://accounts.google.com/ServiceLogin"));

            GoogleHTTP.AuthorizationCookieCont = cookies.Item1;
            GoogleHTTP.AuthorizationCookies = cookies.Item2;

            return GoogleHTTP.GetCookieValue("GALX", cookies.Item2);
        }

        public void Login(String email, String password, String refreshToken, String clientId, String clientSecret)
        {
            ClientId = clientId;
            ClientSecret = clientSecret;
            RefreshToken = refreshToken;

            RefreshAccessToken();


            string galx = GetGALX();            
            Dictionary<String, String> fields = new Dictionary<String, String>
            {
                {"GALX", galx},
                {"continue", "https://play.google.com/music/listen"},
                {"_utf8", "☃"},
                {"service", "sj"},
                {"bgresponse", "js_disabled"},
                {"pstMsg", "1"},
                {"dnConn", String.Empty},
                {"checkConnection", String.Format("youtube:{0}:1", RANDOM.Next(100, 1000))},
                {"Email",  email},
                {"Passwd", password},
                {"signIn", "Sign in"},
                {"PersistentCookie", "yes"},
                {"rmShown", "1"}
            };

            FormBuilder builder = new FormBuilder();
            builder.AddFields(fields);
            builder.Close();

            GetAuthCookies(builder);
        }

        private void GetAuthCookies(FormBuilder builder)
        {
            HttpWebRequest request;
            HttpWebResponse response;

            client.UploadDataSync(new Uri("https://accounts.google.com/ServiceLoginAuth"), builder.ContentType, builder.GetBytes(), out request, out response);

            GoogleHTTP.AuthorizationCookies = response.Cookies;
            response.Close();
        }
        #endregion

        #region GetAllSongs
        /// <summary>
        /// Gets all the songs
        /// </summary>
        /// <param name="continuationToken"></param>
        public void GetAllSongs()
        {
            trackContainer = new SortedDictionary<String, GoogleMusicSong>();

            GetAllSongsRequest();
        }

        private void GetAllSongsRequest(String nextPageToken = "")
        {
            String dataString = "";
            if (nextPageToken != "")
                dataString = "{'start-token':'" + nextPageToken + "'}";

            byte[] data = Encoding.UTF8.GetBytes(dataString);

            client.UploadDataAsync(new Uri(sjUrl + "trackfeed?alt=json&updated-min=0&include-tracks=true"), "application/json", data, 10000, SongsReceived, null);
        }

        private void SongsReceived(HttpWebRequest request, HttpWebResponse response, String jsonData, Exception error)
        {
            if (error != null)
            {
                OnError(error);
                return;
            }

            GetAllSongsResp chunk = Newtonsoft.Json.JsonConvert.DeserializeObject<GetAllSongsResp>(jsonData);

            foreach (GoogleMusicSong song in chunk.Data.Songs)
                if (!song.Deleted)
                    trackContainer.Add(song.ID, song);

            
            // Check for a nextPageToken, and if there is one go round the loop again
            JObject songJson = JObject.Parse(jsonData);
            string nextPageToken = "";
            try { nextPageToken = songJson["nextPageToken"].ToString(); }
            catch { }

            if (!String.IsNullOrEmpty(nextPageToken))
            {
                if (GetChunkOfSongsComplete != null)
                    GetChunkOfSongsComplete();

                GetAllSongsRequest(nextPageToken);
            }
            else
            {
                if (OnGetAllSongsComplete != null)
                    OnGetAllSongsComplete();
            }
        }
        #endregion

        #region CreatePlaylist
        /// <summary>
        /// Creates a playlist with given name
        /// </summary>
        /// <param name="playlistName">Name of playlist</param>
        public MutateResponse CreatePlaylist(String playlistName)
        {
            JObject requestData = new JObject
            {{"mutations", new JArray
            {new JObject 
                {{"create", new JObject 
                    {
                    {"creationTimestamp", -1},
                    {"deleted", false},
                    {"lastModifiedTimestamp",0},
                    {"name", playlistName},
                    {"type", "USER_GENERATED"}
                    }
                }}
            }
            }};

            byte[] data = Encoding.UTF8.GetBytes(requestData.ToString());

            string jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri(sjUrl + "playlistbatch?alt=json"), "application/json", data));

            MutateResponse resp = null;

            try
            {
                MutatePlaylistResponse mutateResponse = JsonConvert.DeserializeObject<MutatePlaylistResponse>(jsonData);
                resp = mutateResponse.MutateResponses[0];
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }

            GoogleMusicPlaylist pl = new GoogleMusicPlaylist();
            pl.ID = resp.ID;
            pl.Name = playlistName;
            playlistContainer.Add(pl.ID, pl);

            return resp;
        }
        #endregion

        #region GetTrack
        /// <summary>
        /// Returns user or instant playlist
        /// </summary>
        public GoogleMusicSong GetTrack(String id)
        {
            GoogleMusicSong song = trackContainer[id];

            return song;
        }
        #endregion

        #region GetPlaylist
        /// <summary>
        /// Returns user or instant playlist
        /// </summary>
        public GoogleMusicPlaylist GetPlaylist(String plID)
        {
            GoogleMusicPlaylist pl = playlistContainer[plID];

            return pl;
        }
        #endregion

        #region GetAllPlaylists
        /// <summary>
        /// Returns all user and instant playlists
        /// </summary>
        public void GetAllPlaylists()
        {
            playlistContainer = new SortedDictionary<String, GoogleMusicPlaylist>();
            GetAllPlaylistRequest();
        }

        // Fetch the full list of playlists
        private void GetAllPlaylistRequest(string nextPageToken = "")
        {
            String dataString = "";
            if (nextPageToken != "")
                dataString = "{'start-token':'" + nextPageToken + "'}";
            byte[] data = Encoding.UTF8.GetBytes(dataString);

            client.UploadDataAsync(new Uri(sjUrl + "playlistfeed?alt=json&updated-min=0&include-tracks=true"), "application/json", data, 10000, PlaylistsReceived, null);

        }

        private void PlaylistsReceived(HttpWebRequest request, HttpWebResponse response, String jsonData, Exception error)
        {
            GoogleMusicPlaylists playlistsReceived = new GoogleMusicPlaylists();
            
            try
            {
                playlistsReceived = JsonConvert.DeserializeObject<GoogleMusicPlaylists>(jsonData);
            }
            catch (Exception e)
            {
                OnError(e);
                return;
            }


            foreach (GoogleMusicPlaylist playlist in playlistsReceived.Data.playlists)
            {
                if (!playlist.Deleted && playlist.Type == "USER_GENERATED" && !String.IsNullOrEmpty(playlist.Name))
                    playlistContainer.Add(playlist.ID, playlist);
            }


            // Check for a nextPageToken, and if there is one go round the loop again
            JObject playlistJson = JObject.Parse(jsonData);
            string nextPageToken = "";
            try { nextPageToken = playlistJson["nextPageToken"].ToString(); }
            catch { }

            if (nextPageToken != "")
            {
                GetAllPlaylistRequest(nextPageToken);
                return;
            }

            GetAllPlaylistSongs();

            if (OnGetAllPlaylistsComplete != null)
                OnGetAllPlaylistsComplete();
        }
        #endregion

        #region GetAllPlaylistSongs
        // We have to get a full list of songs which are in playlists, and then populate the list of playlists with them
        public void GetAllPlaylistSongs(string nextPageToken = "")
        {
            String dataString = "";
            if (nextPageToken != "")
                dataString = "{'start-token':'" + nextPageToken + "'}";
            byte[] data = Encoding.UTF8.GetBytes(dataString);

            client.UploadDataAsync(new Uri(sjUrl + "plentryfeed?alt=json&updated-min=0&include-tracks=true"), "application/json", data, 10000, PlaylistSongsReceived, null);
        }

        // This feels awfully hacky. (and potentially very slow).
        // For each element in the received JSON, fetch the song object from our previously created "trackContainer" list
        // then fetch the playlist from our "playlistContainer" list
        // finally, remove the playlist from the list, update it to have the song object attached, and then re-add it to the list (this will change the list order, but that's not particuarly important)
        // A song which has been "deleted" from a playlist won't be added to it.
        private void PlaylistSongsReceived(HttpWebRequest request, HttpWebResponse response, String jsonData, Exception error)
        {
            JObject allSongsReceived = JObject.Parse(jsonData);

            if (allSongsReceived == null) return;

            foreach (JObject song in allSongsReceived["data"]["items"])
            {
                GoogleMusicPlaylistEntry thisSong = JsonConvert.DeserializeObject<GoogleMusicPlaylistEntry>(song.ToString());

                try
                { 
                    GoogleMusicPlaylist thisPlaylist = playlistContainer[thisSong.PlaylistID];
                    thisPlaylist.Songs.Add(thisSong);
                }
                catch { }
            }

            // Check to see if there's a nextpage token
            // This try/catch approach is pretty awful and done because of laziness. I assume
            // Json.Net has some sort of "does token exist" method?
            string nextPageToken = "";
            try { nextPageToken = allSongsReceived["nextPageToken"].ToString(); }
            catch { }

            if (nextPageToken != "")
            {
                GetAllPlaylistSongs(nextPageToken);
                return;
            }

            if (OnGetAllPlaylistSongsComplete != null)
                OnGetAllPlaylistSongsComplete();
        }
        #endregion

        #region DeletePlaylist
        public MutateResponse DeletePlaylist(String playlistID)
        {
            JObject requestData = new JObject
            {{"mutations", new JArray
            {new JObject 
                {{"delete", playlistID}}
            }
            }};

            byte[] data = Encoding.UTF8.GetBytes(requestData.ToString());

            String jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri(sjUrl + "playlistbatch?alt=json"), "application/json", data));

            MutateResponse resp = null;

            try
            {
                MutatePlaylistResponse mutateResponse = JsonConvert.DeserializeObject<MutatePlaylistResponse>(jsonData);
                resp = mutateResponse.MutateResponses[0];
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }

            playlistContainer.Remove(playlistID);

            return resp;
        }
        #endregion

        #region AddTracksToPlaylist
        public MutateResponse AddTracksToPlaylist(String playlistID, String[] songIds)
        {
            // Unique ID required to place each song in the list
            Guid prev_uid = Guid.NewGuid();
            Guid current_uid = Guid.NewGuid();
            Guid next_uid = Guid.NewGuid();

            // This function is taken more or less completely from def build_plentry_adds() in
            // the unofficial google music API
            JArray songsToAdd = new JArray();

            int i = 0;
            foreach (String id in songIds)
            {
                JObject songJObject = new JObject 
                {
                    { "clientId", current_uid.ToString() },
                    { "creationTimestamp", -1 },
                    { "deleted", false },
                    { "lastModifiedTimestamp", 0},
                    { "playlistId", playlistID },
                    { "source", 1 },
                    { "trackId", id }
                };

                //if (id.First() == 'T')
                //    songJObject["source"] = 2;

                if (i > 0)
                    songJObject["precedingEntryId"] = prev_uid;

                if (i < songIds.Length - 1)
                    songJObject["followingEntryId"] = next_uid;

                JObject createJObject = new JObject { { "create", songJObject } };

                songsToAdd.Add(createJObject);
                prev_uid = current_uid;
                current_uid = next_uid;
                next_uid = Guid.NewGuid();
                i++;
            }

            JObject requestData = new JObject
            {{
                 "mutations", songsToAdd
             }};

            byte[] data = Encoding.UTF8.GetBytes(requestData.ToString());

            String jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri(sjUrl + "plentriesbatch?alt=json"), "application/json", data));

            MutateResponse resp = null;

            try
            {
                MutatePlaylistResponse mutateResponse = JsonConvert.DeserializeObject<MutatePlaylistResponse>(jsonData);
                resp = mutateResponse.MutateResponses[0];
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }

            return resp;
        }
        #endregion

        #region DeleteTracks
        public MutateResponse DeleteTracks(String[] songIds)
        {
            JArray songsToDelete = new JArray();

            foreach (String id in songIds)
            {
                JObject songJObject = new JObject 
                {
                    { "delete", id }
                };

                songsToDelete.Add(songJObject);
            }

            JObject requestData = new JObject
            {{
                 "mutations", songsToDelete
            }};

            byte[] data = Encoding.UTF8.GetBytes(requestData.ToString());

            String jsonData = Encoding.UTF8.GetString(client.UploadDataSync(new Uri(sjUrl + "trackbatch?alt=json"), "application/json", data));

            MutateResponse resp = null;

            try
            {
                MutatePlaylistResponse mutateResponse = JsonConvert.DeserializeObject<MutatePlaylistResponse>(jsonData);
                resp = mutateResponse.MutateResponses[0];
            }
            catch (Exception e)
            {
                OnError(e);
                return null;
            }

            
            foreach (String id in songIds)
            {
                trackContainer.Remove(id);
            }

            
            return resp;
        }
        #endregion

        #region UploadAuth
        public void UploadAuth(string uploaderId, string uploaderFriendlyName)
        {
            UploadResponse uploadResponse = null;

            try
            {
                UpAuthRequest upAuthRequest = new UpAuthRequest();
                upAuthRequest.uploader_id = uploaderId;
                upAuthRequest.friendly_name = uploaderFriendlyName;


                HttpWebRequest request = oAuthClient.UploadWriteStreamSync(new Uri(AndroidUrl + "upauth"), ProtoBuffersContentType, UserAgent);
                Stream requestStream = new MemoryStream();
                Serializer.Serialize(requestStream, upAuthRequest);

                Stream response = oAuthClient.UploadReadStreamSync(request, requestStream);

                uploadResponse = Serializer.Deserialize<UploadResponse>(response);
                response.Close();
            }
            catch(Exception error)
            {
                OnError(error);
                return;
            }

            if (uploadResponse.auth_status != UploadResponse.AuthStatus.OK)
            {
                Exception ex = new Exception(uploadResponse.auth_status.ToString());
                OnError(ex);
                return;
            }
        }
        #endregion

        #region UploadMetadata
        public void UploadMetadata(SortedDictionary<String, Track> tracks, String uploaderId, out List<TrackSampleResponse> responses, out List<SignedChallengeInfo> requests)
        {
            responses = null;
            requests = null;

            UploadResponse uploadResponse = null;

            try
            {
                UploadMetadataRequest uploadMetadataRequest = new UploadMetadataRequest();
                uploadMetadataRequest.uploader_id = uploaderId;
                foreach (string clientId in tracks.Keys)
                    uploadMetadataRequest.track.Add(tracks[clientId]);

                HttpWebRequest request = oAuthClient.UploadWriteStreamSync(new Uri(AndroidUrl + "metadata?version=1"), ProtoBuffersContentType, UserAgent);
                Stream requestStream =  new MemoryStream();
                Serializer.Serialize(requestStream, uploadMetadataRequest);

                Stream response = oAuthClient.UploadReadStreamSync(request, requestStream);

                uploadResponse = Serializer.Deserialize<UploadResponse>(response);
                response.Close();
            }
            catch (Exception error)
            {
                OnError(error);
                return;
            }


            responses = uploadResponse.metadata_response.track_sample_response;
            requests = uploadResponse.metadata_response.signed_challenge_info;
        }
        #endregion

        #region UploadTracks
        private void UploadTracksInternal(SortedDictionary<String, String> trackPaths, SortedDictionary<String, Track> tracks, String uploaderId, List<TrackSampleResponse> responses, List<SignedChallengeInfo> requests, out SortedDictionary<String, String> serverIds, out SortedDictionary<String, String> existingServerIds)
        {
            foreach (SignedChallengeInfo sampleRequest in requests)
            {
                String path = trackPaths[sampleRequest.challenge_info.client_track_id];
                Track track = tracks[sampleRequest.challenge_info.client_track_id];

                TrackSample trackSample = new TrackSample();
                trackSample.track = track;
                trackSample.signed_challenge_info = sampleRequest;
                trackSample.sample = new Byte[0];

                UploadSampleRequest uploadSampleRequest = new UploadSampleRequest();
                uploadSampleRequest.uploader_id = uploaderId;
                uploadSampleRequest.track_sample.Add(trackSample);


                HttpWebRequest request2 = oAuthClient.UploadWriteStreamSync(new Uri(AndroidUrl + "sample?version=1"), ProtoBuffersContentType, UserAgent);
                Stream requestStream2 = new MemoryStream();
                Serializer.Serialize(requestStream2, uploadSampleRequest);

                Stream response2 = oAuthClient.UploadReadStreamSync(request2, requestStream2);

                UploadResponse uploadResponse2 = Serializer.Deserialize<UploadResponse>(response2);
                response2.Close();


                foreach (TrackSampleResponse trackSampleResponse in uploadResponse2.sample_response.track_sample_response)
                    responses.Add(trackSampleResponse);
            }


            serverIds = new SortedDictionary<String, String>();
            existingServerIds = new SortedDictionary<String, String>();
            foreach (TrackSampleResponse trackSampleResponse in responses)
            {
                String path = trackPaths[trackSampleResponse.client_track_id];
                Track track = tracks[trackSampleResponse.client_track_id];

                if (trackSampleResponse.response_code == TrackSampleResponse.ResponseCode.MATCHED)
                {
                    //Nothing to upload
                }
                else if (trackSampleResponse.response_code == TrackSampleResponse.ResponseCode.ALREADY_EXISTS)
                {
                    existingServerIds.Add(trackSampleResponse.client_track_id, trackSampleResponse.server_track_id);
                }
                else if (trackSampleResponse.response_code == TrackSampleResponse.ResponseCode.UPLOAD_REQUESTED)
                {
                    serverIds.Add(trackSampleResponse.client_track_id, trackSampleResponse.server_track_id);
                }
                else //Error
                {
                    Exception ex = new Exception(trackSampleResponse.response_code.ToString());
                    OnError(ex);
                    return;
                }
            }


            if (serverIds.Keys.Count > 0)
            {
                UpdateUploadStateRequest updateUploadStateRequest = new UpdateUploadStateRequest();
                updateUploadStateRequest.uploader_id = uploaderId;
                updateUploadStateRequest.state = UpdateUploadStateRequest.UploadState.START;


                HttpWebRequest request3 = oAuthClient.UploadWriteStreamSync(new Uri(AndroidUrl + "sample?version=1"), ProtoBuffersContentType, UserAgent);
                Stream requestStream3 = new MemoryStream();
                Serializer.Serialize(requestStream3, updateUploadStateRequest);

                Stream response3 = oAuthClient.UploadReadStreamSync(request3, requestStream3);

                UploadResponse uploadResponse3 = Serializer.Deserialize<UploadResponse>(response3);
                response3.Close();


                int numAlreadyUploaded = 0;
                foreach (String clientId in serverIds.Keys)
                {
                    RefreshAccessToken();


                    String serverId = serverIds[clientId];
                    String path = trackPaths[clientId];
                    Track track = tracks[clientId];

                    FileInfo fileInfo = new FileInfo(path);

                    JObject jsonResult = null;

                    bool gotSession = false;
                    bool shouldRetry = true;
                    int attempts = 0;
                    while (shouldRetry && attempts < 10)
                    {
                        
                        String jsonString = "{" + 
                            "\"clientId\": \"Jumper Uploader\"," +
                            "\"createSessionRequest\": {" +
                            "\"fields\": [" +
                                    "{" +
                                        "\"external\": {" +
                                            "\"filename\":\"" + fileInfo.Name + "\"," +
                                            "\"name\":\"" + fileInfo.FullName.Replace("\\", "\\\\") + "\"," +
                                            "\"put\": {}" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"jumper-uploader-title-42\"," +
                                            "\"name\":\"title\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + clientId + "\"," +
                                            "\"name\":\"ClientId\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"1\"," +
                                            "\"name\":\"ClientTotalSongCount\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + numAlreadyUploaded + "\"," +
                                            "\"name\":\"CurrentTotalUploadedCount\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + tracks[clientId].title + "\"," +
                                            "\"name\":\"CurrentUploadingTrack\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + serverId + "\"," +
                                            "\"name\":\"ServerId\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"true\"," +
                                            "\"name\":\"SyncNow\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + tracks[clientId].original_bit_rate + "\"," +
                                            "\"name\":\"TrackBitRate\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"true\"," +
                                            "\"name\":\"TrackDoNotRematch\"" +
                                        "}" +
                                    "}," +
                                    "{" +
                                        "\"inlined\": {" +
                                            "\"content\":\"" + uploaderId + "\"," +
                                            "\"name\":\"UploaderId\"" +
                                        "}" +
                                    "}" +
                                "]" + 
                            "}," + 
                            "\"protocolVersion\": \"0.8\"" + 
                         "}";


                        byte[] result = oAuthClient.UploadDataSync(new Uri("https://uploadsj.clients.google.com/uploadsj/rupio"), "application/json", Encoding.UTF8.GetBytes(jsonString));
                        jsonResult = JObject.Parse(Encoding.UTF8.GetString(result));

                        JToken value;
                        if (jsonResult.TryGetValue("sessionStatus", out value))
                        {
                            shouldRetry = false;
                            gotSession = true;
                        }
                        else
                        {
                            gotSession = false;

                            if (jsonResult.TryGetValue("errorMessage", out value))
                            {
                                string responseCode = jsonResult["errorMessage"]["additionalInfo"]["uploader_service.GoogleRupioAdditionalInfo"]["completionInfo"]["customerSpecificInfo"]["ResponseCode"].ToString();

                                switch (responseCode)
                                {
                                    case "503": //upload servers still syncing
                                        shouldRetry = true;
                                        break;
                                    case "200": //this song is already uploaded
                                        shouldRetry = false;
                                        break;
                                    case "404": //the request was rejected
                                        shouldRetry = false;
                                        break;
                                    default: //the server reported an unknown error
                                        shouldRetry = true;
                                        break;
                                }
                            }
                            else //the server's response could not be understood
                            {
                                shouldRetry = true;
                            }
                        }

                        attempts++;

                        if (shouldRetry)
                            System.Threading.Thread.Sleep(500); //0.5 sec.
                    }

                    if (!gotSession)
                    {
                        //Exception ex = new Exception("Server error");
                        //OnError(ex);
                        continue;
                    }


                    JToken session = jsonResult["sessionStatus"];
                    JToken external = session["externalFieldTransfers"][0];

                    string sessionUrl = external["putInfo"]["url"].ToString();
                    string contentType = "audio/mpeg";

                    HttpWebRequest request = oAuthClient.UploadWriteStreamSync(new Uri(sessionUrl), contentType, UserAgent, "PUT");
                    Stream requestStream = new MemoryStream();

                    Stream file = System.IO.File.OpenRead(path);
                    file.CopyTo(requestStream);
                    file.Close();

                    Stream response = oAuthClient.UploadReadStreamSync(request, requestStream);
                    response.Close();

                    numAlreadyUploaded++;
                }

                UpdateUploadStateRequest updateUploadStateRequest2 = new UpdateUploadStateRequest();
                updateUploadStateRequest2.uploader_id = uploaderId;
                updateUploadStateRequest2.state = UpdateUploadStateRequest.UploadState.STOPPED;


                HttpWebRequest request4 = oAuthClient.UploadWriteStreamSync(new Uri(AndroidUrl + "sample?version=1"), ProtoBuffersContentType, UserAgent);
                Stream requestStream4 = new MemoryStream();
                Serializer.Serialize(requestStream4, updateUploadStateRequest2);

                Stream response4 = oAuthClient.UploadReadStreamSync(request4, requestStream4);

                UploadResponse uploadResponse4 = Serializer.Deserialize<UploadResponse>(response4);
                response4.Close();
            }

            return;
        }

        public void UploadTracks(SortedDictionary<String, String> trackPaths, SortedDictionary<String, Track> tracks, out SortedDictionary<String, String> serverIds, out SortedDictionary<String, String> existingServerIds)
        {
            UploadAuth(DeviceId, DeviceFriendlyName);

            List<TrackSampleResponse> responses;
            List<SignedChallengeInfo> requests;
            UploadMetadata(tracks, DeviceId, out responses, out requests);

            UploadTracksInternal(trackPaths, tracks, DeviceId, responses, requests, out serverIds, out existingServerIds);

            foreach (string clientId in serverIds.Keys)
            {
                string serverId = serverIds[clientId];

                GoogleMusicSong song = new GoogleMusicSong();
                CopyTrackToSong(serverId, tracks[clientId], song);

                trackContainer.Add(serverId, song);
            }

            foreach (string clientId in existingServerIds.Keys)
            {
                string existingServerId = existingServerIds[clientId];

                GoogleMusicSong song = new GoogleMusicSong();
                CopyTrackToSong(existingServerId, tracks[clientId], song);

                trackContainer.Add(existingServerId, song);
            }

            return;
        }

        public String UploadTrack(String clientId, String trackPath, Track track)
        {
            SortedDictionary<String, String> serverIds;
            SortedDictionary<String, String> existingServerIds;

            UploadTracks(new SortedDictionary<String, String> { { clientId, trackPath } }, new SortedDictionary<String, Track> { { clientId, track } }, out serverIds, out existingServerIds);

            if (serverIds.Count == 0)
                return existingServerIds[clientId];
            else
                return serverIds[clientId];
        }
        #endregion

        #region DownloadSong
        public String GetDownloadLink(String serverId) //This version uses MusicManager API unlike GetSongURL
        {
            RefreshAccessToken();

            String jsonData = oAuthClient.DownloadStringSync(new Uri("https://music.google.com/music/export?version=2&songid=" + serverId), DeviceId, UserAgent);
            Dictionary<String, String> json = JsonConvert.DeserializeObject<Dictionary<String, String>>(jsonData);

            return json["url"];
        }

        public HttpWebResponse GetDownloadResponse(String url) //This version uses MusicManager API
        {
            var request = oAuthClient.SetupRequest(new Uri(url), UserAgent);
            request.Method = "GET";

            var response = (HttpWebResponse)request.GetResponse();

            return response;
        }

        public void DownloadSong(HttpWebResponse response, String path) //This version uses MusicManager API
        {
            // Get the stream to read from
            using (Stream downloadStream = response.GetResponseStream())
            {
                FileStream fileStream = new FileStream(path, FileMode.CreateNew);

                byte[] buffer = new Byte[1024];

                int bytesRead;
                while ((bytesRead = downloadStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    fileStream.Write(buffer, 0, bytesRead);
                }

                fileStream.Close();
                downloadStream.Close();
            }

            return;
        }
        #endregion

        #region Utils
        public void CopyTrackToSong(String serverId, Track track, GoogleMusicSong song)
        {
            song.Album = track.album;
            song.AlbumArtist = track.album_artist;
            song.Artist = track.artist;
            song.BPM = track.beats_per_minute;
            song.Comment = track.comment;
            song.Composer = track.composer;
            song.Disc = track.disc_number;
            song.Genre = track.genre;
            song.ID = serverId;
            song.Title = track.title;
            song.TotalDiscs = track.total_disc_count;
            song.TotalTracks = track.total_track_count;
            song.Track = track.track_number;
            song.Year = track.year;
        }
        #endregion
    }
}
