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

        private string ClientId = "215459815136.apps.googleusercontent.com";
        private string ClientSecret = "";
        private API api;
        private GoogleMusic.Clients.WebClient client;

        private DataTable songs_data;
        private List<DataTable> playlists_data;
        private DataTable playlists_table;

        private Dictionary<Guid, Song> songs;
        private Dictionary<Guid, Playlist> playlists;

        public GoogleTest()
        {
            InitializeComponent();

            client = new GoogleMusic.Clients.WebClient();

            songs = new Dictionary<Guid, Song>();
            playlists = new Dictionary<Guid, Playlist>();

            songs_data = CreateSongsDataTable();
            playlists_data = new List<DataTable>();

            playlists_table = new DataTable();
            playlists_table.Columns.Add("Title", Type.GetType("System.String"));
            playlists_table.Columns.Add("Description", Type.GetType("System.String"));
            playlists_table.Columns.Add("Owner", Type.GetType("System.String"));
            playlists_table.Columns.Add("ID", Type.GetType("System.Guid"));
            playlists_table.Columns.Add("Last Modified", Type.GetType("System.DateTime"));
            playlists_table.Columns.Add("Created", Type.GetType("System.DateTime"));

            // Focus on textbox
            this.ActiveControl = txtAccount;

            
            //api = new API(ClientId, ClientSecret);

            //Process.Start(api.MusicManager.GetAuthorizationCodeUrl());

            //api.DeviceFriendlyName = "test uploader";
            //api.DeviceId = "mm:00:16:E6:88:04:57";
        }

        private async void button1_Click(object sender, EventArgs e)
        {
             
            //var login = await client.Login(tbEmail.Text, tbPass.Text);

            Stopwatch watch = new Stopwatch();
            //watch.Start();
            var allsongs = await client.GetAllSongs();
            //watch.Stop();

            List<Song> toDelete = new List<Song>();
            int i = 0;
            foreach (var pair in allsongs.Value)
            {
                toDelete.Add(pair.Value);
                if (++i >= 100) break;
            }

            //var createPlaylist = await client.CreatePlaylist("Distinct playlist right here?", "I have a dream", toDelete);

            var playlists = await client.GetUserPlaylists();

            var load = await client.GetPlaylistSongs(playlists.Value.First().Value);

            return;
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

        #region Events

        private void btnLogin_Enabler(object sender, EventArgs e)
        {
            btnLogin.Enabled = txtAccount.TextLength > 0 && txtPass.TextLength > 0;
        }

        private async void btnLogin_Click(object sender, EventArgs e)
        {
            lblStatus.Text = "Logging in...";
            var login = await client.Login(txtAccount.Text, txtPass.Text);

            if (!login.Success)
            {
                lblStatus.Text = "Login failed!";
                MessageBox.Show("Unable to log in! Please check your credentials and try again.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                lblStatus.Text = "Idle...";
                return;
            }

            await GetAll();

            // Setup UI
            spltMain.Enabled = true;
            tvSidebar.SelectedNode = tvSidebar.Nodes[0];
            tvSidebar.ExpandAll();

            lblStatus.Text = "Idle...";
        }

        private void tvSidebar_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
            {
                tvSidebar.SelectedNode = tvSidebar.Nodes[0];
                return;
            }

            DataTable source = null;
            if (e.Node.Parent == null)
            {
                switch (e.Node.Index)
                {
                    case 0:
                        source = songs_data;
                        break;
                    case 1:
                        source = playlists_table;
                        break;
                }
                btnDeletePlaylist.Enabled = false;
            }
            else
            {
                source = playlists_data[e.Node.Index];
                btnDeletePlaylist.Enabled = true;
            }

            dgvData.DataSource = source;
            lblCount.Text = source.Rows.Count.ToString();
        }

        #endregion



        private async Task GetAll()
        {
            // Get songs
            lblStatus.Text = "Retrieving songs...";
            songs.Clear();
            await client.GetAllSongs(songs);
            Task updateSongs = Task.Run(() => UpdateSongs());

            // Get playlists
            lblStatus.Text = "Retrieving playlists...";
            playlists.Clear();
            await client.GetUserPlaylists(playlists);

            {
                int i = 0;
                Task[] tasks = new Task[playlists.Values.Count];
                foreach (Playlist p in playlists.Values)
                    tasks[i++] = client.GetPlaylistSongs(p);

                for (i = 0; i < playlists.Values.Count; i++)
                    await tasks[i];
            }
            Task updatePlaylists = Task.Run(() => UpdatePlaylists());

            // Await the updating
            await updateSongs;
            await updatePlaylists;
        }

        private void UpdateSongs()
        {
            songs_data.Clear();
            foreach (Song s in songs.Values)
                songs_data.Rows.Add(s.Title, s.Artist, s.Album, s.Genre);
        }

        private void UpdatePlaylists()
        {
            playlists_table.Clear();
            playlists_data.Clear();

            foreach (Playlist p in playlists.Values)
            {
                // Add row in playlists table
                playlists_table.Rows.Add(p.Title, p.Description, p.OwnerName, p.ID, p.LastModifiedTimestamp, p.CreationTimestamp);

                // Add songs to the playlist's song table
                DataTable songs = CreateSongsDataTable();
                foreach (Song s in p.Songs)
                    songs.Rows.Add(s.Title, s.Artist, s.Album, s.Genre);

                playlists_data.Add(songs);

                // Add to TreeView
                if (tvSidebar.InvokeRequired)
                    tvSidebar.Invoke(new MethodInvoker(() => tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3)));
                else
                    tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3);
                
            }
        }

        private DataTable CreateSongsDataTable()
        {
            DataTable result = new DataTable();
            result.Columns.Add("Title", Type.GetType("System.String"));
            result.Columns.Add("Artist", Type.GetType("System.String"));
            result.Columns.Add("Album", Type.GetType("System.String"));
            result.Columns.Add("Genre", Type.GetType("System.String"));

            return result;
        }

        private void FillSongDataRow(DataRow row, Song song)
        {
            row.ItemArray[0] = song.Title;
            row.ItemArray[1] = song.Artist;
            row.ItemArray[2] = song.Album;
            row.ItemArray[3] = song.Genre;
        }



    }
}
