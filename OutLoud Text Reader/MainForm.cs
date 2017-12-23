using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Speech.Synthesis;
using System.IO;

namespace OutLoud_Text_Reader
{
    public partial class MainForm : Form
    {
        private SpeechSynthesizer sapi5engine = new SpeechSynthesizer();
        private String[] VoiceNames;
        private bool changesMade = false;
        private Int64 lengthOfText;
        private Int64 offset;
        private bool ReadingFromMainTextBox = false;
        private string fileName = "";

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            sapi5engine.SpeakStarted += SpeakStart;
            sapi5engine.SpeakCompleted += SpeakComplete;
            sapi5engine.SpeakProgress += SpeakProgression;
            sapi5engine.SetOutputToDefaultAudioDevice();
            pasteButton.Enabled = Clipboard.ContainsText();
            undoToolStripMenuItem.Enabled = MainTextBox.CanUndo;
            redoToolStripMenuItem.Enabled = MainTextBox.CanRedo;
            pasteToolStripMenuItem.Enabled = pasteButton.Enabled;
            VoiceNames = new String[sapi5engine.GetInstalledVoices().Count];
            int a = 0;
            foreach(InstalledVoice v in sapi5engine.GetInstalledVoices())
            {
                VoiceInfo vi = v.VoiceInfo;
                VoiceNames[a] = vi.Name;
                a++;
            }
            comboBox2.DataSource = VoiceNames;
            try
            {
                highlightTextAsItsBeingSpokenToolStripMenuItem.Checked = Properties.Settings.Default.RealtimeHighlightText;
                stopSpeakingWhenTextClickedToolStripMenuItem.Checked = Properties.Settings.Default.StopSpeakOnClick;
                enableClipboardReadingToolStripMenuItem.Checked = Properties.Settings.Default.EnableClipboardReading;
                hideOnCloseButtonToolStripMenuItem.Checked = Properties.Settings.Default.HideOnCloseButton;
                MainTextBox.Font = Properties.Settings.Default.TextFont;
                XMLEnableButton.Checked = Properties.Settings.Default.EnableSSML;
            }
            catch (Exception f)
            {
                MessageBox.Show($"Failed to load the settings! (Technical info: {f.Message}) The default settings will be used.","OutLoud Text Reader",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
        }
        private void SpeakStart(object sender, SpeakStartedEventArgs e)
        {
            playButton.Enabled = false;
            pauseButton.Enabled = true;
            stopButton.Enabled = true;
            playToolStripMenuItem.Enabled = false;
            pauseToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = true;
            ToolbarIcon.Text = "OutLoud Text Speaker - Speaking";
        }
        private void SpeakComplete(object sender, SpeakCompletedEventArgs e)
        {
            playButton.Enabled = true;
            pauseButton.Enabled = false;
            stopButton.Enabled = false;
            playToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            if (ReadingFromMainTextBox && highlightTextAsItsBeingSpokenToolStripMenuItem.Checked)
            {
                MainTextBox.SelectionLength = 0;
                MainTextBox.SelectionStart = MainTextBox.Text.Length;
            }
            if (e.Error != null)
            {
                if(e.Error.GetType().ToString() != "System.OperationCanceledException") {
                    MessageBox.Show($"Speech error: {e.Error.Message}", "OutLoud Text Reader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            toolStripProgressBar1.Value = 0;
            ToolbarIcon.Text = "OutLoud Text Speaker - Idle";
        }

        private void PitchSlider_Scroll(object sender, EventArgs e)
        {
            //pitchValueLabel.Text = PitchSlider.Value.ToString(); //Perhaps, once I can get a way to interact directly with the SAPI 5 DLLs...
            switch (PitchSlider.Value)
            {
                case -2:
                    pitchValueLabel.Text = "Very low";
                    break;
                case -1:
                    pitchValueLabel.Text = "Low";
                    break;
                case 0:
                    pitchValueLabel.Text = "Normal";
                    break;
                case 1:
                    pitchValueLabel.Text = "High";
                    break;
                case 2:
                    pitchValueLabel.Text = "Very high";
                    break;
                default:
                    pitchValueLabel.Text = "undefined";
                    break;
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            propertiesToolStripMenuItem.Checked = settingsButton.Checked;
            splitContainer1.Panel2Collapsed = !settingsButton.Checked;
        }

        private void PanelCloseButton_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel2Collapsed = true;
            settingsButton.Checked = false;
        }

        private void SpeedSlider_Scroll(object sender, EventArgs e)
        {
            speedValueLabel.Text = speedSlider.Value.ToString();
        }

        private void VolumeSlider_Scroll(object sender, EventArgs e)
        {
            volumeValueLabel.Text = volumeSlider.Value.ToString();
        }

        private void TestButton_Click(object sender, EventArgs e)
        {
            SpeechSynthesizer ts = new SpeechSynthesizer();
            ts.SelectVoice(comboBox2.Text);
            ts.Volume = volumeSlider.Value;
            ts.Rate = speedSlider.Value;
            if(ts.Voice.Name == "Microsoft Sam")
                ts.SpeakAsync("You have selected Microsoft Sam as the computer's default unused feature. Blah blah. Yo dude! That job can be boring! I can do more than that. So give me some music. E flat please.");
            else
                ts.SpeakAsync($"Test 1 2 3; This is {ts.Voice.Name}.");
        }

        private void ApplyButton_Click(object sender, EventArgs e)
        {
            sapi5engine.SpeakAsyncCancelAll();
            sapi5engine.SelectVoice(comboBox2.Text);
            sapi5engine.Volume = volumeSlider.Value;
            sapi5engine.Rate = speedSlider.Value;
            lengthOfText = 5;
            ReadingFromMainTextBox = false;
            offset = 0;
            sapi5engine.SpeakAsync("Okay.");
        }

        private void PlayButton_Click(object sender, EventArgs e)
        {
            sapi5engine.SpeakAsyncCancelAll();
            sapi5engine.Resume();
            PromptBuilder pb = new PromptBuilder();
            if (XMLEnableButton.Checked)
            {
                if(MainTextBox.Text.Length == 0)
                {
                    MessageBox.Show("Nothing to speak!");
                    return;
                }
                pb.AppendSsmlMarkup(MainTextBox.Text);
            }
            else
            {
                pb.AppendText(MainTextBox.Text);
            }
            offset = MainTextBox.Text.Length - pb.ToXml().Length + 8; //TODO: Find a better way to calculate the offset, if possible
            lengthOfText = MainTextBox.Text.Length;
            //MessageBox.Show($"Debug: Offset = {offset}; Length = {lengthOfText}"); //That was for debugging the offset functionality.
            ReadingFromMainTextBox = true;
            sapi5engine.SpeakAsync(pb);
        }

        private void PauseButton_Click(object sender, EventArgs e)
        {
            if (sapi5engine.State == SynthesizerState.Paused)
            {
                sapi5engine.Resume();
                playButton.Enabled = false;
                playToolStripMenuItem.Enabled = false;
            }
            else
            {
                sapi5engine.Pause();
                playButton.Enabled = true;
                playToolStripMenuItem.Enabled = true;
            }
        }

        private void StopButton_Click(object sender, EventArgs e)
        {
            sapi5engine.SpeakAsyncCancelAll();
            sapi5engine.Resume();
            playButton.Enabled = true;
            pauseButton.Enabled = false;
            stopButton.Enabled = false;
            playToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            if (ReadingFromMainTextBox && highlightTextAsItsBeingSpokenToolStripMenuItem.Checked)
            {
                MainTextBox.SelectionLength = 0;
                MainTextBox.SelectionStart = MainTextBox.Text.Length;
            }
            toolStripProgressBar1.Value = 0;
        }

        private void NotifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ToolbarIcon.Visible = false;
            Show();
        }

        private void WAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ExportForm a = new ExportForm(MainTextBox.Text, sapi5engine.Voice, XMLEnableButton.Checked, sapi5engine.Rate, sapi5engine.Volume);
            a.ShowDialog();
        }
        private void SpeakProgression(object sender, SpeakProgressEventArgs e)
        {
            //toolStripStatusLabel1.Text = $"Char {e.CharacterPosition + offset + 1}"; //Debug stuff.
            try
            {
                if (ReadingFromMainTextBox && highlightTextAsItsBeingSpokenToolStripMenuItem.Checked)
                {
                    MainTextBox.SelectionStart = (int)(e.CharacterPosition + offset);
                    MainTextBox.SelectionLength = e.CharacterCount;
                }
            }
            catch (Exception)
            {
                toolStripStatusLabel1.Text = $"Internal error highlighting text - Char {MainTextBox.SelectionStart}";
            }
            try
            {
                toolStripProgressBar1.Maximum = (int)(lengthOfText);
                toolStripProgressBar1.Value = (int)(e.CharacterPosition + e.CharacterCount + offset);
                ToolbarIcon.Text = $"OutLoud Text Speaker - Speaking"; //TODO: Make this show the percentage
            }
            catch (Exception)
            {
                ToolbarIcon.Text = "OutLoud Text Speaker - Speaking";
                toolStripProgressBar1.Value = 0;
            }
        }

        private void NewFileButton_Click(object sender, EventArgs e)
        {
            sapi5engine.SpeakAsyncCancelAll();
            sapi5engine.Resume();
            playButton.Enabled = true;
            pauseButton.Enabled = false;
            stopButton.Enabled = false;
            playToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            if (changesMade)
            {
                DialogResult r = MessageBox.Show("Changes were made. Do you wish to save your changes before starting a new file?", "OutLoud Text Reader", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Cancel)
                {
                    if(r == DialogResult.Yes)
                    {
                        AttemptSave(false);
                    }
                }
                else
                {
                    return;
                }
            }
            MainTextBox.Clear();
            MainTextBox.ClearUndo();
            changesMade = false;
            fileName = "";
        }

        private void MainTextBox_TextChanged(object sender, EventArgs e)
        {
            changesMade = true;
            toolStripStatusLabel1.Text = $"Char {MainTextBox.SelectionStart + MainTextBox.SelectionLength + 1}";
            pasteButton.Enabled = Clipboard.ContainsText();
            undoToolStripMenuItem.Enabled = MainTextBox.CanUndo;
            redoToolStripMenuItem.Enabled = MainTextBox.CanRedo;
            pasteToolStripMenuItem.Enabled = pasteButton.Enabled;
        }

        private void XMLEnableButton_Click(object sender, EventArgs e)
        {
            if (XMLEnableButton.Checked)
            {
                DialogResult result = MessageBox.Show("Hey, this feature is EXPERIMENTAL. Don't expect all TTS voices to be fully compliant with SSML. Do you wish to activate this experimental feature?", "OutLoud Text Reader", MessageBoxButtons.YesNo, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
                if (result == DialogResult.No)
                {
                    XMLEnableButton.Checked = false;
                }
            }
        }

        private void AboutVoiceButton_Click(object sender, EventArgs e)
        {
            VoiceInfo currentVoice = sapi5engine.Voice;
            string infoboxtext = $"Voice info:\nName: \"{currentVoice.Name}\"\n{currentVoice.Age} {currentVoice.Gender}\nLanguage: {currentVoice.Culture}";
            MessageBox.Show(infoboxtext,"About Current Voice (though not really)",MessageBoxButtons.OK,MessageBoxIcon.Information);
            //TODO: Make this show the about box of the current engine instead of just popping up a "voice info" dialog box.
        }

        private void LexiconButton_Click(object sender, EventArgs e)
        {
            // TODO: Make a separate form for lexicon, since I have no way of popping up the engine's own Lexicon window
        }

        private void RefreshButton_Click(object sender, EventArgs e)
        {
            VoiceNames = new String[sapi5engine.GetInstalledVoices().Count];
            int a = 0;
            foreach (InstalledVoice v in sapi5engine.GetInstalledVoices())
            {
                VoiceInfo vi = v.VoiceInfo;
                VoiceNames[a] = vi.Name;
                a++;
            }
        }

        private void MainTextBox_SelectionChanged(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"Char {MainTextBox.SelectionStart+MainTextBox.SelectionLength+1}";
            pasteButton.Enabled = Clipboard.ContainsText();
            undoToolStripMenuItem.Enabled = MainTextBox.CanUndo;
            redoToolStripMenuItem.Enabled = MainTextBox.CanRedo;
            pasteToolStripMenuItem.Enabled = pasteButton.Enabled;
        }

        private void OpenButton_Click(object sender, EventArgs e)
        {
            sapi5engine.SpeakAsyncCancelAll();
            sapi5engine.Resume();
            playButton.Enabled = true;
            pauseButton.Enabled = false;
            stopButton.Enabled = false;
            playToolStripMenuItem.Enabled = true;
            pauseToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = false;
            if (changesMade)
            {
                DialogResult r = MessageBox.Show("Changes were made. Do you wish to save your changes before opening another file?", "OutLoud Text Reader", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Cancel)
                {
                    if (r == DialogResult.Yes)
                    {
                        AttemptSave(false);
                    }
                }
                else
                {
                    return;
                }
            }
            if(openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    MainTextBox.Clear();
                    
                    fileName = openFileDialog1.FileName;
                    MainTextBox.Text = File.ReadAllText(fileName);
                    MainTextBox.ClearUndo();
                    changesMade = false;
                }
                catch (FileLoadException x)
                {
                    MessageBox.Show($"There was an error loading the file: {x.Message}","OutLoud Text Reader",MessageBoxButtons.OK,MessageBoxIcon.Error);
                    MainTextBox.Clear();
                    MainTextBox.ClearUndo();
                    changesMade = false;
                    return;
                }
                catch (Exception x)
                {
                    MessageBox.Show($"PLEASE REPORT THIS PROBLEM!\nException message: {x.Message}\nSource: {x.Source}\n--Stack Trace--\n{x.StackTrace}", "OutLoud Text Reader", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    MainTextBox.Clear();
                    MainTextBox.ClearUndo();
                    changesMade = false;
                }
            }
            
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            String result = AttemptSave(false);
            if (result != null)
            {
                MessageBox.Show($"Error while saving. ({result})", "OutLoud Text Reader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private String AttemptSave(bool SaveAs)
        {
            try
            {
                if((fileName == "")||SaveAs)
                    if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                    {
                        fileName = saveFileDialog1.FileName;
                    }
                    else
                    {
                        return null;
                    }
                File.WriteAllText(fileName,MainTextBox.Text);
                changesMade = false;
                return null;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        private void PropertiesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            settingsButton.Checked = propertiesToolStripMenuItem.Checked;
            splitContainer1.Panel2Collapsed = !settingsButton.Checked;
        }

        private void AboutOutLoudTextReaderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutForm a = new AboutForm();
            a.ShowDialog();
        }

        private void CutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.Cut();
        }

        private void CopyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.Copy();
        }

        private void PasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.Paste();
        }

        private void CutButton_Click(object sender, EventArgs e)
        {
            MainTextBox.Cut();
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            MainTextBox.Copy();
        }

        private void PasteButton_Click(object sender, EventArgs e)
        {
            MainTextBox.Paste();
            MainTextBox.Font = MainTextBox.Font;
        }

        private void SelectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.SelectAll();
        }

        private void UndoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.Undo();
        }

        private void RedoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTextBox.Redo();
        }

