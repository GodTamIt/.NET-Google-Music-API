using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace GoogleMusic
{
    
    public class Playlist : ICloneable
    {

        #region Properties: Info

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("id")]
        public Guid ID { get; protected internal set; }

        [JsonProperty("creationTimestamp")]
        public long CreationTimestampMicroseconds { get; protected internal set; }

        [JsonProperty("recentTimestamp")]
        public long RecentTimestampMicrseconds { get; protected internal set; }

        [JsonProperty("type")]
        public int Type { get; protected internal set; }

        [JsonProperty("sharedToken")]
        public string SharedToken { get; protected internal set; }

        [JsonProperty("lastModifiedTimestamp")]
        public long LastModifiedTimestampMicroseconds { get; protected internal set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("ownerName")]
        public string OwnerName { get; protected internal set; }

        [JsonProperty("playlistArtUrl")]
        public string[] PlaylistArtUrl { get; set; }

        [JsonProperty("ownerProfilePhotoUrl")]
        public string OwnerProfilePhotoUrl { get; protected internal set; }

        [JsonProperty("suggestedPlaylistArtUrl")]
        public string[] SuggestedPlaylistArtUrl { get; protected internal set; }

        [JsonProperty("requestTime")]
        public double RequestTime { get; set; }

        #endregion

        #region Properties: Calculated

        public DateTime CreationTimestamp
        {
            get { return FromUnixMicroseconds(CreationTimestampMicroseconds); }
        }

        public DateTime RecentTimestamp
        {
            get { return FromUnixMicroseconds(RecentTimestampMicrseconds); }
        }

        public DateTime LastModifiedTimestamp
        {
            get { return FromUnixMicroseconds(LastModifiedTimestampMicroseconds); }
        }

        private static DateTime FromUnixMicroseconds(long microseconds)
        {
            return new DateTime(1970, 01, 01).AddTicks(microseconds * (TimeSpan.TicksPerMillisecond / 1000)).ToLocalTime();
        }

        #endregion

        #region Properties: Songs

        private List<Song> _songs;
        public List<Song> Songs
        {
            get { return _songs; }
            set
            {
                if (value == null && _songs != null)
                    _songs = new List<Song>();
                else
                    _songs = value;
            }
        }

        #endregion

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
