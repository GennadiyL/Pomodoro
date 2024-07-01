using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 250;
        private const int FinishedDuration = 10;
        private const int DefaultDuration = 25;
        private const int MinMinutes = 5;
        private const int MaxMinutes = 90;

        private AppState _appState = AppState.Idle;
        private DateTime? _startTime;
        private int? _passedSeconds;
        private bool _isBlinked;

        private readonly int _duration = DefaultDuration;

        private readonly NotifyIcon _trayIcon;
        private readonly Timer _refreshTimer;

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
                if (_duration < MinMinutes || _duration > MaxMinutes)
                {
                    throw new ArgumentException($"Duration should be {MinMinutes}...{MaxMinutes} minutes.");
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

            _refreshTimer = new Timer
            {
                Interval = RefreshInterval, 
                Enabled = true
            };
            _refreshTimer.Tick += RefreshTimerTickHandler;

            Type type = typeof(Resources);
            _icons = new Icon[MaxMinutes + 1];
            for (int i = 0; i <= MaxMinutes; i++)
            {
                PropertyInfo propertyInfo = type.GetProperty($"Number{i:D2}", BindingFlags.Static | BindingFlags.NonPublic);
                Icon icon = (Icon)propertyInfo!.GetValue(null);
                _icons[i] = icon;   
            }
        }

        private void ApplicationExitHandler(object sender, EventArgs e)
        {
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
            }

            if (_refreshTimer != null)
            {
                _refreshTimer.Enabled = false;
                _refreshTimer.Dispose();
            }
        }

        private void StartMenuItemClickHandler(object sender, EventArgs e)
        {
            if (_appState == AppState.Idle)
            {
                _appState = AppState.Started;
                _startTime = DateTime.Now;
                _passedSeconds = default;
                _isBlinked = false;
                return;
            }
            if (_appState == AppState.Started)
            {
                _appState = AppState.Postponed;
                _passedSeconds = (int)(DateTime.Now - _startTime!.Value).TotalSeconds;
                _startTime = default;
                _isBlinked = false;
                return;
            }
            if (_appState == AppState.Postponed)
            {
                _appState = AppState.Started;
                _startTime = DateTime.Now.AddSeconds(-_passedSeconds!.Value);
                _passedSeconds = default;
                _isBlinked = false;
                return;
            }
            if(_appState == AppState.Finished)
            {
                _appState = AppState.Started;
                _startTime = DateTime.Now;
                _passedSeconds = default;
                _isBlinked = false;
                return;
            }

            throw new InvalidOperationException("Invalid Application state");
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
                throw new InvalidOperationException("Invalid Application state");
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
                return;
            }
            if (_appState == AppState.Started)
            {
                int passedSeconds = (int)(DateTime.Now - _startTime!.Value).TotalSeconds;
                int remainsSeconds = _duration * 60 - passedSeconds;
                int minutes = remainsSeconds / 60;
                int seconds = remainsSeconds % 60;
                int visibleMinutes = seconds == 0 ? minutes : minutes + 1;

                _timeLabelMenuItem.Text = $@"{minutes:D2}:{seconds:D2}";
                _trayIcon.Icon = _icons[visibleMinutes];
                _startMenuItem.Text = $@"Pause";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
                if (remainsSeconds <= 0)
                {
                    _appState = AppState.Finished;
                    _startTime = DateTime.Now;
                    _passedSeconds = default;
                }
                return;
            }
            if (_appState == AppState.Postponed)
            {
                int passedSeconds = _passedSeconds!.Value;
                int remainsSeconds = _duration * 60 - passedSeconds;
                int minutes = remainsSeconds / 60;
                int seconds = remainsSeconds % 60;
                int visibleMinutes = seconds == 0 ? minutes : minutes + 1;

                _timeLabelMenuItem.Text = $@"{minutes:D2}:{seconds:D2}";
                _trayIcon.Icon = _icons[visibleMinutes];
                _startMenuItem.Text = $@"Continue ({visibleMinutes})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
                return;
            }
            if (_appState == AppState.Finished)
            {
                int passedBlinkSeconds = (int)(DateTime.Now - _startTime!.Value).TotalSeconds;
                _isBlinked = !_isBlinked;

                _timeLabelMenuItem.Text = $@"Finished";
                _trayIcon.Icon = _isBlinked ? Resources.Number00blink : Resources.Number00;
                _startMenuItem.Text = $@"Start ({_duration})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;
                
                if (passedBlinkSeconds >= FinishedDuration)
                {
                    _appState = AppState.Idle;
                    _startTime = DateTime.MinValue;
                    _passedSeconds = 0;
                    _isBlinked = false;
                }
                return;
            }
            throw new InvalidOperationException("Invalid Application state");
        }
    }
}
