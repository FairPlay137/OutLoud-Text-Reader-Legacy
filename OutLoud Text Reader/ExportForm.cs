using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace OutLoud_Text_Reader
{
    public partial class ExportForm : Form
    {
        private SpeechSynthesizer sapi5engine = new SpeechSynthesizer();
        private string TextToRead;
        private bool XMLMarkupEnabled;
        public ExportForm(string inputtext, VoiceInfo voice, bool useXMLMarkup, int speed, int volume)
        {
            InitializeComponent();
            sapi5engine.SpeakStarted += SpeakStart;
            sapi5engine.SpeakCompleted += SpeakComplete;
            sapi5engine.SpeakProgress += SpeakProgression;
            TextToRead = inputtext;
            XMLMarkupEnabled = useXMLMarkup;
            sapi5engine.SelectVoice(voice.Name);
            sapi5engine.Rate = speed;
            sapi5engine.Volume = volume;
        }
        private void SpeakStart(object sender, SpeakStartedEventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            pictureBox1.Visible = true;
            label1.Text = $"Status: Exporting...";
            progressBar1.Value = 0;
        }
        private void SpeakComplete(object sender, SpeakCompletedEventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = true;
            progressBar1.Maximum = TextToRead.Length;
            if (e.Error != null)
            {
                label1.Text = $"Status: Error! ({e.Error.Message})";
                progressBar1.Value = 0;
            }
            else
            {
                label1.Text = $"Status: Complete!";
                progressBar1.Value = TextToRead.Length;
            }
            pictureBox1.Visible = false;
        }
        private void SpeakProgression(object sender, SpeakProgressEventArgs e)
        {
            progressBar1.Maximum = TextToRead.Length;
            progressBar1.Value = e.CharacterPosition + e.CharacterCount;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool DoWrite = true;
            
            if (DoWrite)
            {
                sapi5engine.SetOutputToWaveFile(textBox1.Text);
                sapi5engine.SpeakAsync(TextToRead);
            }
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            sapi5engine.Dispose();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
            textBox1.Text = saveFileDialog1.FileName;
        }
    }
}
