using System.Configuration;
using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 250;
        private const int DefaultDuration = 25;

        private bool _isNormal = true;
        private DateTime? _startTime = default;
        private int _duration = 25;

        private readonly NotifyIcon _trayIcon;
        private readonly Timer _refreshTimer;
        private readonly ToolStripLabel _timeLabelMenuItem;

        public TrayIcon()
        {
            Application.ApplicationExit += ApplicationExitHandler;

            try
            {
                string durationString = ConfigurationManager.AppSettings["duration"];
                _duration = int.Parse(durationString!);
                if (_duration < 5 || _duration > 180)
                {
                    throw new ArgumentException("Duration should be 5...180 minutes.");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Configuration error. Set duration to 25 minutes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _duration = 25;
            }

            _timeLabelMenuItem = new ToolStripLabel(DateTime.Now.ToString("HH:mm:ss"))
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font(FontFamily.GenericSansSerif, 11, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkRed,
                BackColor = Color.DarkGray
            };

            ToolStripSeparator separator1 = new ToolStripSeparator();

            ToolStripMenuItem startMenuItem = new ToolStripMenuItem
            {
                Text = _startTime.HasValue ? @"Restart" : @"New"
            };
            startMenuItem.Click += StartMenuItemClickHandler;

            ToolStripMenuItem stopMenuItem = new ToolStripMenuItem
            {
                Text = @"Stop"
            };
            stopMenuItem.Click += StopMenuItemClickHandler;

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
            contextMenuStrip.Items.Add(startMenuItem);
            contextMenuStrip.Items.Add(stopMenuItem);
            contextMenuStrip.Items.Add(separator2);
            contextMenuStrip.Items.Add(closeMenuItem);

            _trayIcon = new NotifyIcon
            {
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = @"GLSoft Pomodoro",
                BalloonTipTitle = @"Pomodoro",
                Text = @"Pomodoro",
                Icon = Resources.Number00,
                ContextMenuStrip = contextMenuStrip,
                Visible = true
            };

            _refreshTimer = new Timer { Interval = RefreshInterval, Enabled = true };
            _refreshTimer.Tick += RefreshTimerTickHandler;
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
            throw new NotImplementedException();
        }

        private void StopMenuItemClickHandler(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void CloseMenuItemClickHandler(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RefreshTimerTickHandler(object sender, EventArgs e)
        {
            if (_isNormal)
            {
                _trayIcon.Icon = Resources.Number00;
            }
            else
            {
                _trayIcon.Icon = Resources.Number00blink;
            }

            _isNormal = !_isNormal;

            if (_timeLabelMenuItem != null && _timeLabelMenuItem.Visible)
            {
                _timeLabelMenuItem.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}
