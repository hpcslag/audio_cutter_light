using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore.Streams;

namespace AudioPlayerSample
{
    public partial class Form1 : Form
    {
        private readonly MusicPlayer _musicPlayer = new MusicPlayer();
        private bool _stopSliderUpdate;
        private readonly ObservableCollection<MMDevice> _devices = new ObservableCollection<MMDevice>();

        public string currentPlayFilename = "";
        
        public Form1()
        {
            InitializeComponent();
            components = new Container();
            components.Add(_musicPlayer);
            _musicPlayer.PlaybackStopped += (s, args) =>
            {
                //WasapiOut uses SynchronizationContext.Post to raise the event
                //There might be already a new WasapiOut-instance in the background when the async Post method brings the PlaybackStopped-Event to us.
                if(_musicPlayer.PlaybackState != PlaybackState.Stopped)
                    btnPlay.Enabled = btnStop.Enabled = btnPause.Enabled = false;
            };
        }

        private void btnOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = CodecFactory.SupportedFilesFilterEn
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    _musicPlayer.Open(openFileDialog.FileName, (MMDevice)comboBox1.SelectedItem);
                    trackbarVolume.Value = _musicPlayer.Volume;

                    btnPlay.Enabled = true;
                    btnPause.Enabled = btnStop.Enabled = false;

                    // set filename
                    this.currentPlayFilename = openFileDialog.FileName;

