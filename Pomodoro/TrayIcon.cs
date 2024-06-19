using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 250;

        private bool isNormal = true;

        private readonly NotifyIcon _trayIcon;
        private readonly Timer _refreshTimer;
        private readonly ToolStripLabel _timeLabelMenuItem;

        public TrayIcon()
        {
            Application.ApplicationExit += ApplicationExitHandler;

            _trayIcon = new NotifyIcon
            {
                BalloonTipIcon = ToolTipIcon.Info,
                BalloonTipText = @"GLSoft Pomodoro",
                BalloonTipTitle = @"Pomodoro",
                Text = @"Pomodoro",
                Icon = Resources.Number00
            };

            ToolStripMenuItem closeMenuItem = new ToolStripMenuItem
            {
                Text = @"Close Pomodoro", 
            };
            closeMenuItem.Click += this.CloseMenuItemClickHandler;

            _timeLabelMenuItem = new ToolStripLabel(DateTime.Now.ToString("HH:mm:ss"))
            {
                Text = DateTime.Now.ToString("HH:mm:ss"),
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.DarkRed,
                BackColor = Color.DarkGray
            };

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip
            {
                Font = new Font(FontFamily.GenericSansSerif, 9, FontStyle.Regular),
                AutoSize = true
            };
            contextMenuStrip.Items.Add(_timeLabelMenuItem);
            contextMenuStrip.Items.Add("-");
            contextMenuStrip.Items.Add(closeMenuItem);

            _trayIcon.ContextMenuStrip = contextMenuStrip;
            _trayIcon.Visible = true;

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

        private void CloseMenuItemClickHandler(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void RefreshTimerTickHandler(object sender, EventArgs e)
        {
            if (isNormal)
            {
                _trayIcon.Icon = Resources.Number00;
            }
            else
            {
                _trayIcon.Icon = Resources.Number00blink;
            }

            isNormal = !isNormal;

            if (_timeLabelMenuItem != null && _timeLabelMenuItem.Visible)
            {
                _timeLabelMenuItem.Text = DateTime.Now.ToString("HH:mm:ss");
            }
        }
    }
}
