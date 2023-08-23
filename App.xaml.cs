using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Application = System.Windows.Application;

namespace CrystalDock
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public const int GWL_EX_STYLE = -20;
        public const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

        public static NotifyIcon TaskBarIcon = new NotifyIcon();
        public static Settings? settings;
        private MainWindow? DockWindow;
        private SystemMonitor? SystemMonitor;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitializeSettings();
            InitializeTaskBarIcon();

            // Create and show the main window
            DockWindow = new MainWindow();
            if (CrystalDock.Properties.Settings.Default.DebugMode)
            {
                DockWindow.BorderBrush = new SolidColorBrush(Colors.Fuchsia);
                DockWindow.BorderThickness = new Thickness(1);
            }
            DockWindow.Show();

            // Create and show another window
            SystemMonitor = new SystemMonitor();
            if (CrystalDock.Properties.Settings.Default.DebugMode)
            {
                SystemMonitor.BorderBrush = new SolidColorBrush(Colors.Fuchsia);
                SystemMonitor.BorderThickness = new Thickness(1);
            }
            SystemMonitor.Show();
        }

        private void InitializeSettings()
        {
            if ((settings = new Settings(CrystalDock.Properties.Resources.SettingsIniFile)) != null)
            {
                if (settings.IsDockLocked)
                {
                    if (DockWindow == null) return;
                    DockWindow.IconGrid.Background.Opacity = 0.005;
                }
            }
        }

        private void InitializeTaskBarIcon()
        {
            if (settings == null)
            {
                DockWindow!.Close();
                SystemMonitor!.Close();
                Environment.Exit(0);
                return;
            }

            TaskBarIcon.Icon = CrystalDock.Properties.Resources.CrystalDockIcon_Plain;
            TaskBarIcon.Text = "Crystal Dock";
            TaskBarIcon.Visible = true;

            ContextMenuStrip cm = new ContextMenuStrip();
            ToolStripMenuItem sysMonitorVisible = new ToolStripMenuItem()
            {
                Name = "sysMonitorVisible",
                Checked = CrystalDock.Properties.Settings.Default.SystemMonitorVisible,
                Text = "Show/Hide System Monitor",
                CheckOnClick = true
            };
            ToolStripMenuItem dockToggle = new ToolStripMenuItem()
            {
                Name = "DockToggle",
                Checked = App.settings.IsDockLocked,
                Text = "Dock Locked",
                CheckOnClick = true
            };
            ToolStripMenuItem ShowWindowsAgain = new ToolStripMenuItem()
            {
                Name = "ShowWindowsAgain",
                Text = "Show Window After Win+D"
            };
            dockToggle.Click += ToggleDockLocking;
            sysMonitorVisible.Click += SystemMonitorVisible_Toggle;
            ShowWindowsAgain.Click += ShowWindowsAgain_Click;
            cm.Items.Add(ShowWindowsAgain);
            cm.Items.Add(sysMonitorVisible);
            cm.Items.Add(dockToggle);
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Exit", null, new EventHandler(ExitApplication));

            TaskBarIcon.ContextMenuStrip = cm;

            TaskBarIcon.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right && TaskBarIcon.ContextMenuStrip != null)
                {
                    TaskBarIcon.ContextMenuStrip.Show();
                }
            };
        }

        private void ShowWindowsAgain_Click(object? sender, EventArgs e)
        {
            if (DockWindow != null)
            {
                double width = DockWindow.Width;
                double height = DockWindow!.Height;
                DockWindow.WindowState = WindowState.Maximized;
                DockWindow.WindowState = WindowState.Normal;
                DockWindow.Width = width;
                DockWindow.Height = height;
            }

            if (SystemMonitor != null)
            {
                double width = SystemMonitor.Width;
                double height = SystemMonitor.Height;
                SystemMonitor.WindowState = WindowState.Maximized;
                SystemMonitor.WindowState = WindowState.Normal;
                SystemMonitor.Width = width;
                SystemMonitor.Height = height;
            }
        }

        private void SystemMonitorVisible_Toggle(object? sender, EventArgs e)
        {
            if (CrystalDock.Properties.Settings.Default.SystemMonitorVisible && SystemMonitor._instance!.IsVisible)
            {
                CrystalDock.Properties.Settings.Default.SystemMonitorVisible = false;
                CrystalDock.Properties.Settings.Default.Save();

                SystemMonitor._instance!.Hide();
            }
            else
            {
                CrystalDock.Properties.Settings.Default.SystemMonitorVisible = true;
                CrystalDock.Properties.Settings.Default.Save();

                SystemMonitor._instance!.Show();
            }
        }

        private void ToggleDockLocking(object? sender, EventArgs e)
        {
            if (settings == null) return;
            settings.ToggleDockPositionLock();
            DockWindow!.IconGrid.Background.Opacity = settings.IsDockLocked ? 0.005 : 0.1;
            SystemMonitor!.mainStackPanel.Background = settings!.IsDockLocked ? new SolidColorBrush(Color.FromArgb(0, 255, 255, 255)) : new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
            SystemMonitor!.resizeGripBottomRight.Visibility = settings!.IsDockLocked ? Visibility.Hidden : Visibility.Visible;
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            settings!.SaveSettings();
            DockWindow!.Close();
            SystemMonitor!.Close();
            Environment.Exit(0);
        }

    }
}
