using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GoogleMusicAPI;
using System.Net;
using System.Threading.Tasks;
using wireless_android_skyjam;

namespace GoogleMusicTest
{
    public partial class GoogleTest : Form
    {
        API api = new API();
        GoogleMusicPlaylists pls;

        string ClientId = "215459815136.apps.googleusercontent.com";
        string ClientSecret = "tJ1gxPFfqt6FFvMqXm29QE6T";

        public GoogleTest()
        {
            InitializeComponent();

            api.OnGetAllSongsComplete += GetAllSongsDone;
            api.OnGetAllPlaylistsComplete += GetAllPlaylistsDone;
            api.OnGetAllSongsComplete += GetAllSongsComplete;
            api.RequestAuthCode(ClientId);

            api.DeviceFriendlyName = "test uploader";
            api.DeviceId = "mm:00:16:E6:88:04:57";
        }

        void GetAllSongsDone()
        {
            int num = 1;
            foreach (string serverId in api.trackContainer.Keys)
            {
                GoogleMusicSong song = api.trackContainer[serverId];

                ListViewItem lvi = new ListViewItem();
                lvi.Text = (num++).ToString();
                lvi.SubItems.Add(song.Title);
                lvi.SubItems.Add(song.Artist);
                lvi.SubItems.Add(song.Album);
                lvi.SubItems.Add(song.ID);
                this.Invoke(new MethodInvoker(delegate
                {
                    lvSongs.Items.Add(lvi);
                }));

                if (num >= 100)
                    break;
            }
        }

        void GetAllPlaylistsDone()
        {
            this.Invoke(new MethodInvoker(delegate
            {
                foreach (GoogleMusicPlaylist pl in api.playlistContainer.Values)
                {
                    lbPlaylists.Items.Add(pl);
                }
            }));
        }
        
        void GetAllSongsComplete()
        {
            this.Invoke(new MethodInvoker(delegate
            {
                foreach (GoogleMusicPlaylist pl in api.playlistContainer.Values)
                {
                    lbPlaylists.Items.Add(pl);
                }
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            tbRefreshToken.Text = api.RequestRefreshToken(tbCode.Text, ClientId, ClientSecret);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            api.Login(tbEmail.Text, tbPass.Text, tbRefreshToken.Text, ClientId, ClientSecret);
            this.Invoke(new MethodInvoker(delegate
            {
                this.Text += " -> Logged in";
            }));
        }

        private void btnCreatePl_Click(object sender, EventArgs e)
        {
            var resp = api.CreatePlaylist("Testing");

            if (!String.IsNullOrEmpty(resp.ID))
                MessageBox.Show("Created pl");
            else
                MessageBox.Show("Error");
        }

        private void btnFetchSongs_Click(object sender, EventArgs e)
        {
            lvSongs.Items.Clear();
            //api.GetAllSongs(String.Empty);
            api.GetAllSongs();
        }

        private void btnGetPlaylists_Click(object sender, EventArgs e)
        {
            lbPlaylists.Items.Clear();
            api.GetAllPlaylists();
        }

        private void btnSongURL_Click(object sender, EventArgs e)
        {
            String id = lvSongs.SelectedItems[0].SubItems[4].Text;
            var songurl = api.GetDownloadLink(id);
            var response = api.GetDownloadResponse(songurl);
            api.DownloadSong(response, "C:\\Windows\\Temp\\test.mp3");
            
            //new WebClient().DownloadFile(songurl, "C:\\Windows\\Temp\\test.mp3");
        }

        private void btnDeletePl_Click(object sender, EventArgs e)
        {
            String id = "";
            foreach (GoogleMusicPlaylist pl in api.playlistContainer.Values)
            {
                if(pl.Name.Equals(lbPlaylists.SelectedItem.ToString()))
                {
                    id = pl.ID;
                    break;
                }
            }

            var resp = api.DeletePlaylist(id);

            if (!String.IsNullOrEmpty(resp.ID))
            {
                MessageBox.Show("Deleted");
            }
        }

        private void btnGetPlaylistSongs_Click(object sender, EventArgs e)
        {
            lvSongs.Items.Clear();

            String id = "";
            foreach (GoogleMusicPlaylist pl in api.playlistContainer.Values)
            {
                if (pl.Name.Equals(lbPlaylists.SelectedItem.ToString()))
                {
                    id = pl.ID;
                    break;
                }
            }

            var pls2 = api.GetPlaylist(id);

            int num = 1;
            foreach (GoogleMusicPlaylistEntry entry in pls2.Songs)
            {
                GoogleMusicSong song = api.GetTrack(entry.TrackID);

                ListViewItem lvi = new ListViewItem();
                lvi.Text = (num++).ToString();
                lvi.SubItems.Add(song.Title);
                lvi.SubItems.Add(song.Artist);
                lvi.SubItems.Add(song.Album);
                lvi.SubItems.Add(song.ID);
                this.Invoke(new MethodInvoker(delegate
                {
                    lvSongs.Items.Add(lvi);
                }));

                if (num >= 100)
                    break;
            }
        }

        private void btnAddTrack_Click(object sender, EventArgs e)
        {
            String id = "";
            foreach (GoogleMusicPlaylist pl in api.playlistContainer.Values)
            {
                if (pl.Name.Equals(lbPlaylists.SelectedItem.ToString()))
                {
                    id = pl.ID;
                    break;
                }
            }

            String songId = lvSongs.SelectedItems[0].SubItems[4].Text;

            var resp = api.AddTracksToPlaylist(id, new String[] {songId});

            if (!String.IsNullOrEmpty(resp.ID))
            {
                MessageBox.Show("Added");
            }
        }

        private void btnDeleteTrack_Click(object sender, EventArgs e)
        {
            String songId = lvSongs.SelectedItems[0].SubItems[4].Text;

            var resp = api.DeleteTracks(new String[] { songId });

            if (!String.IsNullOrEmpty(resp.ID))
            {
                MessageBox.Show("Deleted");
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            var track = new Track();
            track.album = "album";
            track.artist= "artist2";
            track.client_id = "F4FDC0F2-0386-4037-88F4-0595FBA49B24";
            track.genre= "genre";
            track.original_bit_rate = 192;
            track.title = "title";
            track.track_type = Track.TrackType.LOCAL_TRACK;
            //track.id = "25bbdcd0-6c32-3477-b7fa-1c3e4a91b032";
            //track.store_id = "25bbdcd0-6c32-3477-b7fa-1c3e4a91b032";
            //track.metajam_id = "25bbdcd0-6c32-3477-b7fa-1c3e4a91b032";
            track.do_not_rematch = false;

            MessageBox.Show(api.UploadTrack(track.client_id, "C:\\Windows\\Temp\\test.mp3", track));
        }
    }
}
