using System.Configuration;
using System.Reflection;
using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 250;
        private const int FinishedDuration = 10;
        private const int DefaultDuration = 25;
        private const int MaxMinutes = 90;

        private DateTime _startTime = DateTime.MinValue;
        private int _passedSeconds = 0;
        private AppState _appState = AppState.Idle;
        private bool _isBlinked = false;
        
        private readonly NotifyIcon _trayIcon;
        private readonly Timer _refreshTimer;
        private readonly int _duration = DefaultDuration;

        private readonly ToolStripLabel _timeLabelMenuItem;
        private readonly ToolStripMenuItem _startMenuItem;
        private readonly ToolStripMenuItem _stopMenuItem;

        private readonly Icon[] _icons;

        public TrayIcon()
        {
            Application.ApplicationExit += ApplicationExitHandler;

            try
            {
                string durationString = ConfigurationManager.AppSettings["duration"];
                _duration = int.Parse(durationString!);
                if (_duration < 5 || _duration > 90)
                {
                    throw new ArgumentException("Duration should be 5...90 minutes.");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, $@"Configuration error. Set duration to {DefaultDuration} minutes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _duration = DefaultDuration;
            }

            _timeLabelMenuItem = new ToolStripLabel
            {
                Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkRed,
                BackColor = Color.DarkGray
            };

            ToolStripSeparator separator1 = new ToolStripSeparator();

            _startMenuItem = new ToolStripMenuItem();
            _startMenuItem.Click += StartMenuItemClickHandler;

            _stopMenuItem = new ToolStripMenuItem();
            _stopMenuItem.Click += StopMenuItemClickHandler;

            ToolStripSeparator separator2 = new ToolStripSeparator();

            ToolStripMenuItem closeMenuItem = new ToolStripMenuItem
            {
                Text = @"Close",
            };
            closeMenuItem.Click += CloseMenuItemClickHandler;

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip
            {
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular),
                AutoSize = true
            };
            contextMenuStrip.Items.Add(_timeLabelMenuItem);
            contextMenuStrip.Items.Add(separator1);
            contextMenuStrip.Items.Add(_startMenuItem);
            contextMenuStrip.Items.Add(_stopMenuItem);
            contextMenuStrip.Items.Add(separator2);
            contextMenuStrip.Items.Add(closeMenuItem);

            _trayIcon = new NotifyIcon
            {
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = @"GLSoft Pomodoro",
                BalloonTipTitle = @"Pomodoro",
                Text = @"Pomodoro",
                Icon = Resources.Pomodoro,
                ContextMenuStrip = contextMenuStrip,
                Visible = true
            };

            _refreshTimer = new Timer { Interval = RefreshInterval, Enabled = true };
            _refreshTimer.Tick += RefreshTimerTickHandler;

            Type type = typeof(Resources);
            _icons = new Icon[MaxMinutes + 1];
            for (int i = 0; i <= MaxMinutes; i++)
            {
                PropertyInfo propertyInfo = type.GetProperty($"Number{i:D2}", BindingFlags.Static | BindingFlags.NonPublic);
                Icon icon = (Icon)propertyInfo.GetValue(null);
                _icons[i] = icon;   
            }
        }

        private void ApplicationExitHandler(object sender, EventArgs e)
        {
            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            _refreshTimer.Enabled = false;
            _refreshTimer.Dispose();
        }

        private void StartMenuItemClickHandler(object sender, EventArgs e)
        {
            if (_appState == AppState.Idle)
            {
                _appState = AppState.Started;
                _startTime = DateTime.Now;
                _passedSeconds = 0;
            }
            else if (_appState == AppState.Started)
            {
                _appState = AppState.Postponed;
                _startTime = DateTime.MinValue;
                _passedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
            }
            else if (_appState == AppState.Postponed)
            {
                _appState = AppState.Started;
                _startTime = DateTime.Now.AddSeconds(-_passedSeconds);
                _passedSeconds = 0;
            }
            else
            {
                throw new InvalidOperationException($"Critical error. State is {_appState}");
            }
        }

        private void StopMenuItemClickHandler(object sender, EventArgs e)
        {
            if (_appState == AppState.Started || _appState == AppState.Postponed || _appState == AppState.Finished)
            {
                _appState = AppState.Idle;
                _passedSeconds = 0;
                _startTime = DateTime.MinValue;
                _isBlinked = false;
            }
            else
            {
                throw new InvalidOperationException($"Critical error. State is {_appState}");
            }
        }

        private void CloseMenuItemClickHandler(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RefreshTimerTickHandler(object sender, EventArgs e)
        {
            if (_appState == AppState.Idle)
            {
                _timeLabelMenuItem.Text = @"------";
                _trayIcon.Icon = Resources.Pomodoro;
                _startMenuItem.Text = $@"Start ({_duration})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = false;
            }
            else if (_appState == AppState.Started)
            {
                _passedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
                int remainsSeconds = _duration * 60 - _passedSeconds;
                remainsSeconds =  remainsSeconds < 0 ? 0 : remainsSeconds;
                int minutes = remainsSeconds / 60;
                int seconds = remainsSeconds % 60;

                _timeLabelMenuItem.Text = $@"{minutes:D2}:{seconds:D2}";
                _trayIcon.Icon = _icons[seconds == 0 ? minutes : minutes + 1];
                _startMenuItem.Text = $@"Pause";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
                if (remainsSeconds == 0)
                {
                    _appState = AppState.Finished;
                    _startTime = DateTime.Now;
                    _passedSeconds = 0;
                }
            }
            else if (_appState == AppState.Postponed)
            {
                int remainsSeconds = _duration * 60 - _passedSeconds;
                remainsSeconds = remainsSeconds < 0 ? 0 : remainsSeconds;
                int minutes = remainsSeconds / 60;
                int seconds = remainsSeconds % 60;

                _timeLabelMenuItem.Text = $@"{minutes:D2}:{seconds:D2}";
                _trayIcon.Icon = _icons[seconds == 0 ? minutes : minutes + 1];
                _startMenuItem.Text = $@"Continue {(remainsSeconds - 1) / 60 + 1}";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
            }
            else if (_appState == AppState.Finished)
            {
                _passedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
                _timeLabelMenuItem.Text = $@"Finished";
                _trayIcon.Icon = _isBlinked ? Resources.Number00blink : Resources.Number00;
                _startMenuItem.Text = $@"Start ({_duration})";
                _startMenuItem.Enabled = false;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
                _isBlinked = !_isBlinked;
                if (FinishedDuration >= _passedSeconds)
                {
                    _appState = AppState.Idle;
                    _startTime = DateTime.MinValue;
                    _passedSeconds = 0;
                    _isBlinked = false;
                }
            }
        }
    }
}
