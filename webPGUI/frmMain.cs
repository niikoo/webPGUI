using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using SlavaGu.ConsoleAppLauncher;

namespace webPGUI
{
    public sealed partial class MainForm : Form
    {

        public MainForm()
        {
            InitializeComponent();
            AllowDrop = true;
            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;
            tabControl1.SelectedIndexChanged += TabControl1_SelectedIndexChanged;

        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        public static string[] AcceptedFileTypes = {
            ".jpg", ".jpeg", ".tif", ".png", ".gif", ".webp"
        };

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            foreach (string file in files)
            {
                var extension = Path.GetExtension(file);
                //Console.WriteLine(extension);

                if (AcceptedFileTypes.Contains(extension))
                {
                    openFileDialog1.FileName = file;
                    if (radioButton_single.Checked) {
                        textBox_input.Text = file;
                        textBox_outputfile.Text =
                            $"{Path.GetDirectoryName(openFileDialog1.FileName)}\\{Path.GetFileNameWithoutExtension(openFileDialog1.FileName)}.webp";
                    } else
                    {
                        listBox_batch.Items.Add(file);
                    }
                    
                    ProcessArgs();
                }
            }
        }

        private void TabControl1_SelectedIndexChanged(object sender, EventArgs e)
            => AllowDrop = tabControl1.SelectedIndex == 0;

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            numericUpDown_q.Value = trackBar_quality.Value;
            ProcessArgs();
        }


