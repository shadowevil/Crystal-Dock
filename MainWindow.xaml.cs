using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using MessageBox = System.Windows.MessageBox;
using System.Diagnostics;

namespace CrystalDock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool dragging = false;

        private Settings settings;
        private NotifyIcon ni = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
            settings = new Settings(Properties.Resources.SettingsIniFile);
        }

        private void MainWindow_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"])) return;
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.Save();
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"])) return;
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }


        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Variable to hold the handle for the form
            var helper = new WindowInteropHelper(this).Handle;
            //Performing some magic to hide the form from Alt+Tab
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            ni.Icon = Properties.Resources.CrystalDockIcon_Plain;
            ni.Visible = true;
            ContextMenuStrip cm = new ContextMenuStrip();
            //cm.Items.Add("Settings", null, new EventHandler(OpenSettings));
            ToolStripMenuItem DockToggle = new ToolStripMenuItem()
            {
                Name = "DockToggle",
                Checked = Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]),
                Text = "Dock Locked",
                CheckOnClick = true,

            };
            DockToggle.Click += ToggleDockLocking;
            cm.Items.Add(DockToggle);
            //cm.Items.Add("Unlock Dock", null, new EventHandler(UnlockDock));
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Exit", null, new EventHandler(ExitApplication));
            ni.ContextMenuStrip = cm;
            ni.MouseClick += (s, e) =>
            {
                if(e.Button == MouseButtons.Right)
                {
                    if (ni.ContextMenuStrip != null)
                    {
                        ni.ContextMenuStrip.Show();
                    }
                }
            };

            if(settings != null)
            {
                LoadButtons();
                if (Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]))
                {
                    this.Background.Opacity = 0.005;
                    return;
                }
            }
        }

        private void ToggleDockLocking(object? sender, EventArgs e)
        {
            if (settings == null) return;
            settings.ToggleDockPositionLock();
            if (!Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]))
                this.Background.Opacity = 0.1;
            else this.Background.Opacity = 0.005;
        }

        private void LoadButtons()
        {
            IconGrid.Children.Clear();
            IconGrid.ColumnDefinitions.Clear();
            int column = 0; // Start from the first column

            foreach (var iconEntry in settings.GetIconEntries())
            {
                IconInfo iconInfo = iconEntry.Value;
                if (File.Exists(Properties.Resources.IconFolder + iconInfo.IconImage))
                {
                    System.Windows.Controls.Image iconImg = new System.Windows.Controls.Image
                    {
                        Name = iconInfo.IconImage.Substring(0, iconInfo.IconImage.Length - 4),
                        Width = Convert.ToUInt32(settings.GetGlobalSettings["IconSize"]),
                        Height = Convert.ToUInt32(settings.GetGlobalSettings["IconSize"]),
                        Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + iconInfo.IconImage)),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = iconInfo,
                        ContextMenu = new ContextMenu()
                    };

                    MenuItem tsmi = new MenuItem()
                    {
                        Header = "Remove"
                    };
                    ICommand? cmd = null;
                    cmd = new RelayCommand<string>(RemoveEntry);
                    tsmi.Command = cmd;
                    tsmi.CommandParameter = iconEntry.Key;
                    iconImg.ContextMenu.Items.Add(tsmi);
                    iconImg.ContextMenuOpening += (s, e) =>
                    {
                        e.Handled = Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]);
                    };

                    iconImg.Opacity = 0.5;

                    // Create a new column definition for the current image
                    ColumnDefinition colDefinition = new ColumnDefinition();
                    IconGrid.ColumnDefinitions.Add(colDefinition);

                    Grid.SetRow(iconImg, 0); // Assuming you want to place the images in the first row
                    Grid.SetColumn(iconImg, column);

                    iconImg.MouseLeftButtonUp += IconImg_MouseLeftButtonUp;

                    iconImg.MouseEnter += (s, e) =>
                    {
                        if (s is System.Windows.Controls.Image g)
                            g.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + (g.Tag as IconInfo)?.IconImageHover));
                    };
                    iconImg.MouseLeave += (s, e) =>
                    {
                        if (s is System.Windows.Controls.Image g)
                            g.Source = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + (g.Tag as IconInfo)?.IconImage));
                    };

                    IconGrid.Children.Add(iconImg);

                    column++; // Move to the next column for the next image
                }
            }

            IconGrid.MaxWidth = (Convert.ToUInt32(settings.GetGlobalSettings["IconSize"]) + 10) * IconGrid.Children.Count;
            IconGrid.Width = IconGrid.MaxWidth;
            IconGrid.Height = (Convert.ToUInt32(settings.GetGlobalSettings["IconSize"]) + 10);
            mainGrid.MaxWidth = IconGrid.MaxWidth + 10;
            mainGrid.Width = mainGrid.MaxWidth;
            mainGrid.Height = IconGrid.Height + 10;
            MaxWidth = IconGrid.MaxWidth + 10;
            Width = MaxWidth;
            Height = mainGrid.Height;
            BottomLine.X2 = Width;
            BottomLine.Y1 = Height - 4;
            BottomLine.Y2 = Height - 4;
        }

        private void RemoveEntry(string key)
        {
            settings.RemoveEntry(key);
            LoadButtons();
        }

        private void IconImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Image iconImg)
            {
                if (iconImg.Tag is IconInfo iconInfo)
                {
                    using (Process myProcess = new Process())
                    {
                        myProcess.StartInfo.FileName = iconInfo.Action;
                        myProcess.Start();
                    }
                }
            }
        }

        private void OpenSettings(object? sender, EventArgs e)
        {
            MessageBox.Show("This is supposed to open the settings menu");
        }

        private void ExitApplication(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void MainWindow_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]))
            {
                this.Background.Opacity = 0.1;
                return;
            }
            this.Background.Opacity = 0.005;
            foreach (System.Windows.Controls.Image iconImg in IconGrid.Children.OfType<System.Windows.Controls.Image>())
            {
                iconImg.Opacity = 0.5;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settings.SaveSettings();
        }

        private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]))
            {
                this.Background.Opacity = 0.1;
                return;
            }
            this.Background.Opacity = 0.005;
            foreach (System.Windows.Controls.Image iconImg in IconGrid.Children.OfType<System.Windows.Controls.Image>())
            {
                iconImg.Opacity = 1;
            }
        }
    }
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke((T)parameter!) ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute((T)parameter!);
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }

    public static class ShiftWindowOntoScreenHelper
    {
        /// <summary>
        ///     Intent:  
        ///     - Shift the window onto the visible screen.
        ///     - Shift the window away from overlapping the task bar.
        /// </summary>
        public static void ShiftWindowOntoScreen(Window window)
        {
            // Note that "window.BringIntoView()" does not work.                            
            if (window.Top < SystemParameters.VirtualScreenTop)
            {
                window.Top = SystemParameters.VirtualScreenTop;
            }

            if (window.Left < SystemParameters.VirtualScreenLeft)
            {
                window.Left = SystemParameters.VirtualScreenLeft;
            }

            if (window.Left + window.Width > SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth)
            {
                window.Left = SystemParameters.VirtualScreenWidth + SystemParameters.VirtualScreenLeft - window.Width;
            }

            if (window.Top + window.Height > SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight)
            {
                window.Top = SystemParameters.VirtualScreenHeight + SystemParameters.VirtualScreenTop - window.Height;
            }

            // Shift window away from taskbar.
            {
                var taskBarLocation = GetTaskBarLocationPerScreen();

                // If taskbar is set to "auto-hide", then this list will be empty, and we will do nothing.
                foreach (var taskBar in taskBarLocation)
                {
                    Rectangle windowRect = new Rectangle((int)window.Left, (int)window.Top, (int)window.Width, (int)window.Height);

                    // Keep on shifting the window out of the way.
                    int avoidInfiniteLoopCounter = 25;
                    while (windowRect.IntersectsWith(taskBar))
                    {
                        avoidInfiniteLoopCounter--;
                        if (avoidInfiniteLoopCounter == 0)
                        {
                            break;
                        }

                        // Our window is covering the task bar. Shift it away.
                        var intersection = Rectangle.Intersect(taskBar, windowRect);

                        if (intersection.Width < window.Width
                            // This next one is a rare corner case. Handles situation where taskbar is big enough to
                            // completely contain the status window.
                            || taskBar.Contains(windowRect))
                        {
                            if (taskBar.Left == 0)
                            {
                                // Task bar is on the left. Push away to the right.
                                window.Left = window.Left + intersection.Width;
                            }
                            else
                            {
                                // Task bar is on the right. Push away to the left.
                                window.Left = window.Left - intersection.Width;
                            }
                        }

                        if (intersection.Height < window.Height
                            // This next one is a rare corner case. Handles situation where taskbar is big enough to
                            // completely contain the status window.
                            || taskBar.Contains(windowRect))
                        {
                            if (taskBar.Top == 0)
                            {
                                // Task bar is on the top. Push down.
                                window.Top = window.Top + intersection.Height;
                            }
                            else
                            {
                                // Task bar is on the bottom. Push up.
                                window.Top = window.Top - intersection.Height;
                            }
                        }

                        windowRect = new Rectangle((int)window.Left, (int)window.Top, (int)window.Width, (int)window.Height);
                    }
                }
            }
        }

        /// <summary>
        /// Returned location of taskbar on a per-screen basis, as a rectangle. See:
        /// https://stackoverflow.com/questions/1264406/how-do-i-get-the-taskbars-position-and-size/36285367#36285367.
        /// </summary>
        /// <returns>A list of taskbar locations. If this list is empty, then the taskbar is set to "Auto Hide".</returns>
        private static List<Rectangle> GetTaskBarLocationPerScreen()
        {
            List<Rectangle> dockedRects = new List<Rectangle>();
            foreach (var screen in Screen.AllScreens)
            {
                if (screen.Bounds.Equals(screen.WorkingArea) == true)
                {
                    // No taskbar on this screen.
                    continue;
                }

                Rectangle rect = new Rectangle();

                var leftDockedWidth = Math.Abs((Math.Abs(screen.Bounds.Left) - Math.Abs(screen.WorkingArea.Left)));
                var topDockedHeight = Math.Abs((Math.Abs(screen.Bounds.Top) - Math.Abs(screen.WorkingArea.Top)));
                var rightDockedWidth = ((screen.Bounds.Width - leftDockedWidth) - screen.WorkingArea.Width);
                var bottomDockedHeight = ((screen.Bounds.Height - topDockedHeight) - screen.WorkingArea.Height);
                if ((leftDockedWidth > 0))
                {
                    rect.X = screen.Bounds.Left;
                    rect.Y = screen.Bounds.Top;
                    rect.Width = leftDockedWidth;
                    rect.Height = screen.Bounds.Height;
                }
                else if ((rightDockedWidth > 0))
                {
                    rect.X = screen.WorkingArea.Right;
                    rect.Y = screen.Bounds.Top;
                    rect.Width = rightDockedWidth;
                    rect.Height = screen.Bounds.Height;
                }
                else if ((topDockedHeight > 0))
                {
                    rect.X = screen.WorkingArea.Left;
                    rect.Y = screen.Bounds.Top;
                    rect.Width = screen.WorkingArea.Width;
                    rect.Height = topDockedHeight;
                }
                else if ((bottomDockedHeight > 0))
                {
                    rect.X = screen.WorkingArea.Left;
                    rect.Y = screen.WorkingArea.Bottom;
                    rect.Width = screen.WorkingArea.Width;
                    rect.Height = bottomDockedHeight;
                }
                else
                {
                    // Nothing found!
                }

                dockedRects.Add(rect);
            }

            if (dockedRects.Count == 0)
            {
                // Taskbar is set to "Auto-Hide".
            }

            return dockedRects;
        }
    }
}
