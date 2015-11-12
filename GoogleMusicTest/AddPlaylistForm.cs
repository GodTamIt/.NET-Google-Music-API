using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GoogleMusicTest
{
    public partial class AddPlaylistForm : Form
    {
        public AddPlaylistForm()
        {
            InitializeComponent();
        }

        private void txtTitle_TextChanged(object sender, EventArgs e)
        {
            btnSave.Enabled = txtTitle.TextLength > 0;
        }

        public string Title
        {
            get { return txtTitle.Text; }
        }

        public string Description
        {
            get { return txtDescription.Text; }
        }
    }
}
