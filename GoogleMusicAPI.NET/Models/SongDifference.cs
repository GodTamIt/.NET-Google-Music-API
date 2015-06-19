using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMusic
{
    public class SongDifference
    {
        public SongDifference(Guid ID)
        {
            if (ID == Guid.Empty)
                throw new ArgumentException("ID cannot be empty");

            this.ID = ID;
        }

        #region Properties

        public Guid ID { get; private set; }
        public string Title { get; set; }
        public string AlbumArt { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string TitleNorm { get; set; }
        public string ArtistNorm { get; set; }
        public string AlbumNorm { get; set; }
        public string AlbumArtistNorm { get; set; }
        public string Composer { get; set; }
        public string Genre { get; set; }
        public long? Duration { get; set; }
        public int? Track { get; set; }
        public int? TotalTracks { get; set; }
        public int? Disc { get; set; }
        public int? TotalDiscs { get; set; }
        public int? Year { get; set; }
        public int? PlayCount { get; set; }
        public int? Rating { get; set; }
        public float? CreationDate { get; set; }
        public double? LastPlayed { get; set; }
        public string StoreID { get; set; }
        public string MatchedID { get; set; }
        public int? Type { get; set; }
        public string Comment { get; set; }
        public string FixMatchNeeded { get; set; }
        public string ArtURL { get; set; }
        public bool? PermanentlyDelete { get; set; }
        public string Unknown12 { get; set; }
        public bool? IsDeleted { get; set; }
        public string Pending { get; set; }
        public string SubjectToCuration { get; set; }
        public string MatchedAlbumId { get; set; }
        public string MatchedArtistId { get; set; }
        public int? Bitrate { get; set; }
        public bool? Explicit { get; set; }
        public string OtherMatchedId { get; set; }

        #endregion

        public object[] ToArray()
        {
            return new object[] {
                this.ID,
                this.Title,
                this.AlbumArt,
                this.Artist,
                this.Album,
                this.AlbumArtist,
                this.TitleNorm,
                this.ArtistNorm,
                this.AlbumNorm,
                this.AlbumArtistNorm,
                this.Composer, //10
                this.Genre,
                this.Unknown12,
                this.Duration,
                this.Track,
                this.TotalTracks,
                this.Disc,
                this.TotalDiscs,
                this.Year,
                this.IsDeleted != null ? (int?)Convert.ToInt32(this.IsDeleted) : null,
                this.PermanentlyDelete, //20
                this.Pending,
                this.PlayCount,
                this.Rating,
                this.CreationDate,
                this.LastPlayed,
                this.SubjectToCuration,
                this.StoreID,
                this.MatchedID,
                this.Type,
                this.Comment, //30
                null,
                this.MatchedAlbumId,
                this.MatchedArtistId,
                this.Bitrate,
                null,
                this.ArtURL,
                null,
                this.Explicit != null ? (int?)Convert.ToInt32(this.Explicit) : null,
                null,
                null, //40
                null,
                true,
            };
        }

    }
}