                    // draw waveform
                    var source = CodecFactory.Instance.GetCodec(openFileDialog.FileName);
                    selectionRangeSlider1.storedWaveForm = WaveformData.GetData(source)[0]; // get channel 0
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Could not open file: " + ex.Message);
                }
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if(_musicPlayer.PlaybackState != PlaybackState.Playing)
            {
                // first move to start pos
                trackBar1.Value = (int)((selectionRangeSlider1.SelectedMin / 100) * (trackBar1.Maximum));
                double perc = selectionRangeSlider1.SelectedMin / 100;
                TimeSpan position = TimeSpan.FromMilliseconds(_musicPlayer.Length.TotalMilliseconds * perc);
                _musicPlayer.Position = position;

                _musicPlayer.Play();
                btnPlay.Enabled = false;
                btnPause.Enabled = btnStop.Enabled = true;
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            if(_musicPlayer.PlaybackState == PlaybackState.Playing)
            {
                _musicPlayer.Pause();
                btnPause.Enabled = false;
                btnPlay.Enabled = btnStop.Enabled = true;
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if(_musicPlayer.PlaybackState != PlaybackState.Stopped)
            {
                _musicPlayer.Stop();
                btnPlay.Enabled = btnStop.Enabled = btnPause.Enabled = false;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            TimeSpan position = _musicPlayer.Position;
            TimeSpan length = _musicPlayer.Length;
            if (position > length)
                length = position;

            lblPosition.Text = String.Format(@"{0:mm\:ss} / {1:mm\:ss}", position, length);

            if (!_stopSliderUpdate &&
                length != TimeSpan.Zero && position != TimeSpan.Zero)
            {
                double perc = position.TotalMilliseconds / length.TotalMilliseconds * trackBar1.Maximum;
                trackBar1.Value = (int)perc;
            }

            timeAxis.Text = position.ToString();

            var startPercentage = (selectionRangeSlider1.SelectedMin / 100);
            var endPercentage = (selectionRangeSlider1.SelectedMax / 100);
            if(startPercentage <= 0)
            {
                startTime.Text = TimeSpan.FromMilliseconds(0).ToString();
            }
            else
            {
                startTime.Text = TimeSpan.FromMilliseconds(length.TotalMilliseconds * (selectionRangeSlider1.SelectedMin / 100)).ToString();
            }

            if (endPercentage <= 0) {
                endTime.Text = TimeSpan.FromMilliseconds(0).ToString();
            }
            else
            {
                endTime.Text = TimeSpan.FromMilliseconds(length.TotalMilliseconds * (selectionRangeSlider1.SelectedMax / 100)).ToString();
            }
            

            //Console.WriteLine(((float)trackBar1.Value / (float)trackBar1.Maximum) * 100);

            // detect time is larger then max?
            if (((float)trackBar1.Value / (float)trackBar1.Maximum) * 100 >= selectionRangeSlider1.SelectedMax) {
                btnPause_Click(null, null);
                // fixed to max selection
                trackBar1.Value = (int)((selectionRangeSlider1.SelectedMax / 100) * (trackBar1.Maximum));
            }

            // when finish
            if (((float)trackBar1.Value / (float)trackBar1.Maximum) * 100 >= 99)
            {
                _musicPlayer.Pause();
                btnPause.Enabled = false;
                btnPlay.Enabled = btnStop.Enabled = true;
                trackBar1.Value = (int)((selectionRangeSlider1.SelectedMin / 100) * (trackBar1.Maximum));
            }
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _stopSliderUpdate = true;
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                _stopSliderUpdate = false;
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            if (_stopSliderUpdate)
            {
                double perc = trackBar1.Value / (double)trackBar1.Maximum;
                TimeSpan position = TimeSpan.FromMilliseconds(_musicPlayer.Length.TotalMilliseconds * perc);
                _musicPlayer.Position = position;
            }

            // update user control for range slide
            selectionRangeSlider1.Value = ((float)trackBar1.Value / (float)trackBar1.Maximum) * 100;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            using (var mmdeviceEnumerator = new MMDeviceEnumerator())
            {
                using (
                    var mmdeviceCollection = mmdeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active))
                {
                    foreach (var device in mmdeviceCollection)
                    {
                        _devices.Add(device);
                    }
                }
            }

            comboBox1.DataSource = _devices;
            comboBox1.DisplayMember = "FriendlyName";
            comboBox1.ValueMember = "DeviceID";
        }

        private void trackbarVolume_ValueChanged(object sender, EventArgs e)
        {
            _musicPlayer.Volume = trackbarVolume.Value;
        }

        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Direction == SelectionChangedEventArgs.DirectionEnum.Min)
            {
                Console.WriteLine("Range Change");
                double perc = selectionRangeSlider1.SelectedMin / 100;
                TimeSpan position = TimeSpan.FromMilliseconds(_musicPlayer.Length.TotalMilliseconds * perc);
                _musicPlayer.Position = position;
            }
        }

        private void export_Click(object sender, EventArgs e)
        {
            if(currentPlayFilename == "")
            {
                MessageBox.Show("沒有選擇任何檔案");
                return;
            }

            var ss = TimeSpan.FromMilliseconds(_musicPlayer.Length.TotalMilliseconds * (selectionRangeSlider1.SelectedMin / 100)).TotalSeconds;
            var t = TimeSpan.FromMilliseconds(_musicPlayer.Length.TotalMilliseconds * (selectionRangeSlider1.SelectedMax / 100)).TotalSeconds - ss;
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Filter = "Common Music File (*.mp3) | *.mp3|Music File (*" + Path.GetExtension(currentPlayFilename) + ") | " + "*" + Path.GetExtension(currentPlayFilename) + "| All files(*.*) | *.*"
            };
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                switch (Path.GetExtension(currentPlayFilename)) {
                    case ".mp4":
                        Execute(@"./ffmpeg.exe", "-ss " + ss + " -t " + t + " -i \"" + currentPlayFilename + "\" -b:a 192K -vn \"" + saveFileDialog.FileName + "\"");
                        break;
                    default:
                        Execute(@"./ffmpeg.exe", "-ss " + ss + " -t " + t + " -i \"" + currentPlayFilename + "\" -acodec copy \"" + saveFileDialog.FileName + "\"");
                        break;
                }

                MessageBox.Show("完成輸出");

            }
        }

        private static string Execute(string exePath, string parameters)
        {
            string result = String.Empty;

            using (Process p = new Process())
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = exePath;
                p.StartInfo.Arguments = parameters;
                p.Start();
                p.WaitForExit();

                p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                {
                    MessageBox.Show("錯誤: " + e.ToString());
                };

                result = p.StandardOutput.ReadToEnd();
            }

            return result;
        }
    }
}
