﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SlavaGu.ConsoleAppLauncher;


namespace webPGUI
{
    public partial class OutputForm : Form
    {
        public OutputForm()
        {
            InitializeComponent();
        }

        private void frmOutput_Load(object sender, EventArgs e)
        {
            textBox2.Text = Globals.Consolelog;
        }
    }
}
