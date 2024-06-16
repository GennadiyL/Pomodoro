using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Timer = System.Windows.Forms.Timer;

namespace Pomodoro
{
    public class TrayIcon : ApplicationContext
    {
        private const int RefreshInterval = 10000;

        private readonly NotifyIcon _trayIcon;
        private readonly Timer _refreshTimer;

        public TrayIcon()
        {
            Application.ApplicationExit += ApplicationExitHandler;

            ToolStripMenuItem closeMenuItem = new ToolStripMenuItem { Text = @"Close App" };
            closeMenuItem.Click += this.CloseMenuItemClickHandler;

            _trayIcon = new NotifyIcon();
            _trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            _trayIcon.BalloonTipText = @"GLSoft Pomodoro";
            _trayIcon.BalloonTipTitle = @"Pomodoro";
            _trayIcon.Text = @"Pomodoro";
            _trayIcon.Icon = Resources.Pomodoro;
            _trayIcon.ContextMenuStrip = new ContextMenuStrip();
            _trayIcon.ContextMenuStrip.Items.Add(closeMenuItem);
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
        }
    }
}
