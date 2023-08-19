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
using MessageBox = System.Windows.MessageBox;
using DataFormats = System.Windows.Forms.DataFormats;
using SizeF = System.Drawing.Size;
using System.Diagnostics;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Image = System.Windows.Controls.Image;
using System.Windows.Controls.Primitives;
using Application = System.Windows.Application;
using System.Windows.Media;
using Panel = System.Windows.Controls.Panel;
using Cursors = System.Windows.Input.Cursors;

namespace CrystalDock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow? _instance;
        private Settings settings;
        private NotifyIcon TaskBarIcon = new NotifyIcon();

        public MainWindow()
        {
            InitializeComponent();
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
            settings = new Settings(Properties.Resources.SettingsIniFile);

            this.AllowDrop = true;
            _instance = this;
        }

        protected override void OnDrop(System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            base.OnDrop(e);
            if(e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if(filePaths.Count() > 0)
                {
                    // Only get the first file discard the rest
                    string filePath = filePaths[0];
                    switch(Path.GetExtension(filePath).ToLower().Substring(1))
                    {
                        case "url":
                            {
                                string[] lines = File.ReadAllLines(filePath);
                                string URL = (lines.FirstOrDefault(x => x.StartsWith("URL")) ?? "").Split('=').Last();
                                string iconPath = (lines.FirstOrDefault(x => x.StartsWith("IconFile")) ?? "").Split('=').Last();
                                if (!File.Exists(iconPath)) return;
                                string savePath = Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + settings.NextEntry;
                                if (File.Exists(savePath + ".png")) File.Delete(savePath + ".png");
                                if (File.Exists(savePath + "_over.png")) File.Delete(savePath + "_over.png");
                                if (Path.GetExtension(iconPath).ToLower().Substring(1) == "exe")
                                {
                                    System.Drawing.Icon.ExtractAssociatedIcon(iconPath)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                }
                                else
                                {
                                    new Icon(iconPath, new SizeF(256, 256)).ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                }
                                if (!File.Exists(savePath + ".png")) return;
                                File.Copy(savePath + ".png", savePath + "_over.png");
                                IconInfo iconInfo = new IconInfo();
                                iconInfo.IconImage = settings.NextEntry + ".png";
                                iconInfo.IconImageHover = settings.NextEntry + "_over.png";
                                iconInfo.Action = URL;
                                settings.AddEntry(iconInfo);
                                LoadButtons();
                                break;
                            }
                        case "lnk":
                            {
                                // Do something with shortcut files?
                                ShortcutInfo info = ShortcutInfo.FromShortcutFile(filePath);
                                string URL = info.TargetPath;
                                //Discord cause they be different
                                if (info.TargetPath.Contains("Update.exe") && info.Arguments.Contains("--processStart Discord.exe"))
                                {
                                    URL = info.WorkingDirectory + "\\" + "Discord.exe";
                                }
                                string savePath = Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + settings.NextEntry;
                                if (File.Exists(savePath + ".png")) File.Delete(savePath + ".png");
                                if (File.Exists(savePath + "_over.png")) File.Delete(savePath + "_over.png");
                                if (Path.GetExtension(URL).ToLower().Substring(1) == "exe")
                                {
                                    System.Drawing.Icon.ExtractAssociatedIcon(URL)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                }
                                else return;
                                if (!File.Exists(savePath + ".png")) return;
                                File.Copy(savePath + ".png", savePath + "_over.png");
                                IconInfo iconInfo = new IconInfo();
                                iconInfo.IconImage = settings.NextEntry + ".png";
                                iconInfo.IconImageHover = settings.NextEntry + "_over.png";
                                iconInfo.Action = URL;
                                settings.AddEntry(iconInfo);
                                LoadButtons();
                                break;
                            }
                        case "exe":
                            {
                                // filePath is the path to the executable
                                // using Icon.ExtractAssociatedIcon(fileName) we can extract the icon (wont have a fancy hover but meh)
                                string URL = filePath;
                                string savePath = Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + settings.NextEntry;
                                if (File.Exists(savePath + ".png")) File.Delete(savePath + ".png");
                                if (File.Exists(savePath + "_over.png")) File.Delete(savePath + "_over.png");
                                if (Path.GetExtension(URL).ToLower().Substring(1) == "exe")
                                {
                                    System.Drawing.Icon.ExtractAssociatedIcon(URL)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                                }
                                else return;
                                if (!File.Exists(savePath + ".png")) return;
                                File.Copy(savePath + ".png", savePath + "_over.png");
                                IconInfo iconInfo = new IconInfo();
                                iconInfo.IconImage = settings.NextEntry + ".png";
                                iconInfo.IconImageHover = settings.NextEntry + "_over.png";
                                iconInfo.Action = URL;
                                settings.AddEntry(iconInfo);
                                LoadButtons();
                                break;
                            }
                    }
                }
            }
        }

        private void MainWindow_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (settings.IsDockLocked) return;
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.Save();
        }

        private void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (settings.IsDockLocked) return;
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
            this.Width = (settings.GetValue<UInt32>("Global", "IconSize") * 2) + settings.GetValue<int>("Global", "IconMargins");
            this.Height = settings.GetValue<UInt32>("Global", "IconSize") * 2;
            mainGrid.Margin = new Thickness(0, Height - 16 - settings.GetValue<UInt32>("Global", "IconSize") - settings.GetValue<UInt32>("Global", "IconMargins"), 0, 0);

            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.Save();

            TaskBarIcon.Icon = Properties.Resources.CrystalDockIcon_Plain;
            TaskBarIcon.Text = "Crystal Dock";
            TaskBarIcon.Visible = true;
            ContextMenuStrip cm = new ContextMenuStrip();
            //cm.Items.Add("Settings", null, new EventHandler(OpenSettings));
            ToolStripMenuItem DockToggle = new ToolStripMenuItem()
            {
                Name = "DockToggle",
                Checked = settings.IsDockLocked,
                Text = "Dock Locked",
                CheckOnClick = true
            };
            DockToggle.Click += ToggleDockLocking;
            cm.Items.Add(DockToggle);
            //cm.Items.Add("Unlock Dock", null, new EventHandler(UnlockDock));
            cm.Items.Add(new ToolStripSeparator());
            cm.Items.Add("Exit", null, new EventHandler(ExitApplication));
            TaskBarIcon.ContextMenuStrip = cm;
            TaskBarIcon.MouseClick += (s, e) =>
            {
                if(e.Button == MouseButtons.Right)
                {
                    if (TaskBarIcon.ContextMenuStrip != null)
                    {
                        TaskBarIcon.ContextMenuStrip.Show();
                    }
                }
            };

            if(settings != null)
            {
                LoadButtons();
                //if (Convert.ToBoolean(settings.GetGlobalSettings["PositionLocked"]))
                if(settings.IsDockLocked)
                {
                    IconGrid.Background.Opacity = 0.005;
                    return;
                }
            }
        }

        private void ToggleDockLocking(object? sender, EventArgs e)
        {
            if (settings == null) return;
            settings.ToggleDockPositionLock();
            if (!settings.IsDockLocked)
                IconGrid.Background.Opacity = 0.1;
            else IconGrid.Background.Opacity = 0.005;
        }

        public void LoadButtons()
        {
            ClearIconGrid();

            int column = 0;

            foreach (var iconEntry in settings.GetIconEntries())
            {
                IconInfo iconInfo = iconEntry.Value;
                string iconImagePath = Path.Combine(Directory.GetCurrentDirectory(), Properties.Resources.IconFolder, iconInfo.IconImage);

                if (File.Exists(iconImagePath))
                {
                    System.Windows.Controls.Image iconImg = CreateIconImage(iconEntry, iconImagePath, column);

                    IconGrid.Children.Add(iconImg);

                    column++;
                } else if(File.Exists(Properties.Resources.MissingIconPath))
                {
                    System.Windows.Controls.Image iconImg = CreateIconImage(iconEntry, Properties.Resources.MissingIconPath, column);

                    IconGrid.Children.Add(iconImg);

                    column++;
                }
            }

            AdjustGridSizes();
        }

        private void ClearIconGrid()
        {
            foreach (var item in IconGrid.Children.OfType<System.Windows.Controls.Image>())
            {
                item.Source = null;
            }

            IconGrid.Children.Clear();
            IconGrid.ColumnDefinitions.Clear();
        }

        private System.Windows.Controls.Image CreateIconImage(KeyValuePair<string, IconInfo> iconInfo, string imagePath, int column)
        {
            System.Windows.Controls.Image iconImg = new System.Windows.Controls.Image
            {
                Name = iconInfo.Value.IconImage.Substring(0, iconInfo.Value.IconImage.Length - 4),
                Width = settings.GetValue<UInt32>("Global", "IconSize"),
                Height = settings.GetValue<UInt32>("Global", "IconSize"),
                //Cursor = System.Windows.Input.Cursors.Hand,
                Tag = iconInfo,
                ContextMenu = CreateContextMenu(iconInfo.Key),
                Opacity = 0.5
            };

            // Load the image using a stream
            using (FileStream fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = fileStream;
                bitmapImage.EndInit();

                iconImg.Source = bitmapImage;
            }

            iconImg.MouseLeftButtonUp += IconImg_MouseLeftButtonUp;
            iconImg.ContextMenuOpening += IconImg_ContextMenuOpening;
            iconImg.MouseEnter += IconImg_MouseEnter;
            iconImg.MouseLeave += IconImg_MouseLeave;

            Grid.SetRow(iconImg, 0);
            Grid.SetColumn(iconImg, column);

            ColumnDefinition colDefinition = new ColumnDefinition();
            IconGrid.ColumnDefinitions.Add(colDefinition);

            return iconImg;
        }

        private void IconImg_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Image img)
            {
                KeyValuePair<string, IconInfo> iconInfo = (KeyValuePair<string, IconInfo>)img.Tag;
                if (iconInfo.Value == null) return;

                // Calculate the column index of the hovered icon
                int columnIndex = Grid.GetColumn(img);

                // Calculate the position based on the icon's column index within the IconGrid
                double left = columnIndex * (settings.GetValue<UInt32>("Global", "IconSize") + settings.GetValue<int>("Global", "IconMargins"));
                double top = 0; // Assuming the icons are in the first row

                // Create a new Image for the zoomed icon
                Image zoomedImg = new Image
                {
                    Source = img.Source,
                    Width = img.ActualWidth + 10,
                    Height = img.ActualHeight + 10,
                    ContextMenu = CreateContextMenu(iconInfo.Key),
                    Cursor = Cursors.Hand
                };
                zoomedImg.MouseLeave += ZoomedImg_MouseLeave;
                zoomedImg.MouseLeftButtonUp += (_s, _e) => IconImg_MouseLeftButtonUp(sender, _e);
                zoomedImg.ContextMenuOpening += IconImg_ContextMenuOpening;

                // Apply a TranslateTransform to center the zoomed icon
                double xOffset = (zoomedImg.Width - img.Width) / 2;
                double yOffset = (zoomedImg.Height - img.Height) / 2;
                zoomedImg.RenderTransform = new TranslateTransform(xOffset, yOffset);

                Panel.SetZIndex(zoomedImg, int.MaxValue);

                // Set the position of the zoomed icon within the Canvas overlay
                Canvas.SetLeft(zoomedImg, left);
                Canvas.SetTop(zoomedImg, top);

                // Add the zoomed icon to the Canvas overlay
                canvasOverlay.Children.Clear();
                canvasOverlay.Children.Add(zoomedImg);
            }
        }

        private void ZoomedImg_MouseLeave(object sender, MouseEventArgs e)
        {
            // Remove the zoomed icon from the Canvas overlay
            MainWindow_MouseLeave(sender, e);
            canvasOverlay.Children.Clear();
        }

        private void IconImg_MouseLeave(object sender, MouseEventArgs e)
        {

        }

        private void IconImg_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = settings.IsDockLocked;
        }

        private ContextMenu CreateContextMenu(string iconKey)
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem removeItem = new MenuItem()
            {
                Header = "Remove",
                Command = new RelayCommand<string>(settings.RemoveEntry),
                CommandParameter = iconKey
            };
            MenuItem RefreshItem = new MenuItem()
            {
                Header = "Refresh",
                Command = new RelayCommand(LoadButtons)
            };

            contextMenu.Items.Add(removeItem);
            contextMenu.Items.Add(RefreshItem);

            return contextMenu;
        }

        private void AdjustGridSizes()
        {
            int iconSizeWithMargin = (int)settings.GetValue<UInt32>("Global", "IconSize") + settings.GetValue<int>("Global", "IconMargins");
            int iconCount = IconGrid.Children.Count;

            if (iconCount > 0)
            {
                this.Width = (settings.GetValue<UInt32>("Global", "IconSize") + settings.GetValue<int>("Global", "IconMargins")) * iconCount;

                IconGrid.MaxWidth = iconSizeWithMargin * iconCount;
                IconGrid.Width = IconGrid.MaxWidth + settings.GetValue<int>("Global", "IconMargins");
                IconGrid.Height = iconSizeWithMargin;

                mainGrid.MaxWidth = IconGrid.MaxWidth + settings.GetValue<int>("Global", "IconMargins");
                mainGrid.Width = mainGrid.MaxWidth;
                mainGrid.Height = IconGrid.Height + settings.GetValue<int>("Global", "IconMargins");

                BottomLine.X2 = mainGrid.Width;
                BottomLine.Y1 = mainGrid.Height - 4;
                BottomLine.Y2 = mainGrid.Height - 4;
            }
        }

        private void IconImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is System.Windows.Controls.Image iconImg)
            {
                if (iconImg.Tag is KeyValuePair<string, IconInfo> tag)
                {
                    if (tag.Value is IconInfo iconInfo)
                    {
                        e.Handled = true;
                        ShellExecute(IntPtr.Zero, "open", iconInfo.Action, null, null, 1);
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
            if (!settings.IsDockLocked)
            {
                IconGrid.Background.Opacity = 0.1;
                return;
            }
            IconGrid.Background.Opacity = 0.005;
            foreach (System.Windows.Controls.Image iconImg in IconGrid.Children.OfType<System.Windows.Controls.Image>())
            {
                iconImg.Opacity = 0.5;
            }
            canvasOverlay.Children.Clear();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            settings.SaveSettings();
        }

        private void MainWindow_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!settings.IsDockLocked)
            {
                IconGrid.Background.Opacity = 0.1;
                return;
            }
            IconGrid.Background.Opacity = 0.005;
            foreach (System.Windows.Controls.Image iconImg in IconGrid.Children.OfType<System.Windows.Controls.Image>())
            {
                iconImg.Opacity = 1;
            }
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string? lpParameters,
            string? lpDirectory,
            int nShowCmd);
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

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        public RelayCommand(Action execute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            _execute();
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
