using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GoogleMusic
{
    
    public class Playlist
    {
        #region Properties: Info

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("id")]
        public Guid ID { get; protected set; }

        [JsonProperty("creationTimestamp")]
        public long CreationTimestampMicroseconds { get; protected set; }

        [JsonProperty("recentTimestamp")]
        public long RecentTimestampMicrseconds { get; protected set; }

        [JsonProperty("type")]
        public int Type { get; protected set; }

        [JsonProperty("sharedToken")]
        public string SharedToken { get; protected set; }

        [JsonProperty("lastModifiedTimestamp")]
        public long LastModifiedTimestampMicroseconds { get; protected set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("ownerName")]
        public string OwnerName { get; protected set; }

        [JsonProperty("playlistArtUrl")]
        public string[] PlaylistArtUrl { get; set; }

        [JsonProperty("ownerProfilePhotoUrl")]
        public string OwnerProfilePhotoUrl { get; protected set; }

        [JsonProperty("suggestedPlaylistArtUrl")]
        public string[] SuggestedPlaylistArtUrl { get; protected set; }

        [JsonProperty("requestTime")]
        public double RequestTime { get; set; }

        #endregion

        #region Properties: Songs

        private List<Song> _songs;
        public List<Song> Songs
        {
            get { return _songs; }
            set
            {
                if (value == null)
                    _songs = new List<Song>();
                else
                    _songs = value;
            }
        }

        #endregion
    }
}
