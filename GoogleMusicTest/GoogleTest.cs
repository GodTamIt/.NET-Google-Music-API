using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;
using System.Diagnostics;

using GoogleMusic;

namespace GoogleMusicTest
{
    public partial class GoogleTest : Form
    {

        string ClientId = "215459815136.apps.googleusercontent.com";
        string ClientSecret = "";
        API api;

        public GoogleTest()
        {
            InitializeComponent();

            //api = new API(ClientId, ClientSecret);

            //Process.Start(api.MusicManager.GetAuthorizationCodeUrl());

            //api.DeviceFriendlyName = "test uploader";
            //api.DeviceId = "mm:00:16:E6:88:04:57";
        }

        void GetAllSongsDone()
        {
        }

        void GetAllPlaylistsDone()
        {

        }
        
        void GetAllSongsComplete()
        {

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var client = new GoogleMusic.Clients.WebClient();
            var login = await client.Login(tbEmail.Text, tbPass.Text);

            Stopwatch watch = new Stopwatch();
            //watch.Start();
            var allsongs = await client.GetAllSongs();
            //watch.Stop();

            List<Song> toDelete = new List<Song>();
            int i = 0;
            foreach (var pair in allsongs.Value)
            {
                toDelete.Add(pair.Value);
                if (++i > 3) break;
            }

            var delete = await client.DeleteSongs(toDelete);

            //var playlists = await client.GetUserPlaylists();

            //var load = await client.GetPlaylistSongs(playlists.Value.First().Value);

            return;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            MessageBox.Show((await api.Login(tbEmail.Text, tbPass.Text, tbCode.Text)).ToString());
        }

        private void btnCreatePl_Click(object sender, EventArgs e)
        {
        }

        private void btnFetchSongs_Click(object sender, EventArgs e)
        {
        }

        private void btnGetPlaylists_Click(object sender, EventArgs e)
        {
        }

        private void btnSongURL_Click(object sender, EventArgs e)
        {
        }

        private void btnDeletePl_Click(object sender, EventArgs e)
        {
        }

        private void btnGetPlaylistSongs_Click(object sender, EventArgs e)
        {
        }

        private void btnAddTrack_Click(object sender, EventArgs e)
        {
        }

        private void btnDeleteTrack_Click(object sender, EventArgs e)
        {
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
        }
    }
}
