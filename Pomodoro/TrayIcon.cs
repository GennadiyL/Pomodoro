using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 250;
        private const int FinishedStateDuration = 10;
        private const int DefaultDuration = 25;
        private const int MinMinutes = 5;
        private const int MaxMinutes = 90;

        private AppState _appState = AppState.Idle;
        private AppState _previousAppState = AppState.None;

        private DateTime _startTime = DateTime.MinValue;
        private int _passedSeconds = -1;
        private bool _isIconNegative = false;

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
            switch (_appState)
            {
                case AppState.Idle:
                    _appState = AppState.Started;
                    _startTime = DateTime.Now;
                    _passedSeconds = -1;
                    _isIconNegative = false;
                    return;
                case AppState.Started:
                    _appState = AppState.Postponed;
                    _passedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
                    _startTime = DateTime.MinValue;
                    _isIconNegative = false;
                    return;
                case AppState.Postponed:
                    _appState = AppState.Started;
                    _startTime = DateTime.Now.AddSeconds(-_passedSeconds);
                    _passedSeconds = -1;
                    _isIconNegative = false;
                    return;
                case AppState.Finished:
                    _appState = AppState.Started;
                    _startTime = DateTime.Now;
                    _passedSeconds = -1;
                    _isIconNegative = false;
                    return;
                default:
                    throw new InvalidOperationException("Invalid Application state");
            }
        }

        private void StopMenuItemClickHandler(object sender, EventArgs e)
        {
            if (_appState == AppState.Started || _appState == AppState.Postponed || _appState == AppState.Finished)
            {
                _appState = AppState.Idle;
                _passedSeconds = -1;
                _startTime = DateTime.MinValue;
                _isIconNegative = false;
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
                HandleIdleState();
                return;
            }
            if (_appState == AppState.Started)
            {
                HandleStartedState();
                return;
            }
            if (_appState == AppState.Postponed)
            {
                HandlePostponedState();
                return;
            }
            if (_appState == AppState.Finished)
            {
                HandleFinishedState();
                return;
            }
            throw new InvalidOperationException("Invalid Application state");
        }


        private void HandleIdleState()
        {
            if (_previousAppState != AppState.Idle)
            {
                _timeLabelMenuItem.Text = @"------";
                _trayIcon.Icon = Resources.Pomodoro;
                _startMenuItem.Text = $@"Start ({_duration})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = false;

                _previousAppState = AppState.Idle;
            }
        }

        private void HandleStartedState()
        {
            if (_previousAppState != AppState.Started)
            {
                _startMenuItem.Text = $@"Pause";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;

                _previousAppState = AppState.Started;
            }

            int passedSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
            if (passedSeconds != _passedSeconds)
            {
                int currentRemainsSeconds = _duration * 60 - passedSeconds;
                int previousRemainsSeconds = _duration * 60 - _passedSeconds;

                int currentRoundedMinutes = (currentRemainsSeconds + 59) / 60;
                int previousRoundedMinutes = (previousRemainsSeconds + 59) / 60;

                _timeLabelMenuItem.Text = $@"{currentRemainsSeconds / 60:D2}:{currentRemainsSeconds % 60:D2}";

                if (currentRoundedMinutes != previousRoundedMinutes)
                {
                    _trayIcon.Icon = _icons[currentRoundedMinutes];
                }

                _passedSeconds = passedSeconds;

                if (currentRemainsSeconds <= 0)
                {
                    _appState = AppState.Finished;
                    _startTime = DateTime.Now;
                    _passedSeconds = -1;
                    _isIconNegative = false;
                }
            }
        }

        private void HandlePostponedState()
        {
            if (_previousAppState != AppState.Postponed)
            {
                int remainsSeconds = _duration * 60 - _passedSeconds;
                int roundedMinutes = (remainsSeconds + 59) / 60;

                _timeLabelMenuItem.Text = $@"{remainsSeconds / 60:D2}:{remainsSeconds % 60:D2}";
                _trayIcon.Icon = _icons[roundedMinutes];
                _startMenuItem.Text = $@"Continue ({roundedMinutes})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;

                _previousAppState = AppState.Postponed;
            }
        }

        private void HandleFinishedState()
        {
            if (_previousAppState != AppState.Finished)
            {
                _timeLabelMenuItem.Text = $@"Finished";
                _startMenuItem.Text = $@"Start ({_duration})";
                _startMenuItem.Enabled = true;
                _stopMenuItem.Text = @"Stop";
                _stopMenuItem.Enabled = true;

                _previousAppState = AppState.Finished;
            }

            _isIconNegative = !_isIconNegative;
            _trayIcon.Icon = _isIconNegative ? Resources.Number00blink : Resources.Number00;

            int passedBlinkSeconds = (int)(DateTime.Now - _startTime).TotalSeconds;
            if (passedBlinkSeconds >= FinishedStateDuration)
            {
                _appState = AppState.Idle;
                _startTime = DateTime.MinValue;
                _passedSeconds = -1;
                _isIconNegative = false;
            }
        }
    }
}
