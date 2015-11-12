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
using System.Drawing;

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

        private Dictionary<Guid, Song> songs_dict;
        private Dictionary<Guid, Playlist> playlists_dict;

        public GoogleTest()
        {
            InitializeComponent();

            client = new GoogleMusic.Clients.WebClient();

            songs_dict = new Dictionary<Guid, Song>();
            playlists_dict = new Dictionary<Guid, Playlist>();

            songs_data = CreateSongsDataTable();
            playlists_data = new List<DataTable>();

            playlists_table = new DataTable();
            playlists_table.Columns.Add("Title", Type.GetType("System.String"));
            playlists_table.Columns.Add("Description", Type.GetType("System.String"));
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

            spltTop.Panel2.Enabled = false;

            await GetAll();

            // Setup UI
            tvSidebar.ExpandAll();
        }

        private async void btnRefresh_Click(object sender, EventArgs e)
        {
            await GetAll();
        }

        private void tvSidebar_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null)
            {
                tvSidebar.SelectedNode = tvSidebar.Nodes[0];
                return;
            }

            DataTable source = null;
            bool isPlaylistSelected = false;

            if (e.Node.Parent == null)
            {
                switch (e.Node.Index)
                {
                    case 0:
                        source = songs_data;
                        break;
                    case 1:
                        source = playlists_table;
                        isPlaylistSelected = true;
                        break;
                }
            }
            else
            {
                source = playlists_data[e.Node.Index];
            }

            btnAdd.Enabled = isPlaylistSelected;
            btnAddTo.Enabled = !isPlaylistSelected;
            
            dgvData.DataSource = source;
            lblCount.Text = source.Rows.Count.ToString();
        }

        private async void btnAddTo_Click(object sender, EventArgs e)
        {
            if (dgvData.SelectedRows.Count < 1)
                return;

            ToolStripItem tsSender = (ToolStripItem)sender;

            Guid[] ids = new Guid[dgvData.SelectedRows.Count];
            for (int i = 0; i < dgvData.SelectedRows.Count; i++)
                ids[i] = (Guid)dgvData.SelectedRows[i].Cells["ID"].Value;

            await AddToPlaylist((Guid)tsSender.Tag, ids);
        }

        private async void dgvData_UserDeletingRow(object sender, DataGridViewRowCancelEventArgs e)
        {
            e.Cancel = await DeleteRows(new DataGridViewRow[] { e.Row }, false);
        }

        private async void btnRemove_Click(object sender, EventArgs e)
        {
            if (dgvData.SelectedRows.Count < 1)
                return;

            DataGridViewRow[] arr = new DataGridViewRow[dgvData.SelectedRows.Count];
            dgvData.SelectedRows.CopyTo(arr, 0);
            await DeleteRows(arr, true);
        }

        #endregion

        #region Sidebar Dragging

        private Rectangle dragBoxFromMouseDown;
        private int rowIndexFromMouseDown;
        private TreeNode lastHighlightedNode = null;

        private void dgvData_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                // If the mouse moves outside the rectangle, start the drag.
                if (dragBoxFromMouseDown != Rectangle.Empty && !dragBoxFromMouseDown.Contains(e.X, e.Y))
                {
                    // Proceed with the drag and drop, passing in the list item.                    
                    DragDropEffects dropEffect = dgvData.DoDragDrop(dgvData.Rows[rowIndexFromMouseDown].Cells["ID"].Value, DragDropEffects.Copy);
                }
            }
        }

        private void dgvData_MouseDown(object sender, MouseEventArgs e)
        {
            // Get the index of the item the mouse is below.
            rowIndexFromMouseDown = dgvData.HitTest(e.X, e.Y).RowIndex;
            if (rowIndexFromMouseDown != -1 && tvSidebar.SelectedNode != null && tvSidebar.SelectedNode != tvSidebar.Nodes[1])
            {
                // Remember the point where the mouse down occurred. 
                // The DragSize indicates the size that the mouse can move 
                // before a drag event should be started.                
                Size dragSize = SystemInformation.DragSize;

                // Create a rectangle using the DragSize, with the mouse position being
                // at the center of the rectangle.
                dragBoxFromMouseDown = new Rectangle(new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)), dragSize);
            }
            else
                // Reset the rectangle if the mouse is not over an item in the ListBox.
                dragBoxFromMouseDown = Rectangle.Empty;
        }

        private void tvSidebar_DragOver(object sender, DragEventArgs e)
        {
            Point p = tvSidebar.PointToClient(new Point(e.X, e.Y));
            TreeNode node = tvSidebar.GetNodeAt(p.X, p.Y);

            if (node == null)
                return;

            if (lastHighlightedNode != null && lastHighlightedNode != node)
                lastHighlightedNode.BackColor = Color.White;

            if (node.Parent != tvSidebar.Nodes[1])
            {
                lastHighlightedNode = null;
                e.Effect = DragDropEffects.None;
            }
            else
            {
                lastHighlightedNode = node;
                e.Effect = DragDropEffects.Copy;
                node.BackColor = Color.FromArgb(239, 108, 0);
            }
        }

        private async void tvSidebar_DragDrop(object sender, DragEventArgs e)
        {
            Point p = tvSidebar.PointToClient(new Point(e.X, e.Y));
            TreeNode node = tvSidebar.GetNodeAt(p.X, p.Y);

            if (node == null || node.Tag == null)
                return;

            Playlist playlist = playlists_dict[(Guid) node.Tag];

            await AddToPlaylist((Guid)node.Tag, new Guid[] { (Guid)e.Data.GetData("System.Guid") });
            
            if (lastHighlightedNode != null)
            {
                lastHighlightedNode.BackColor = Color.White;
                lastHighlightedNode = null;
            }
        }

        private void tvSidebar_DragLeave(object sender, EventArgs e)
        {
            if (lastHighlightedNode != null)
            {
                lastHighlightedNode.BackColor = Color.White;
                lastHighlightedNode = null;
            }
        }

        #endregion

        #region Functions

        private async Task GetAll()
        {
            // Disable form
            spltMain.Enabled = false;

            // ** Library **
            lblStatus.Text = "Retrieving songs...";
            songs_dict.Clear();
            await client.GetAllSongs(songs_dict);
            Task updateSongs = Task.Run(() => UpdateSongs());

            // ** Playlists **
            lblStatus.Text = "Retrieving playlists...";

            // Clear everything
            playlists_dict.Clear();
            tvSidebar.Nodes[1].Nodes.Clear();
            btnAddTo.DropDownItems.Clear();

            await client.GetUserPlaylists(playlists_dict);
            {
                int i = 0;
                Task[] tasks = new Task[playlists_dict.Values.Count];
                foreach (Playlist p in playlists_dict.Values)
                    tasks[i++] = client.GetPlaylistSongs(p);

                for (i = 0; i < playlists_dict.Values.Count; i++)
                    await tasks[i];
            }
            Task updatePlaylists = Task.Run(() => UpdatePlaylists());

            // Await the updating
            await updateSongs;
            await updatePlaylists;

            // Enable form again
            spltMain.Enabled = true;
            tvSidebar.SelectedNode = tvSidebar.Nodes[0];
            lblStatus.Text = "Idle...";
        }

        private void UpdateSongs()
        {
            songs_data.Rows.Clear();
            foreach (Song s in songs_dict.Values)
                songs_data.Rows.Add(s.Title, s.Artist, s.Album, s.Genre, s.Year, s.ID, "-");
        }

        private void UpdatePlaylists()
        {
            playlists_table.Rows.Clear();
            playlists_data.Clear();

            foreach (Playlist p in playlists_dict.Values)
            {
                // Add row in playlists table
                playlists_table.Rows.Add(p.Title, p.Description, p.ID, p.LastModifiedTimestamp, p.CreationTimestamp);

                // Add songs to the playlist's song table
                DataTable songs = CreateSongsDataTable();
                foreach (Song s in p.Songs)
                    songs.Rows.Add(s.Title, s.Artist, s.Album, s.Genre, s.Year, s.ID, s.PlaylistEntryId);

                playlists_data.Add(songs);

                // Add to TreeView
                if (tvSidebar.InvokeRequired)
                    tvSidebar.Invoke(new MethodInvoker(() =>
                        {
                            TreeNode node = tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3);
                            node.Tag = p.ID;
                        }));
                else
                {
                    TreeNode node = tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3);
                    node.Tag = p.ID;
                }

                // Add to btnAddTo
                btnAddTo_AddEntry(p);
            }
        }

        private async Task CreatePlaylist(string title, string description)
        {
            var result = await client.CreatePlaylist(title, description);
            if (!result.Success)
            {
                MessageBox.Show("Failed to add new playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            Playlist p = result.Value;

            playlists_dict.Add(p.ID, p);

            // Add row in playlists table
            playlists_table.Rows.Add(p.Title, p.Description, p.ID, p.LastModifiedTimestamp, p.CreationTimestamp);

            // Add songs to the playlist's song table
            DataTable songs = CreateSongsDataTable();
            foreach (Song s in p.Songs)
                songs.Rows.Add(s.Title, s.Artist, s.Album, s.Genre, s.Year, s.ID, s.PlaylistEntryId);

            playlists_data.Add(songs);

            // Add to TreeView
            if (tvSidebar.InvokeRequired)
                tvSidebar.Invoke(new MethodInvoker(() =>
                {
                    TreeNode node = tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3);
                    node.Tag = p.ID;
                }));
            else
            {
                TreeNode node = tvSidebar.Nodes[1].Nodes.Add(p.Title, p.Title, 1, 3);
                node.Tag = p.ID;
            }

            // Add to btnAddTo
            btnAddTo_AddEntry(p);
        }

        private async Task<bool> DeletePlaylist(Guid playlistID)
        {
            Playlist playlist;
            try
            {
                playlist = playlists_dict[playlistID];
            }
            catch (Exception)
            {
                MessageBox.Show("A local error occurred while removing from the playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            var result = await client.DeletePlaylist(playlist);

            if (!result.Success)
            {
                MessageBox.Show("Failed to remove songs from playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            playlists_dict.Remove(playlistID);

            var dataSource = dgvData.DataSource;
            dgvData.DataSource = null;
            dgvData.Refresh();

            tvSidebar.Nodes[1].Nodes.Clear();
            btnAddTo.DropDownItems.Clear();
            await Task.Run(() => UpdatePlaylists());

            dgvData.DataSource = dataSource;

            tvSidebar.SelectedNode = tvSidebar.Nodes[1];

            return true;
        }

        private async Task AddToPlaylist(Guid playlistID, Guid[] songIDs)
        {
            Playlist playlist;
            Song[] songs;
            try
            {
                playlist = playlists_dict[playlistID];
                songs = new Song[songIDs.Length];
                for (int i = 0; i < songIDs.Length; i++)
                {
                    songs[i] = songs_dict[songIDs[i]];
                }
            }
            catch (Exception)
            {
                MessageBox.Show("A local error occurred while adding to the playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            var result = await client.AddToPlaylist(playlists_dict[playlistID], songs);

            if (!result.Success)
            {
                MessageBox.Show("Failed to add songs to playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }

            for (int i = 0; i < songs.Length; i++)
            {
                Song cloned = (Song)songs[i].Clone();
                cloned.PlaylistEntryId = result.Value[cloned.ID].ToString();
                playlist.Songs.Add(cloned);
            }

            tvSidebar.Nodes[1].Nodes.Clear();
            btnAddTo.DropDownItems.Clear();
            UpdatePlaylists();
        }

        private async Task<bool> DeleteFromPlaylist(Guid playlistID, Guid[] songIDs)
        {
            Playlist playlist;
            Song[] songs;
            try
            {
                playlist = playlists_dict[playlistID];
                songs = new Song[songIDs.Length];
                for (int i = 0; i < songIDs.Length; i++)
                {
                    songs[i] = playlist.Songs.Find((s) => s.PlaylistEntryId == songIDs[i].ToString());
                }
            }
            catch (Exception)
            {
                MessageBox.Show("A local error occurred while removing from the playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            var result = await client.DeleteFromPlaylist(playlist, songs);

            if (!result.Success)
            {
                MessageBox.Show("Failed to remove songs from playlist!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            foreach (Song song in songs)
                playlist.Songs.Remove(song);

            return true;
        }

        private async Task<bool> DeleteFromLibrary(Guid[] songIDs)
        {
            Song[] songs;
            try
            {
                songs = new Song[songIDs.Length];
                for (int i = 0; i < songIDs.Length; i++)
                {
                    songs[i] = songs_dict[songIDs[i]];
                }
            }
            catch (Exception)
            {
                MessageBox.Show("A local error occurred while deleting the song(s)!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            var result = await client.DeleteSongs(songs);

            if (!result.Success)
            {
                MessageBox.Show("Failed to delete the song(s)!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return false;
            }

            foreach (Guid id in songIDs)
                songs_dict.Remove(id);

            foreach (Playlist p in playlists_dict.Values)
            {
                foreach (Song song in songs)
                {
                    p.Songs.RemoveAll((s) => s == song);
                }
            }

            var dataSource = dgvData.DataSource;
            dgvData.DataSource = null;
            dgvData.Refresh();

            tvSidebar.Nodes[1].Nodes.Clear();
            btnAddTo.DropDownItems.Clear();
            await Task.Run(() => UpdateSongs());
            await Task.Run(() => UpdatePlaylists());

            dgvData.DataSource = dataSource;

            return true;
        }

        #endregion

        #region Helpers

        private void btnAddTo_AddEntry(Playlist playlist)
        {
            // Add to btnAddTo
            if (btnAddTo.GetCurrentParent().InvokeRequired)
                btnAddTo.GetCurrentParent().Invoke(new MethodInvoker(() => btnAddTo_AddEntry(playlist)));
            else
            {
                ToolStripItem item = btnAddTo.DropDownItems.Add(playlist.Title, GoogleMusicTest.Properties.Resources.Playlist);
                item.Tag = playlist.ID;
                item.ImageScaling = ToolStripItemImageScaling.None;
                item.Click += btnAddTo_Click;
            }
        }

        private DataTable CreateSongsDataTable()
        {
            DataTable result = new DataTable();
            result.Columns.Add("Title", Type.GetType("System.String"));
            result.Columns.Add("Artist", Type.GetType("System.String"));
            result.Columns.Add("Album", Type.GetType("System.String"));
            result.Columns.Add("Genre", Type.GetType("System.String"));
            result.Columns.Add("Year", Type.GetType("System.Int32"));
            result.Columns.Add("ID", Type.GetType("System.Guid"));
            result.Columns.Add("Entry ID", Type.GetType("System.String"));
            return result;
        }

        private async Task<bool> DeleteRows(IList<DataGridViewRow> rows, bool delete)
        {
            bool success = true;
            Guid[] ids = new Guid[rows.Count];
            for (int i = 0; i < rows.Count; i++)
                ids[i] = Guid.Parse(rows[i].Cells[tvSidebar.SelectedNode.Parent != null ? "Entry ID" : "ID"].Value.ToString());
            
            
            if (tvSidebar.SelectedNode == tvSidebar.Nodes[0])
            {
                // Library delete
                success = await DeleteFromLibrary(ids);
            }
            else if (tvSidebar.SelectedNode == tvSidebar.Nodes[1])
            {
                // Delete playlist
                foreach (Guid id in ids)
                {
                    success = success & await DeletePlaylist(id);
                }
            }
            else
            {
                success = await DeleteFromPlaylist((Guid)tvSidebar.SelectedNode.Tag, ids);
                if (success && delete)
                {
                    foreach (var row in rows)
                    {
                        dgvData.Rows.Remove(row);
                    }
                }
            }

            return success;
        }

        #endregion

        private async void btnAdd_Click(object sender, EventArgs e)
        {
            AddPlaylistForm frmAddPlaylist = new AddPlaylistForm();
            if (frmAddPlaylist.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return;

            await CreatePlaylist(frmAddPlaylist.Title, frmAddPlaylist.Description);
        }











    }
}
