using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Threading.Tasks;
using GoogleMusic;

namespace GoogleMusicTest
{
    public partial class GoogleTest : Form
    {

        string ClientId = "215459815136.apps.googleusercontent.com";
        string ClientSecret = "tJ1gxPFfqt6FFvMqXm29QE6T";
        API api;

        public GoogleTest()
        {
            InitializeComponent();

            api = new API(ClientId, ClientSecret);

            Process.Start(api.MusicManager.GetAuthorizationCodeUrl());

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

        private void button1_Click(object sender, EventArgs e)
        {
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            MessageBox.Show((await api.LoginAsync(tbEmail.Text, tbPass.Text, tbCode.Text)).ToString());
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
