using AxWMPLib;
using LiveSplit.Model;
using LiveSplit.UI.Components;
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Xml;

namespace LiveSplit.Video
{
    public class VideoComponent : ControlComponent
    {
        public VideoSettings Settings { get; set; }
        public LiveSplitState State { get; set; }
        public System.Timers.Timer SynchronizeTimer { get; set; }

        public AxWindowsMediaPlayer mediaPlayer { get; set; }

        protected string OldMRL { get; set; }

        public override string ComponentName => "Video";

        public override float HorizontalWidth => Settings.Width;

        public override float MinimumHeight => 10;

        public override float VerticalHeight => Settings.Height;

        public override float MinimumWidth => 10;

        public bool Initialized { get; set; }

        public VideoComponent(LiveSplitState state)
            : this(state, CreateVLCControl())
        { }

        public VideoComponent(LiveSplitState state, AxWindowsMediaPlayer vlc)
            : base(state, vlc, ex => ErrorCallback(state.Form, ex))
        {
            Settings = new VideoSettings();
            State = state;
            mediaPlayer = vlc;
            state.OnReset += state_OnReset;
            state.OnStart += state_OnStart;
            state.OnPause += state_OnPause;
            state.OnResume += state_OnResume;
        }

        static void ErrorCallback(Form form, Exception ex)
        {
            MessageBox.Show(form, "Something went wrong loading the Video Component.", "Video Component Could Not Be Loaded", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void state_OnResume(object sender, EventArgs e)
        {
            InvokeIfNeeded(() =>
            {
                lock (mediaPlayer)
                {
                    mediaPlayer.Ctlcontrols.play();
                }
            });
        }

        void state_OnPause(object sender, EventArgs e)
        {
            InvokeIfNeeded(() =>
            {
                lock (mediaPlayer)
                {
                    mediaPlayer.Ctlcontrols.pause();
                }
            });
        }

        void state_OnStart(object sender, EventArgs e)
        {
            InvokeIfNeeded(() =>
            {
                lock (mediaPlayer)
                {
                    mediaPlayer.Ctlcontrols.play();
                    if (activated)
                        Control.Visible = true;
                }
            });
            Synchronize();
        }

        public void Synchronize()
        {
            Synchronize(TimeSpan.Zero);
        }

        private TimeSpan GetCurrentTime()
        {
            return State.CurrentTime[TimingMethod.RealTime].Value;
        }

        public void Synchronize(TimeSpan offset)
        {
            if (SynchronizeTimer != null && SynchronizeTimer.Enabled)
                SynchronizeTimer.Enabled = false;
            InvokeIfNeeded(() =>
            {
                lock (mediaPlayer)
                    mediaPlayer.Ctlcontrols.currentPosition = (GetCurrentTime() + offset + Settings.Offset).TotalSeconds;
            });

            SynchronizeTimer.Enabled = true;
        }

        void state_OnReset(object sender, TimerPhase e)
        {
            InvokeIfNeeded(() =>
            {
                lock (mediaPlayer)
                {
                    mediaPlayer.Ctlcontrols.stop();
                    if (activated)
                        Control.Visible = false;
                }
            });
        }

        private static AxWindowsMediaPlayer CreateVLCControl()
        {
            var vlc = new AxWindowsMediaPlayer();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ComponentHostForm));
            ((System.ComponentModel.ISupportInitialize)(vlc)).BeginInit();
            vlc.Enabled = true;
            vlc.Name = "vlc";
            vlc.OcxState = ((AxHost.State)(resources.GetObject("AxWindowsMediaPlayer.OcxState")));
            ((System.ComponentModel.ISupportInitialize)(vlc)).EndInit();

            return vlc;
        }

        public override Control GetSettingsControl(UI.LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public override XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public override void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        private void DisposeIfError()
        {
            if (ErrorWithControl && !mediaPlayer.IsDisposed)
            {
                Dispose();
                throw new Exception();
            }
        }

        public override void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            base.DrawVertical(g, state, width, clipRegion);
            DisposeIfError();
        }

        public override void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            base.DrawHorizontal(g, state, height, clipRegion);
            DisposeIfError();
        }

        public override void Update(UI.IInvalidator invalidator, LiveSplitState state, float width, float height, UI.LayoutMode mode)
        {
            if (!mediaPlayer.IsDisposed)
            {
                base.Update(invalidator, state, width, height, mode);
                mediaPlayer.uiMode = "none";

                if (!Initialized)
                {
                    Control.Visible = !Control.Created;
                    Initialized = Control.Created;
                }
                else
                {
                    if (mediaPlayer != null && OldMRL != Settings.MRL && !string.IsNullOrEmpty(Settings.MRL))
                    {
                        InvokeIfNeeded(() =>
                        {
                            lock (mediaPlayer)
                            {
                                mediaPlayer.URL = Settings.MRL;
                            }
                        });
                    }
                    OldMRL = Settings.MRL;

                    if (mediaPlayer != null)
                    {
                        InvokeIfNeeded(() =>
                        {
                            lock (mediaPlayer)
                            {
                                mediaPlayer.settings.mute = true;
                            }
                        });
                    }
                }
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            State.OnReset -= state_OnReset;
            State.OnStart -= state_OnStart;
            State.OnPause -= state_OnPause;
            State.OnResume -= state_OnResume;
            if (SynchronizeTimer != null)
                SynchronizeTimer.Dispose();
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