        private void button2_Click(object sender, EventArgs e)
        {
            // ========================= RUN COMMAND ===============================
            Cursor.Current = Cursors.WaitCursor;

            Text = "Encoding... Please wait...";
            Refresh();

            SaveSettings();

            const string textSeparator = "===================================================================== \r\n";

            Globals.Consolelog =
                $"{textSeparator}{DateTime.Now.ToString(CultureInfo.InvariantCulture)} - Start encoding... \r\n{textSeparator}\r\n";

            if (radioButton_single.Checked)
            {
                Globals.Consolelog += ConsoleApp.Run("cwebp.exe", Globals.Args).Output.Trim();
            }
            else
            {   // batch mode
                int count = listBox_batch.Items.Count;
                int current = 1;

                foreach (string file in listBox_batch.Items)
                {
                    var finalArgs = $"{Globals.Args} \"{file}\" -o \"{textBox_outputfile.Text}\\{Path.GetFileNameWithoutExtension(file)}.webp\" ";

                    Globals.Consolelog += $"File {current} of {count}\r\n";
                    Globals.Consolelog += ConsoleApp.Run("cwebp.exe", finalArgs).Output.Trim();
                    Globals.Consolelog += $"\r\n{textSeparator}\r\n";

                    Text = $"Encoding... {current} of {count} Please wait...";
                    Refresh();
                    current++;
                }
            }

            Cursor.Current = Cursors.Default;

            Text = "WebP encoding tool GUI";
            Refresh();

            var frm = new OutputForm();
            frm.ShowDialog();
        }


        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = false;
            openFileDialog1.Filter = "Image files (*.jpg, *.jpeg, *.tif, *.gif, *.png, *.webp) | *.jpg; *.jpeg; *.tif; *.gif; *.png; *.webp | JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|TIFF Files (*.tif)|*.tif|WebP Files (*.webp)|*.webp";
            openFileDialog1.FilterIndex = 1;

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            textBox_input.Text = openFileDialog1.FileName;
            textBox_outputfile.Text =
                $"{Path.GetDirectoryName(openFileDialog1.FileName)}\\{Path.GetFileNameWithoutExtension(openFileDialog1.FileName)}.webp";
            ProcessArgs();
        }

        /// <summary>
        /// Generate command line arguments
        /// </summary>
        public void ProcessArgs()
        {
            Globals.Args = "cwebp.exe"; //  cwebp [-preset <...>] [options] in_file [-o out_file]

            if (cboPreset.SelectedIndex > 0) // preset setting, one of: default, photo, picture, drawing, icon, text
                Globals.Args += $" -preset {cboPreset.Text}"; // -preset must come first, as it overwrites other parameters

            Globals.Args += $" -q {trackBar_quality.Value}"; // -q <float> quality factor (0:small..100:big)
            Globals.Args += $" -alpha_q {trackBar_alpha_q.Value}"; // -alpha_q <int> transparency-compression quality (0..100)
            Globals.Args += $" -z {trackBar_z.Value}"; // activates lossless preset with given level in [0:fast, ..., 9:slowest]
            Globals.Args += $" -m {trackBar_m.Value}"; // compression method (0=fast, 6=slowest)
            Globals.Args += $" -segments {trackBar_segments.Value}"; // number of segments to use (1..4)

            uint sizetemp = Convert.ToUInt32(numericUpDown_Size.Value); 
            if (radioLossySize.Checked && sizetemp > 0) {
                switch (comboUnitSize.SelectedIndex) {
                    case 1:
                        sizetemp *= 1024;
                        break;
                    case 2:
                        sizetemp *= 1024 * 1024;
                        break;
                }
                Globals.Args += $" -size {sizetemp}"; // target size (in bytes)
            }

            if (checkBox_psnr.Checked)
                Globals.Args += $" -psnr {numericUpDown_psnr.Value}";  // target PSNR (in dB.typically: 42)
                // Specify a target PSNR (in dB) to try and reach for the compressed output. Compressor will make several pass of partial encoding in order to get as close as possible to this target.

            // TODO -s <int> <int> ......... input size (width x height) for YUV

            Globals.Args += $" -sns {trackBar_sns.Value}"; // spatial noise shaping (0:off, 100:max)
            Globals.Args += $" -f {trackBar_f.Value}"; // deblocking filter strength (0=off..100)
            // Specify the strength of the deblocking filter, between 0 (no filtering) and 100 (maximum filtering). A value of 0 will turn off any filtering. Higher value will increase the strength of the filtering process applied after decoding the picture. The higher the value the smoother the picture will appear. Typical values are usually in the range of 20 to 50.
            Globals.Args += $" -sharpness {trackBar_sharpness.Value}"; // filter sharpness (0:most .. 7:least sharp)

            if (checkBox_strong.Checked) {
                Globals.Args += " -strong"; // use strong filter instead of simple (default)
            } else {
                Globals.Args += " -nostrong"; // use simple filter instead of strong
            }

            // Globals.args += " -partition_limit " + trackBar10.Value.ToString(); // limit quality to fit the 512k limit on the first partition(0 = no degradation... 100 = full)
            Globals.Args += $" -pass {trackBar_pass.Value}"; // analysis pass number (1..10)

            /* TODO -crop <x> <y> <w> <h> .. crop picture with the given rectangle     */

            if (checkBox_resize.Checked)
                Globals.Args += $" -resize {numericUpDown_resizeX.Value} {numericUpDown_resizeY.Value}"; // -resize < w > < h > ........resize picture(after any cropping)

            if (checkBox_mt.Checked)
                Globals.Args += " -mt"; // use multi-threading if available

            if (checkBox_low_memory.Checked)
                Globals.Args += " -low_memory"; // reduce memory usage (slower encoding)


            if (checkBox_map.Checked)
                Globals.Args += $" -map {numericUpDown_map.Value}"; // print map of extra info

            if (checkBox_print_psnr.Checked)
                Globals.Args += " -print_psnr"; // print map of extra info

            if (checkBox_print_ssim.Checked)
                Globals.Args += " -print_ssim"; // prints averaged SSIM distortion

            if (checkBox_print_lsim.Checked)
                Globals.Args += " -print_lsim"; // prints local-similarity distortion

            if (checkBox_d.Checked)
                Globals.Args += " -d file.pgm"; // dump the compressed output (PGM file)

            if (checkBox_alpha_method.Checked)
                Globals.Args += " -alpha_method 1"; // transparency-compression method (0..1)
            else
                Globals.Args += " -alpha_method 0"; // Specify the algorithm used for alpha compression. Off denotes no compression, On uses WebP lossless format for compression.

            switch (cbo_alpha_filter.SelectedIndex)
            { // predictive filtering for alpha plane, one of: none, fast(default) or best
                case 0:
                    Globals.Args += " -alpha_filter none";
                    break;
                case 1:
                    Globals.Args += " -alpha_filter fast";
                    break;
                case 2:
                    Globals.Args += " -alpha_filter best";
                    break;
            }

            if (checkBox_exact.Checked)
                Globals.Args += " -exact"; // preserve RGB values in transparent area

            if (checkBox_blend_alpha.Checked) // blend colors against background color expressed as RGB values written in hexadecimal, e.g. 0xc0e0d0 for red = 0xc0 green = 0xe0 and blue = 0xd0
                Globals.Args += $" -blend_alpha {HexConverter(colorDialog1.Color)}";

            if (checkBox_noalpha.Checked)
                Globals.Args += " -noalpha"; // discard any transparency information

            if (radioLossless.Checked)
                Globals.Args += " -lossless";// encode image losslessly

            if (trackBar_near_lossless.Value < 100)
                Globals.Args += $" -near_lossless {trackBar_near_lossless.Value}"; // use near-lossless image preprocessing(0..100 = off)

            if (cboHint.SelectedIndex>0)
                Globals.Args += $" -hint {cboHint.SelectedItem}"; // specify image characteristics hint, one of: photo, picture or graph

            /* -metadata <string> ..... comma separated list of metadata to
                           copy from the input to the output if present.
                           Valid values: all, none (default), exif, icc, xmp

                  -version ............... print version number and exit */

            if (checkBox_short.Checked)
                Globals.Args += " -short"; // condense printed message

            if (checkBox_quiet.Checked)
                Globals.Args += " -quiet"; // don't print anything

            if (checkBox_noasm.Checked)
                Globals.Args += " -noasm"; // disable all assembly optimizations

            if (checkBox_v.Checked)
                Globals.Args += " -v"; // verbose, e.g. print encoding/decoding times

            if (checkBox_progress.Checked)
                Globals.Args += " -progress"; // report encoding progres

            // ============================== experimental features =================================
            if (checkBox_jpeg_like.Checked)
                Globals.Args += " -jpeg_like"; // roughly match expected JPEG size

            if (checkBox_af.Checked)
                Globals.Args += " -af"; // auto-adjust filter strength

            // -pre <int> ............. pre-processing filter Specify some pre-processing steps. Using a value of 2 will trigger quality-dependent pseudo-random dithering during RGBA->YUVA conversion (lossy compression only).

            // ============================== output =================================
            if (radioButton_single.Checked)
            {
                if (openFileDialog1.FileName != "")
                {
                    Globals.Args += $" \"{openFileDialog1.FileName}\" "; // input
                    Globals.Args += $"-o \"{textBox_outputfile.Text}\" ";
                    button_encode.Enabled = true;
                }
                else {
                    Globals.Args += " SPECIFY A FILE NAME";
                    button_encode.Enabled = false;
                }
                textBox_console.Text = Globals.Args; // show commands on text box
            } else
            {
                textBox_console.Text = Globals.Args;

                if (folderBrowserDialog1.SelectedPath != "")
                {
                    textBox_console.Text += " \"MULTIPLE FILES\" "; // input
                    textBox_console.Text += $"-o \"{folderBrowserDialog1.SelectedPath}\" ";
                    button_encode.Enabled = true;
                }
                else {
                    textBox_console.Text += " SPECIFY A OUTPUT FOLDER";
                    button_encode.Enabled = false;
                }
            }
        }

        private void trackBar1_Scroll_1(object sender, EventArgs e)
        {
            numericUpDown_alpha_q.Value = trackBar_alpha_q.Value;
            ProcessArgs();
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            numericUpDown_z.Value = trackBar_z.Value;
            ProcessArgs();
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            numericUpDown_m.Value = trackBar_m.Value;
            ProcessArgs();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            trackBar_quality.Value = Decimal.ToInt32(numericUpDown_q.Value);
            ProcessArgs();
        }



        private void button1_Click(object sender, EventArgs e)
        {
            saveFileDialog1.Filter = "webP Image (*.webp) | *.webp";
            saveFileDialog1.FilterIndex = 1;

            if (radioButton_single.Checked)
            {
                if (saveFileDialog1.ShowDialog() != DialogResult.OK) return;
                textBox_outputfile.Text = saveFileDialog1.FileName;
                ProcessArgs();
            }
            else
            {
                if (folderBrowserDialog1.ShowDialog() != DialogResult.OK) return;
                textBox_outputfile.Text = folderBrowserDialog1.SelectedPath;
                ProcessArgs();
            }
        }


        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) => ProcessArgs();

        private void numericUpDown4_ValueChanged(object sender, EventArgs e) => ProcessArgs();

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox8_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void button5_Click(object sender, EventArgs e)
        {
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;
            label_color.BackColor = colorDialog1.Color;
            ProcessArgs();
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            btn_color.Enabled = checkBox_jpeg_like.Checked;
            ProcessArgs();
        }

        /// <summary>
        /// Convert Color to HEX value like 0xFFFFFF
        /// </summary>
        /// <param name="c">Color var type</param>
        /// <returns></returns>
        private static string HexConverter(Color c) => $"0x{c.R:X2}{c.G:X2}{c.B:X2}";

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioLossless.Checked)
            {
                label16.Text = "faster (larger file size)";
                label17.Text = "slower";
            } else
            {
                label16.Text = "lower quality (smaller file size)";
                label17.Text = "better";
            }
            ProcessArgs();
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            ProcessArgs();
        }

        private void trackBar9_Scroll(object sender, EventArgs e)
        {
            numericUpDown_sns.Value = trackBar_sns.Value;
            ProcessArgs();
        }

        private void trackBar10_Scroll(object sender, EventArgs e)
        {
            numericUpDown_partition_limit.Value = trackBar_partition_limit.Value;
            ProcessArgs();
        }

        private void trackBar7_Scroll(object sender, EventArgs e)
        {
            numericUpDown_f.Value = trackBar_f.Value;
            ProcessArgs();
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            trackBar_f.Value = decimal.ToInt32(numericUpDown_f.Value);
            ProcessArgs();
        }

        /// <summary>
        /// Read saved settings
        /// </summary>
        private void ReadSettings()
        {
            // prevent loose settings on version upgrade
            if (Properties.Settings.Default.MustUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.MustUpgrade = false;
                Properties.Settings.Default.Save();
            }

            trackBar_quality.Value = Properties.Settings.Default.q;
            cboPreset.SelectedIndex = Properties.Settings.Default.preset;
            trackBar_alpha_q.Value = Properties.Settings.Default.alpha_q;
            numericUpDown_q.Value = Properties.Settings.Default.alpha_q;

            switch (Properties.Settings.Default.lossless)
            {
                case 0: radioLossy.Checked = true; // lossy
                    break;
                case 1: radioLossless.Checked = true; // lossless
                    break;
                case 2: radioLossySize.Checked = true; // lossy size
                    break;
            }
            
            trackBar_z.Value=Properties.Settings.Default.z;
            numericUpDown_z.Value= Properties.Settings.Default.z;

            trackBar_m.Value = Properties.Settings.Default.m;
            numericUpDown_m.Value = Properties.Settings.Default.m;

            trackBar_segments.Value = Properties.Settings.Default.segments;
            numericUpDown_segments.Value = Properties.Settings.Default.segments;

            numericUpDown_Size.Value = Convert.ToDecimal(Properties.Settings.Default.size);

            numericUpDown_psnr.Value = Convert.ToDecimal(Properties.Settings.Default.psnrval);
            checkBox_psnr.Checked = Properties.Settings.Default.psnr;

            trackBar_f.Value = Properties.Settings.Default.f;
            numericUpDown_f.Value = Properties.Settings.Default.f;

            trackBar_sharpness.Value = Properties.Settings.Default.sharpness;
            numericUpDown_sharpness.Value = Properties.Settings.Default.sharpness;

            checkBox_strong.Checked = Properties.Settings.Default.strong;

            trackBar_partition_limit.Value = Properties.Settings.Default.partition_limit;
            numericUpDown_partition_limit.Value = Properties.Settings.Default.partition_limit;

            trackBar_pass.Value = Properties.Settings.Default.pass;
            numericUpDown_pass.Value = Properties.Settings.Default.pass;

            //TODOresize CROP
            checkBox_mt.Checked = Properties.Settings.Default.mt;
            checkBox_low_memory.Checked = Properties.Settings.Default.low_memory;
            numericUpDown_map.Value = Convert.ToDecimal(Properties.Settings.Default.map);
            checkBox_print_psnr.Checked = Properties.Settings.Default.print_psnr;
            checkBox_print_ssim.Checked = Properties.Settings.Default.print_ssim;
            checkBox_print_lsim.Checked = Properties.Settings.Default.print_lsim;
            //TODOdump
            checkBox_alpha_method.Checked = Properties.Settings.Default.alpha_method;
            cbo_alpha_filter.SelectedIndex = Properties.Settings.Default.alpha_filter;
            checkBox_exact.Checked = Properties.Settings.Default.exact;
            checkBox_blend_alpha.Checked = Properties.Settings.Default.blend_alpha;
            colorDialog1.Color = Properties.Settings.Default.blend_alpha_color;
            checkBox_noalpha.Checked = Properties.Settings.Default.noalpha;

            trackBar_near_lossless.Value = Properties.Settings.Default.near_lossless;
            numericUpDown_near_lossless.Value = Properties.Settings.Default.near_lossless;

            cboHint.SelectedIndex = Properties.Settings.Default.hint;
            checkBox_short.Checked = Properties.Settings.Default.shortshort;
            checkBox_quiet.Checked = Properties.Settings.Default.quiet;
            checkBox_v.Checked = Properties.Settings.Default.v;
            checkBox_progress.Checked = Properties.Settings.Default.progress;
            checkBox_jpeg_like.Checked = Properties.Settings.Default.jpeg_like;
            checkBox_af.Checked = Properties.Settings.Default.af;

        }

        /// <summary>
        /// Save Settings
        /// </summary>
        private void SaveSettings()
        {
            Properties.Settings.Default.q = trackBar_quality.Value;
            Properties.Settings.Default.preset = cboPreset.SelectedIndex;
            Properties.Settings.Default.alpha_q = trackBar_alpha_q.Value;

            if (radioLossy.Checked)
                Properties.Settings.Default.lossless = 0;
            else if (radioLossless.Checked)
                Properties.Settings.Default.lossless = 1;
            else if (radioLossySize.Checked)
                Properties.Settings.Default.lossless = 2;

            Properties.Settings.Default.z = trackBar_z.Value;

            Properties.Settings.Default.m = trackBar_m.Value;
            Properties.Settings.Default.segments = trackBar_segments.Value;
            Properties.Settings.Default.size = Convert.ToInt32(numericUpDown_Size.Value);
            Properties.Settings.Default.psnrval = Convert.ToInt32(numericUpDown_psnr.Value);
            Properties.Settings.Default.psnr = checkBox_psnr.Checked;
            Properties.Settings.Default.f = trackBar_f.Value;
            Properties.Settings.Default.sharpness = trackBar_sharpness.Value;
            Properties.Settings.Default.strong = checkBox_strong.Checked;

            Properties.Settings.Default.pass = trackBar_pass.Value;

            Properties.Settings.Default.mt = checkBox_mt.Checked;
            Properties.Settings.Default.low_memory = checkBox_low_memory.Checked;
            Properties.Settings.Default.map = Convert.ToInt32(numericUpDown_map.Value);
            Properties.Settings.Default.print_psnr = checkBox_print_psnr.Checked;
            Properties.Settings.Default.print_ssim = checkBox_print_ssim.Checked;
            Properties.Settings.Default.print_lsim = checkBox_print_lsim.Checked;

            Properties.Settings.Default.partition_limit= trackBar_partition_limit.Value;

            Properties.Settings.Default.alpha_method = checkBox_alpha_method.Checked;
            Properties.Settings.Default.alpha_filter = cbo_alpha_filter.SelectedIndex;
            Properties.Settings.Default.exact = checkBox_exact.Checked;
            Properties.Settings.Default.blend_alpha = checkBox_blend_alpha.Checked;
            Properties.Settings.Default.blend_alpha_color = colorDialog1.Color;
            Properties.Settings.Default.noalpha = checkBox_noalpha.Checked;
            Properties.Settings.Default.near_lossless = trackBar_near_lossless.Value;
            Properties.Settings.Default.hint = cboHint.SelectedIndex;
            Properties.Settings.Default.shortshort = checkBox_short.Checked;
            Properties.Settings.Default.quiet = checkBox_quiet.Checked;
            Properties.Settings.Default.v = checkBox_v.Checked;
            Properties.Settings.Default.progress = checkBox_progress.Checked;
            Properties.Settings.Default.jpeg_like = checkBox_jpeg_like.Checked;
            Properties.Settings.Default.af = checkBox_af.Checked;


            Properties.Settings.Default.Save();
        }



        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_psnr.Enabled = checkBox_psnr.Checked;
            ProcessArgs();
        }

        private void numericUpDown5_ValueChanged(object sender, EventArgs e)
        {
            ProcessArgs();
            ProcessArgs();
        }

        private void numericUpDown7_ValueChanged(object sender, EventArgs e)
        {
            trackBar_partition_limit.Value = decimal.ToInt32(numericUpDown_partition_limit.Value);
            ProcessArgs();
        }

        private void numericUpDown13_ValueChanged(object sender, EventArgs e)
        {
            trackBar_pass.Value = decimal.ToInt32(numericUpDown_pass.Value);
            ProcessArgs();
        }

        private void numericUpDown8_ValueChanged(object sender, EventArgs e)
        {
            trackBar_near_lossless.Value = decimal.ToInt32(numericUpDown_near_lossless.Value);
            ProcessArgs();
        }

        private void numericUpDown10_ValueChanged(object sender, EventArgs e)
        {
            trackBar_m.Value = decimal.ToInt32(numericUpDown_m.Value);
            ProcessArgs();
        }

        private void numericUpDown11_ValueChanged(object sender, EventArgs e)
        {
            trackBar_segments.Value = decimal.ToInt32(numericUpDown_segments.Value);
            ProcessArgs();
        }

        private void numericUpDown12_ValueChanged(object sender, EventArgs e)
        {
            trackBar_sharpness.Value = decimal.ToInt32(numericUpDown_sharpness.Value);
            ProcessArgs();
        }

        private void numericUpDown9_ValueChanged(object sender, EventArgs e)
        {
            trackBar_z.Value = decimal.ToInt32(numericUpDown_z.Value);
            ProcessArgs();
        }

        private void numericUpDown6_ValueChanged(object sender, EventArgs e)
        {
            trackBar_sns.Value = decimal.ToInt32(numericUpDown_sns.Value);
            ProcessArgs();
        }

        private void trackBar6_Scroll(object sender, EventArgs e)
        {
            numericUpDown_segments.Value = trackBar_segments.Value;
            ProcessArgs();
        }

        private void trackBar11_Scroll(object sender, EventArgs e)
        {
            numericUpDown_segments.Value = trackBar_near_lossless.Value;
            ProcessArgs();
        }

        private void trackBar4_Scroll(object sender, EventArgs e)
        {
            numericUpDown_sharpness.Value = trackBar_sharpness.Value;
            ProcessArgs();
        }

        private void trackBar5_Scroll(object sender, EventArgs e)
        {
            numericUpDown_pass.Value = trackBar_pass.Value;
            ProcessArgs();
        }

        private void numericUpDown2_ValueChanged(object sender, EventArgs e)
        {
            trackBar_alpha_q.Value = decimal.ToInt32(numericUpDown_alpha_q.Value);
            ProcessArgs();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox3_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox10_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox13_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox14_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox15_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox17_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox18_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox19_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox20_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox21_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            numericUpDown_map.Enabled = checkBox_map.Checked;
            ProcessArgs();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void checkBox2_CheckedChanged(object sender, EventArgs e) => ProcessArgs();

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e) => ProcessArgs();

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioLossySize.Checked)
            {
                numericUpDown_Size.Visible = true;
                comboUnitSize.Visible = true;
            } else
            {
                numericUpDown_Size.Visible = false;
                comboUnitSize.Visible = false;
            }
            ProcessArgs();
        }


        private void frmMain_Load(object sender, EventArgs e)
        {
            label_header.Width = 710;
            ReadSettings();
            comboUnitSize.SelectedIndex = 1;
            cboHint.SelectedIndex = 0;
            cbo_alpha_filter.SelectedIndex = 1;
        }

        private void button9_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 7;
            button_1.BackColor = Color.FromArgb(218, 218, 218);
            button_2.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(218, 218, 218);
            button_4.BackColor = Color.FromArgb(15, 157, 88);

            button_1.Enabled = true;
            button_2.Enabled = true;
            button_3.Enabled = true;
            button_4.Enabled = false;

            HideSubMenu(false);
        }

        private void button7_Click_1(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 1;
            button_1.BackColor = Color.FromArgb(218, 218, 218);
            button_2.BackColor = Color.FromArgb(15, 157, 88);
            button_3.BackColor= Color.FromArgb(218, 218, 218);
            button_4.BackColor = Color.FromArgb(218, 218, 218);

            button_1.Enabled = true;
            button_2.Enabled = false;
            button_3.Enabled = true;
            button_4.Enabled = true;

            HideSubMenu(false);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            // advanced options
            tabControl1.SelectedIndex = 2;
            button_1.BackColor = Color.FromArgb(218, 218, 218);
            button_2.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(15, 157, 88); 
            button_4.BackColor = Color.FromArgb(218, 218, 218);
            button_31.BackColor = Color.FromArgb(218, 218, 218);
            button_32.BackColor = Color.FromArgb(218, 218, 218);
            button_33.BackColor = Color.FromArgb(218, 218, 218);
            button_34.BackColor = Color.FromArgb(218, 218, 218);

            button_1.Enabled = true;
            button_2.Enabled = true;
            button_3.Enabled = false;
            button_4.Enabled = true;

            button_31.Enabled = true;
            button_32.Enabled = true;
            button_33.Enabled = true;
            button_34.Enabled = true;


            HideSubMenu(true);
        }

        private void HideSubMenu(bool menustatus)
        {
            button_31.Visible = menustatus;
            button_32.Visible = menustatus;
            button_33.Visible = menustatus;
            button_34.Visible = menustatus;
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 0;
            button_1.BackColor = Color.FromArgb(15, 157, 88);
            button_2.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(218, 218, 218);
            button_4.BackColor = Color.FromArgb(218, 218, 218);

            button_1.Enabled = false;
            button_2.Enabled = true;
            button_3.Enabled = true;
            button_4.Enabled = true;

            HideSubMenu(false);
        }


        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void button11_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 3;
            button_31.BackColor = Color.FromArgb(15, 157, 88);
            button_32.BackColor = Color.FromArgb(218, 218, 218);
            button_33.BackColor = Color.FromArgb(218, 218, 218);
            button_34.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(218, 218, 218);

            button_31.Enabled = false;
            button_32.Enabled = true;
            button_33.Enabled = true;
            button_34.Enabled = true;
            button_3.Enabled = true;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 4;
            button_31.BackColor = Color.FromArgb(218, 218, 218); 
            button_32.BackColor = Color.FromArgb(15, 157, 88);
            button_33.BackColor = Color.FromArgb(218, 218, 218);
            button_34.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(218, 218, 218);

            button_31.Enabled = true;
            button_32.Enabled = false;
            button_33.Enabled = true;
            button_34.Enabled = true;
            button_3.Enabled = true;
        }

        private void button13_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 5;
            button_31.BackColor = Color.FromArgb(218, 218, 218);
            button_32.BackColor = Color.FromArgb(218, 218, 218);
            button_33.BackColor = Color.FromArgb(15, 157, 88);
            button_34.BackColor = Color.FromArgb(218, 218, 218);
            button_3.BackColor = Color.FromArgb(218, 218, 218);

            button_31.Enabled = true;
            button_32.Enabled = true;
            button_33.Enabled = false;
            button_34.Enabled = true;
            button_3.Enabled = true;
        }

        private void button14_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedIndex = 6;
            button_31.BackColor = Color.FromArgb(218, 218, 218);
            button_32.BackColor = Color.FromArgb(218, 218, 218);
            button_33.BackColor = Color.FromArgb(218, 218, 218);
            button_34.BackColor = Color.FromArgb(15, 157, 88);
            button_3.BackColor = Color.FromArgb(218, 218, 218);

            button_31.Enabled = true;
            button_32.Enabled = true;
            button_33.Enabled = true;
            button_34.Enabled = false;
            button_3.Enabled = true;
        }


        private void button10_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textBox_console.Text);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            SaveSettings();

            AboutBoxForm frm2 = new AboutBoxForm();
            frm2.ShowDialog();
        }

        private void numericUpDown_sharpness_ValueChanged(object sender, EventArgs e)
        {
            trackBar_sharpness.Value = decimal.ToInt32(numericUpDown_sharpness.Value);
            ProcessArgs();
        }

        private void trackBar_sharpness_Scroll(object sender, EventArgs e)
        {
            numericUpDown_sharpness.Value = trackBar_sharpness.Value;
            ProcessArgs();
        }

        private void trackBar_f_Scroll(object sender, EventArgs e)
        {
            numericUpDown_f.Value = trackBar_f.Value;
            ProcessArgs();
        }

        private void numericUpDown_f_ValueChanged(object sender, EventArgs e)
        {
            trackBar_f.Value = decimal.ToInt32(numericUpDown_f.Value);
            ProcessArgs();
        }

        private void trackBar_m_Scroll(object sender, EventArgs e)
        {
            numericUpDown_m.Value = trackBar_m.Value;
            ProcessArgs();
        }

        private void numericUpDown_m_ValueChanged(object sender, EventArgs e)
        {
            trackBar_m.Value = decimal.ToInt32(numericUpDown_m.Value);    
            ProcessArgs();
        }

        private void cboPreset_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProcessArgs();
        }

        private void radioLossy_CheckedChanged(object sender, EventArgs e)
        {
            ProcessArgs();
        }

        private void numericUpDown_z_ValueChanged(object sender, EventArgs e)
        {
            trackBar_z.Value = decimal.ToInt32(numericUpDown_z.Value);
            ProcessArgs();
        }

        private void trackBar_z_Scroll(object sender, EventArgs e)
        {
            numericUpDown_z.Value = trackBar_z.Value;
            ProcessArgs();
        }

        private void trackBar_segments_Scroll(object sender, EventArgs e)
        {
            numericUpDown_segments.Value = trackBar_segments.Value;
            ProcessArgs();
        }

        private void numericUpDown_segments_ValueChanged(object sender, EventArgs e)
        {
            trackBar_segments.Value = decimal.ToInt32(numericUpDown_segments.Value);
            ProcessArgs();
        }

        private void trackBar_sns_Scroll(object sender, EventArgs e)
        {
            numericUpDown_sns.Value = trackBar_sns.Value;
            ProcessArgs();
        }

        private void numericUpDown_sns_ValueChanged(object sender, EventArgs e)
        {
            trackBar_sns.Value = decimal.ToInt32(numericUpDown_sns.Value);
            ProcessArgs();
        }

        private void trackBar_near_lossless_Scroll(object sender, EventArgs e)
        {
            numericUpDown_near_lossless.Value = trackBar_near_lossless.Value;
            ProcessArgs();
        }

        private void numericUpDown_near_lossless_ValueChanged(object sender, EventArgs e)
        {
            trackBar_near_lossless.Value = decimal.ToInt32(numericUpDown_near_lossless.Value);
            ProcessArgs();
        }

        private void trackBar_pass_Scroll(object sender, EventArgs e)
        {
            numericUpDown_pass.Value = trackBar_pass.Value;
            ProcessArgs();
        }

        private void numericUpDown_pass_ValueChanged(object sender, EventArgs e)
        {
            trackBar_pass.Value = decimal.ToInt32(numericUpDown_pass.Value);
            ProcessArgs();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            
        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            
        }

        private void button3_Click_1(object sender, EventArgs e)
        {
           
        }

        private void button_add_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = true;
            openFileDialog1.Filter = "Image files (*.jpg, *.jpeg, *.tif, *.gif, *.png, *.webp) | *.jpg; *.jpeg; *.tif; *.gif; *.png; *.webp | JPEG Files (*.jpeg)|*.jpeg|PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|GIF Files (*.gif)|*.gif|TIFF Files (*.tif)|*.tif|WebP Files (*.webp)|*.webp";
            openFileDialog1.FilterIndex = 1;

            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            foreach (var f in openFileDialog1.FileNames)
            {
                listBox_batch.Items.Add(f);

                //listView1.Items.Add (Path.GetFileName (f));
            }
        }

        private void button_remove_Click(object sender, EventArgs e)
        {
            if (listBox_batch.SelectedIndex >= 0)
            {
                listBox_batch.Items.RemoveAt(listBox_batch.SelectedIndex);
            }
        }

        private void radioButton_multiple_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton_multiple.Checked) return;
            listBox_batch.Visible = true;
            button_add.Visible = true;
            button_remove.Visible = true;
            textBox_input.Visible = false;
            button_inputfile.Visible = false;
            label2.Text = "Select multiple images";
            label29.Visible = false;
            label4.Text = "Output folder";
        }

        private void radioButton_single_CheckedChanged(object sender, EventArgs e)
        {
            if (!radioButton_single.Checked) return;
            listBox_batch.Visible = false;
            button_add.Visible = false;
            button_remove.Visible = false;
            textBox_input.Visible = true;
            button_inputfile.Visible = true;
            label2.Text = "Select your image";
            label29.Visible = true;
            label4.Text = "Output image";
        }
    }
}
