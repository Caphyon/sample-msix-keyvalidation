using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DemoApp
{
    public partial class LicenseForm : Form
    {
        public string productKey { get;private set; }
        public LicenseForm()
        {
            InitializeComponent();
        }

        private void btnActivate_Click(object sender, EventArgs e)
        {
            productKey=textLicenseKey.Text;
            this.DialogResult=DialogResult.OK;
            this.Close();
        }
    }
}
