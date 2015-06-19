using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMusic
{
    /// <summary>
    /// A class representing a song in the Google Music API.
    /// </summary>
    public class Song
    {
        #region Builder

        /// <summary>
        /// Builds a Google Music song from a dynamic type.
        /// </summary>
        /// <param name="track">Required. The dynamic object to build from. The function expects this to be an array at runtime.</param>
        /// <returns></returns>
        public static Song BuildFromDynamic(dynamic track)
        {
            Song song = new Song();

            song.ID = track[0];
            song.Title = track[1];
            song.AlbumArt = track[2];
            song.Artist = track[3];
            song.Album = track[4];
            song.AlbumArtist = track[5];
            song.TitleNorm = track[6];
            song.ArtistNorm = track[7];
            song.AlbumNorm = track[8];
            song.AlbumArtistNorm = track[9];
            song.Composer = track[10];
            song.Genre = track[11];
            song.fhb = track[12];
            song.Duration = (long)track[13];
            song.Track = track[14] == null ? 0 : track[14];
            song.TotalTracks = track[15] == null ? 0 : track[15];
            song.Disc = track[16] == null ? 0 : track[16];
            song.TotalDiscs = track[17] == null ? 0 : track[17];
            song.Year = track[18];
            song.IsDeleted = track[19] == null ? 0 : track[19];
            song.PermanentlyDelete = track[20];
            song.Pending = track[21];
            song.PlayCount = track[22] == null ? 0 : track[22];
            song.Rating = track[23] == null ? 0 : track[23];
            song.CreationDateMicroseconds = track[24];
            song.LastPlayedMicroseconds = track[25];
            song.SubjectToCuration = track[26];
            song.StoreID = track[27];
            song.MatchedID = track[28];
            // 27/28 seem same?
            song.Type = (int)track[29];
            song.Comment = track[30];
            song.FixMatchNeeded = track[31]; // 31 fix match needed
            song.MatchedAlbumId = track[32]; // 32 matched album
            song.MatchedArtistId = track[33]; // 33 matched artist
            if (track[34] != null) song.Bitrate = track[34]; // 34 bitrate
            song.RecentTimetampMicroseconds = track[35] == null ? 0 : track[35];  // 35 recent timestamp
            song.ArtURL = track[36];
            song.AlbumPlaybackTimestamp = track[37];
            song.Explicit = (track[38] == null ? false : Convert.ToBoolean(track[38]));
            song.Rjb = track[39];
            song.PreviewToken = track[40];
            song.CurationSuggested = track[41];
            song.CuratedByUser = track[42];
            song.PlaylistEntryId = track[43];
            song.SharingInfo = track[44];
            song.PreviewInfo = track[45];
            song.AlbumArtUrl = track[46];

            if (track.Count <= 47) return song;

            if (track.Count > 47)
            {
                song.Explanation = track[47];
                song.dib = track[48];
                song.fib = track[49];
                song.qeb = track[50];
                if (track.Count > 50)
                {
                    song.OtherMatchedId = track[50];
                }
                song.Unknown51 = track[51];
                if (track.Count > 52)
                {
                    song.YoutubeId = track[52];
                    song.Unknown53 = track[53];
                    song.Unknown54 = track[54];
                }
                if (track.Count > 55)
                {
                    song.YoutubeInfo = track[55];
                }
                if (track.Count > 56)
                {
                    song.u5 = track[56];
                }
            }
            return song;
        }

        #endregion

        #region Properties: Original
        public Guid ID { get; protected set; }
        public string Title { get; set; }
        public string AlbumArt { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Composer { get; set; }
        public string Genre { get; set; }
        public long Duration { get; protected set; }
        public int Track { get; set; }
        public int TotalTracks { get; set; }
        public int Disc { get; set; }
        public int TotalDiscs { get; set; }
        public int? Year { get; set; }
        public int PlayCount { get; set; }
        public int Rating { get; set; }
        public long CreationDateMicroseconds { get; protected set; }
        public long? LastPlayedMicroseconds { get; protected set; }
        public string StoreID { get; set; }
        public string MatchedID { get; set; }
        public int Type { get; set; }
        public string Comment { get; set; }
        public string FixMatchNeeded { get; set; }
        public string MatchedAlbumId { get; set; }
        public string MatchedArtistId { get; protected set; }
        public int? Bitrate { get; protected set; }
        public string ArtURL { get; set; }
        public string SubjectToCuration { get; set; }
        public string TitleNorm { get; set; }
        public string ArtistNorm { get; set; }
        public string AlbumNorm { get; set; }
        public string AlbumArtistNorm { get; set; }
        public string fhb { get; set; }
        public bool IsDeleted { get; set; }
        public bool? PermanentlyDelete { get; set; }
        public string Pending { get; set; }
        public long RecentTimetampMicroseconds { get; set; }
        public long? AlbumPlaybackTimestamp { get; set; } // last updated?
        public bool Explicit { get; set; }
        public string OtherMatchedId { get; set; }
        public JArray Rjb { get; set; }
        public string PreviewToken { get; set; }
        public string CurationSuggested { get; set; }
        public string CuratedByUser { get; set; }
        public string PlaylistEntryId { get; set; }
        public string SharingInfo { get; set; }
        public JArray PreviewInfo { get; set; }
        public string AlbumArtUrl { get; set; }
        public string Explanation { get; set; }
        public string dib { get; set; }
        public string fib { get; set; }
        public string qeb { get; set; }
        public JArray Unknown51 { get; set; }
        public string YoutubeId { get; set; }
        public string Unknown53 { get; set; }
        public JArray Unknown54 { get; set; }
        public JArray YoutubeInfo { get; set; }
        public string u5 { get; set; }
        #endregion

        #region Properties: Calculated

        [JsonIgnore]
        public DateTime CreationDate
        {
            get
            {
                return FromUnixMicroseconds(this.CreationDateMicroseconds);
            }
        }

        [JsonIgnore]
        public DateTime? LastPlayedDateTime
        {
            get
            {
                if (LastPlayedMicroseconds.HasValue)
                {
                    return FromUnixMicroseconds(this.LastPlayedMicroseconds.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        [JsonIgnore]
        public DateTime RecentTimestamp
        {
            get
            {
                return FromUnixMicroseconds(this.RecentTimetampMicroseconds);
            }
            set
            {

            }
        }

        [JsonIgnore]
        public DateTime? AlbumPlaybackDateTime
        {
            get
            {
                if (AlbumPlaybackTimestamp.HasValue)
                {
                    return FromUnixMicroseconds(this.AlbumPlaybackTimestamp.Value);
                }
                else
                {
                    return null;
                }
            }
        }

        private static DateTime FromUnixMicroseconds(long microseconds)
        {
            return new DateTime(1970, 01, 01).AddMilliseconds(microseconds / 1000).ToLocalTime();
        }

        #endregion

        #region Comaparisons

        public SongDiff CompareTo(Song update)
        {
            var diff = new SongDiff(this.ID)
            {
                Title = GetComparison(this.Title, update.Title),
                AlbumArt = GetComparison(this.AlbumArt, update.AlbumArt),
                Artist = GetComparison(this.Artist, update.Artist),
                Album = GetComparison(this.Album, update.Album),
                AlbumArtist = GetComparison(this.AlbumArtist, update.AlbumArtist),
                TitleNorm = GetComparison(this.TitleNorm, update.TitleNorm),
                ArtistNorm = GetComparison(this.ArtistNorm, update.ArtistNorm),
                AlbumNorm = GetComparison(this.AlbumNorm, update.AlbumArtistNorm),
                AlbumArtistNorm = GetComparison(this.AlbumNorm, update.AlbumArtistNorm),
                Composer = GetComparison(this.Composer, update.Composer), //10
                Genre = GetComparison(this.Genre, update.Genre),
                //Duration = GetComparison(this.Duration, update.Duration),
                Track = GetComparison(this.Track, update.Track),
                //TotalTracks = GetComparison(this.TotalTracks, update.TotalTracks),
                Disc = GetComparison(this.Disc, update.Disc),
                //TotalDiscs = GetComparison(this.TotalDiscs, update.TotalDiscs),
                Year = GetYearComparison(this.Year, update.Year),
                IsDeleted = GetComparison(this.IsDeleted, update.IsDeleted),
                PermanentlyDelete = GetComparison(this.PermanentlyDelete, update.PermanentlyDelete), //20
                Pending = GetComparison(this.Pending, update.Pending),
                //Playcount = GetComparison(this.Playcount, update.Playcount),
                //Rating = GetComparison(this.Rating, update.Rating),
                //CreationDate = GetComparison(this.CreationDate, update.CreationDate),
                //LastPlayed = GetComparison(this.LastPlayed, update.LastPlayed),
                SubjectToCuration = GetComparison(this.SubjectToCuration, update.SubjectToCuration),
                //StoreID = GetComparison(this.StoreID, update.StoreID),
                MatchedID = GetComparison(this.MatchedID, update.MatchedID),
                //Type = GetComparison(this.Type, update.Type),
                Comment = GetComparison(this.Comment, update.Comment), // 30
                FixMatchNeeded = GetComparison(this.FixMatchNeeded, update.FixMatchNeeded),
                MatchedAlbumId = GetComparison(this.MatchedAlbumId, update.MatchedAlbumId),
                MatchedArtistId = GetComparison(this.MatchedArtistId, update.MatchedArtistId),
                //Bitrate = GetComparison(this.Bitrate, update.Bitrate),
                ArtURL = GetComparison(this.ArtURL, update.ArtURL), // 36
                Explicit = GetComparison(this.Explicit, update.Explicit),

            };
            return diff;
        }

        public void ApplyDiff(SongDiff diff)
        {
            if (diff.ID != null) this.ID = diff.ID;
            if (diff.Title != null) this.Title = diff.Title;
            if (diff.AlbumArt != null) this.AlbumArt = diff.AlbumArt;
            if (diff.Artist != null) this.Artist = diff.Artist;
            if (diff.Album != null) this.Album = diff.Album;
            if (diff.AlbumArtist != null) this.AlbumArtist = diff.AlbumArtist;
            if (diff.TitleNorm != null) this.TitleNorm = diff.TitleNorm;
            if (diff.ArtistNorm != null) this.ArtistNorm = diff.ArtistNorm;
            if (diff.AlbumNorm != null) this.AlbumNorm = diff.AlbumNorm;
            if (diff.AlbumArtistNorm != null) this.AlbumArtistNorm = diff.AlbumArtistNorm;
            if (diff.Composer != null) this.Composer = diff.Composer;
            if (diff.Genre != null) this.Genre = diff.Genre;
            if (diff.Track != null) this.Track = diff.Track.Value;
            if (diff.Disc != null) this.Disc = diff.Disc.Value;
            if (diff.Year != null) this.Year = diff.Year;
            if (diff.IsDeleted != null) this.IsDeleted = diff.IsDeleted.Value;
            if (diff.PermanentlyDelete != null) this.PermanentlyDelete = diff.PermanentlyDelete;
            if (diff.Pending != null) this.Pending = diff.Pending;
            if (diff.SubjectToCuration != null) this.SubjectToCuration = diff.SubjectToCuration;
            if (diff.MatchedID != null) this.MatchedID = diff.MatchedID;
            if (diff.Comment != null) this.Comment = diff.Comment;
            if (diff.MatchedAlbumId != null) this.MatchedAlbumId = diff.MatchedAlbumId;
            if (diff.MatchedArtistId != null) this.MatchedArtistId = diff.MatchedArtistId;
            if (diff.Bitrate != null) this.Bitrate = diff.Bitrate.Value;
            if (diff.ArtURL != null) this.ArtURL = diff.ArtURL;
            if (diff.Explicit != null) this.Explicit = (bool)diff.Explicit;
        }

        private int? GetYearComparison(int? from, int? to)
        {
            if (to.HasValue == false) return null;
            if (from == to) return null;
            return to;
        }

        private T? GetComparison<T>(T? from, T? to) where T : struct, IComparable
        {
            if (to.HasValue == false) return null;
            if (from.HasValue == false) return to;
            if (from.Value.CompareTo(to.Value) == 0) return null;
            return to;
        }

        private T? GetComparison<T>(T from, T to) where T : struct, IComparable
        {
            if (from.CompareTo(to) == 0) return null;
            return to;
        }

        private string GetComparison(string from, string to)
        {
            if (from == to) return null;
            return to;
        }

        #endregion

    }
}