        private void FontButton_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = MainTextBox.Font;
            if(fontDialog1.ShowDialog() == DialogResult.OK)
            {
                MainTextBox.Font = fontDialog1.Font;
            }
        }

        private void HideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolbarIcon.Visible = true;
            Hide();
            ToolbarIcon.BalloonTipIcon = ToolTipIcon.Info;
            ToolbarIcon.BalloonTipTitle = "OutLoud is still running!";
            ToolbarIcon.BalloonTipText = "Double-click the OutLoud icon in the toolbar to re-show the main window!";
            ToolbarIcon.ShowBalloonTip(3000);
        }

        private void ShowOutLoudWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ToolbarIcon.Visible = false;
            Show();
        }

        private void QuitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void TellCurrentTimeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string tospeak = $"It is now {now.ToShortTimeString()}";
            lengthOfText = tospeak.Length;
            offset = 0;
            sapi5engine.SpeakAsync(tospeak);

        }

        private void TellCurrentDateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            string tospeak = $"Today is {now.ToLongDateString()}";
            lengthOfText = tospeak.Length;
            offset = 0;
            sapi5engine.SpeakAsync(tospeak);
        }

        private void ChangeFontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = MainTextBox.Font;
            if (fontDialog1.ShowDialog() == DialogResult.OK)
            {
                MainTextBox.Font = fontDialog1.Font;
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            String result = AttemptSave(true);
            if (result != null)
            {
                MessageBox.Show($"Error while saving. ({result})", "OutLoud Text Reader", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SpeakClipboardContentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                lengthOfText = Clipboard.GetText().Length;
                ReadingFromMainTextBox = false;
                offset = 0;
                sapi5engine.SpeakAsync(Clipboard.GetText());
            }
            else
            {
                MessageBox.Show("There's no text on the clipboard.");
            }
        }

        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (changesMade)
            {
                DialogResult r = MessageBox.Show("Changes were made. Do you wish to save your changes before exiting?", "OutLoud Text Reader", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (r != DialogResult.Cancel)
                {
                    if (r == DialogResult.Yes)
                    {
                        AttemptSave(false);
                    }
                }
                else
                {
                    e.Cancel = true;
                    return;
                }
            }
            Properties.Settings.Default.RealtimeHighlightText = highlightTextAsItsBeingSpokenToolStripMenuItem.Checked;
            Properties.Settings.Default.StopSpeakOnClick = stopSpeakingWhenTextClickedToolStripMenuItem.Checked;
            Properties.Settings.Default.EnableClipboardReading = enableClipboardReadingToolStripMenuItem.Checked;
            Properties.Settings.Default.HideOnCloseButton = hideOnCloseButtonToolStripMenuItem.Checked;
            Properties.Settings.Default.TextFont = MainTextBox.Font;
            Properties.Settings.Default.EnableSSML = XMLEnableButton.Checked;
            Properties.Settings.Default.Save();
        }

        private void MainTextBox_Click(object sender, EventArgs e)
        {
            if((ReadingFromMainTextBox && highlightTextAsItsBeingSpokenToolStripMenuItem.Checked) && stopSpeakingWhenTextClickedToolStripMenuItem.Checked)
            {
                if(sapi5engine.State == SynthesizerState.Speaking)
                {
                    sapi5engine.SpeakAsyncCancelAll();
                    sapi5engine.Resume();
                    playButton.Enabled = true;
                    pauseButton.Enabled = false;
                    stopButton.Enabled = false;
                    playToolStripMenuItem.Enabled = true;
                    pauseToolStripMenuItem.Enabled = false;
                    stopToolStripMenuItem.Enabled = false;
                }
            }
        }

        private void HighlightTextAsItsBeingSpokenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopSpeakingWhenTextClickedToolStripMenuItem.Enabled = highlightTextAsItsBeingSpokenToolStripMenuItem.Checked;
        }
    }
}
