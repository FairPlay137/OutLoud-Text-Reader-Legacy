using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OutLoud_Text_Reader
{
    public partial class AboutForm : Form
    {
        private int creditsIndex = 0;
        private int creditsIndexMax = 4;
        private bool fadeIn = true;
        private byte brightness = 255;
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {
            label1.Text = $"Version {typeof(OutLoud_Text_Reader.MainForm).Assembly.GetName().Version}";
#if DEBUG
            label2.Visible = true;
#endif
            creditsLabel1.Text = CreditsText(creditsIndex);
            linkLabel1.Links.Add(0,6, "https://github.com/FairPlay137/OutLoud-Text-Reader");
        }
        private string CreditsText(int index)
        {
            switch (index)
            {
                case 0:
                    return "Programmed by FairPlay137-TTS";
                case 1:
                    return "Design inspired by CFS-Technologies' Speakonia";
                case 2:
                    return "This software uses Microsoft Speech API (SAPI) 5.4";
                case 3:
                    return "This software uses .NET Framework 4.5";
                case 4:
                    return "Icons generated with Iconion 2.7";
                default:
                    return $"Index out of range ({index})";
            }
        }

        private void scrollTimer_Tick(object sender, EventArgs e)
        {
            fadeIn = false;
            tickTimer.Enabled = true;
            scrollTimer.Enabled = false;
        }

        private void tickTimer_Tick(object sender, EventArgs e)
        {
            creditsLabel1.ForeColor = Color.FromArgb(brightness,brightness,brightness);
            if (fadeIn)
            {
                creditsLabel1.Padding = new Padding(0, brightness / 24, 0, 0);
                if (brightness > 0)
                {
                    brightness--;
                    if (brightness > 0) brightness--;
                    if (brightness > 0) brightness--;
                    if (brightness > 0) brightness--;
                }
                else
                {
                    tickTimer.Enabled = false;
                    scrollTimer.Enabled = true;
                }
            }
            else
            {
                creditsLabel1.Padding = new Padding(0, 0, 0, brightness / 24);
                if (brightness < 255)
                {
                    brightness++;
                    if (brightness < 255) brightness++;
                    if (brightness < 255) brightness++;
                    if (brightness < 255) brightness++;
                }
                else
                {
                    creditsIndex++;
                    if (creditsIndex > creditsIndexMax) creditsIndex = 0;
                    creditsLabel1.Text = CreditsText(creditsIndex);
                    fadeIn = true;
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            
        }
    }
}
