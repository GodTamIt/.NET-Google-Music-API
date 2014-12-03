using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GoogleMusicAPI
{
    public class GoogleMusicSongUrl
    {
        [JsonProperty("url")]
        public String URL { get; set; }
    };

    public class GoogleSongData
    {
        [JsonProperty("items")]
        public List<GoogleMusicSong> Songs { get; set; }
    }
    
    public class GetAllSongsResp
    {
        [JsonProperty("data")]
        public GoogleSongData Data { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    public class MutateResponse
    {
        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("client_id")]
        public string client_id { get; set; }
        
        [JsonProperty("response_code")]
        public string response_code { get; set; }
    }

    public class MutatePlaylistResponse
    {
        [JsonProperty("mutate_response")]
        public List<MutateResponse> MutateResponses { get; set; }
    }
    
    public class CreatePlaylistResp
    {
        [JsonProperty("id")]
        public String ID { get; set; }

        [JsonProperty("title")]
        public String Title { get; set; }

        [JsonProperty("success")]
        public bool Success { get; set; }
    }

    public class DeletePlaylistResp
    {
        [JsonProperty("deleteId")]
        public String ID { get; set; }
    }

    public class AddTracksToPlaylistResp
    {
        [JsonProperty("playlistId")]
        public String ID { get; set; }

        [JsonProperty("songIds")]
        public List<PlaylistSong> Songs { get; set; }
    }

    public class TracksDeletedResp
    {
        [JsonProperty("listId")]
        public String ID { get; set; }

        [JsonProperty("deleteIds")]
        public List<String> Songs { get; set; }
    }

    public class PlaylistSong
    {
        [JsonProperty("songId")]
        public String ID { get; set; }

        [JsonProperty("playlistEntryId")]
        public String EntryID { get; set; }
    }

    public class GooglePlaylistData
    {
        [JsonProperty("items")]
        public List<GoogleMusicPlaylist> playlists { get; set; }
    }

    public class GoogleMusicPlaylists
    {
        [JsonProperty("data")]
        public GooglePlaylistData Data { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }
    }

    public class GoogleMusicPlaylist
    {
        public override string ToString()
        {
            // Generates the text shown in the combo box
            return Name;
        }

        public GoogleMusicPlaylist()
        {
            Songs = new List<GoogleMusicPlaylistEntry>();
        }

        [JsonProperty("accessControlled")]
        public string AccessControlled { get; set; }

        [JsonProperty("creationTimestamp")]
        public string CreationTimestamp { get; set; }

        [JsonProperty("deleted")]
        public Boolean Deleted { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("lastModifiedTimestamp")]
        public string lastModifiedTimestamp { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ownerName")]
        public string OwnerName { get; set; }

        [JsonProperty("ownerProfilePhotoUrl")]
        public string OwnerProfilePhotoURL { get; set; }

        [JsonProperty("recentTimestamp")]
        public string RecentTimestamp { get; set; }

        [JsonProperty("shareToken")]
        public string ShareToken { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        public List<GoogleMusicPlaylistEntry> Songs { get; set; }
    }

    public class GoogleMusicPlaylistEntry
    {
        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("id")]
        public string ID { get; set; }

        [JsonProperty("playlistId")]
        public string PlaylistID { get; set; }

        [JsonProperty("absolutePosition")]
        public string AbsolutePosition { get; set; }

        [JsonProperty("trackId")]
        public string TrackID { get; set; }

        [JsonProperty("creationTimestamp")]
        public string CreationTimestamp { get; set; }

        [JsonProperty("lastModifiedTimestamp")]
        public string LastModifiedTimestamp { get; set; }

        [JsonProperty("deleted")]
        public Boolean Deleted { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }
    
    public class GoogleMusicSong
    {
        string albumart;

        [JsonProperty( "genre")]
        public string Genre { get; set; }

        [JsonProperty( "beatsPerMinute")]
        public int BPM { get; set; }

        [JsonProperty( "albumArtistNorm")]
        public string AlbumArtistNorm { get; set; }

        [JsonProperty( "artistNorm")]
        public string ArtistNorm { get; set; }

        [JsonProperty( "album")]
        public string Album { get; set; }

        [JsonProperty( "lastPlayed")]
        public double LastPlayed { get; set; }

        [JsonProperty( "type")]
        public int Type { get; set; }

        [JsonProperty( "disc")]
        public int Disc { get; set; }

        [JsonProperty( "id")]
        public string ID { get; set; }

        [JsonProperty( "composer")]
        public string Composer { get; set; }

        [JsonProperty( "title")]
        public string Title { get; set; }

        [JsonProperty( "albumArtist")]
        public string AlbumArtist { get; set; }

        [JsonProperty( "totalTracks")]
        public int TotalTracks { get; set; }

        [JsonProperty( "name")]
        public string Name { get; set; }

        [JsonProperty( "totalDiscs")]
        public int TotalDiscs { get; set; }

        [JsonProperty( "year")]
        public int Year { get; set; }

        [JsonProperty( "titleNorm")]
        public string TitleNorm { get; set; }

        [JsonProperty( "artist")]
        public string Artist { get; set; }

        [JsonProperty( "albumNorm")]
        public string AlbumNorm { get; set; }

        [JsonProperty( "track")]
        public int Track { get; set; }

        [JsonProperty( "durationMillis")]
        public long Duration { get; set; }

        [JsonProperty( "albumArt")]
        public string AlbumArt { get; set; }

        [JsonProperty( "deleted")]
        public bool Deleted { get; set; }

        [JsonProperty( "url")]
        public string URL { get; set; }

        [JsonProperty( "creationDate")]
        public float CreationDate { get; set; }

        [JsonProperty( "playCount")]
        public int Playcount { get; set; }

        [JsonProperty( "rating")]
        public int Rating { get; set; }

        [JsonProperty( "comment")]
        public string Comment { get; set; }

        [JsonProperty( "albumArtUrl")]
        public string ArtURL
        {
            get
            {
                return (!albumart.StartsWith("http:")) ? "http:" + albumart : albumart;
            }
            set { albumart = value; }
        }

        public string ArtistAlbum
        {
            get
            {
                return Artist + ", " + Album;
            }
        }
    }
}
