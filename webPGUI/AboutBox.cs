using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Diagnostics;

namespace webPGUI
{
    internal partial class AboutBoxForm : Form
    {
        public AboutBoxForm()
        {
            InitializeComponent();
            //this.Text = String.Format("About {0}", AssemblyTitle);
            //this.textBoxDescription.Text = AssemblyProduct;
            lblversion.Text = $"{String.Format("v.{0}", AssemblyVersion)} ALPHA";
            /*this.textBoxDescription.Text += AssemblyCopyright;
            this.textBoxDescription.Text += AssemblyCompany;
            this.textBoxDescription.Text += AssemblyDescription;*/

            LinkLabel.Link link = new LinkLabel.Link();
            link.LinkData =
                $"mailto:samuelcarreira@outlook.com?Subject=%5BWebP%20encoding%20tool%20GUI%20v{AssemblyVersion}%5D";
            linkLabel1.Links.Add(link);
            linkLabel1.LinkBehavior = LinkBehavior.NeverUnderline;
        }

        #region Assembly Attribute Accessors

        public string AssemblyVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString();

        #endregion

        private void okButton_Click(object _, EventArgs e)
        {

        }

        private void AboutBox_Load(object _, EventArgs e)
        {

        }

        private void flowLayoutPanel1_Paint(object _, PaintEventArgs e)
        {

        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(e.Link.LinkData as string ?? string.Empty);
        }

        private void textBoxDescription_TextChanged(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }
    }
}
