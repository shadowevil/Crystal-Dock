using System;
using System.Collections.Generic;
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
using DataFormats = System.Windows.Forms.DataFormats;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Image = System.Windows.Controls.Image;
using Panel = System.Windows.Controls.Panel;
using Cursors = System.Windows.Input.Cursors;
using IconD = System.Drawing.Icon;
using CancelEventArgs = System.ComponentModel.CancelEventArgs;

namespace CrystalDock
{
    public partial class MainWindow : Window
    {
        public static MainWindow? _instance;
        private Settings? settings;
        private NotifyIcon TaskBarIcon = new NotifyIcon();

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr ShellExecute(IntPtr hwnd, string lpOperation, string lpFile, string? lpParameters, string? lpDirectory, int nShowCmd);
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EX_STYLE = -20;
        private const int WS_EX_APPWINDOW = 0x00040000, WS_EX_TOOLWINDOW = 0x00000080;
        private void ExitApplication(object? sender, EventArgs e) => this.Close();

        public MainWindow()
        {
            InitializeComponent();

            InitializeSettings();
            InitializeUI();
            InitializeTaskBarIcon();
            LoadButtons();
        }

        private void InitializeSettings()
        {
            if((settings = new Settings(Properties.Resources.SettingsIniFile)) != null)
            {
                if (settings.IsDockLocked)
                {
                    IconGrid.Background.Opacity = 0.005;
                }
            }
        }

        private void InitializeUI()
        {
            if (settings == null)
            {
                this.Close();
                return;
            }

            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;

            this.AllowDrop = true;
            _instance = this;

            var helper = new WindowInteropHelper(this).Handle;
            SetWindowLong(helper, GWL_EX_STYLE, (GetWindowLong(helper, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            int iconSize = (int)settings.GetValue<UInt32>("Global", "IconSize");
            int iconMargins = (int)settings.GetValue<UInt32>("Global", "IconMargins");

            this.Width = iconSize * 2 + iconMargins;
            this.Height = iconSize * 2;

            mainGrid.Margin = new Thickness(0, Height - 16 - iconSize - iconMargins, 0, 0);

            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);

            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.Save();
        }

        private void InitializeTaskBarIcon()
        {
            if (settings == null)
            {
                this.Close();
                return;
            }

            TaskBarIcon.Icon = Properties.Resources.CrystalDockIcon_Plain;
            TaskBarIcon.Text = "Crystal Dock";
            TaskBarIcon.Visible = true;

            ContextMenuStrip cm = new ContextMenuStrip();
            ToolStripMenuItem dockToggle = new ToolStripMenuItem()
            {
                Name = "DockToggle",
                Checked = settings.IsDockLocked,
                Text = "Dock Locked",
                CheckOnClick = true
            };
            dockToggle.Click += ToggleDockLocking;
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

        private void MainWindow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (settings == null) return;
            if (!settings.IsDockLocked)
            {
                IconGrid.Background.Opacity = 0.1;
                return;
            }
            IconGrid.Background.Opacity = 0.005;
            foreach (Image iconImg in IconGrid.Children.OfType<Image>())
            {
                iconImg.Opacity = 0.5;
            }
            canvasOverlay.Children.Clear();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (settings == null) return;
            settings.SaveSettings();
        }

        private void MainWindow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (settings == null) return;
            if (!settings.IsDockLocked)
            {
                IconGrid.Background.Opacity = 0.1;
                return;
            }
            IconGrid.Background.Opacity = 0.005;
            foreach (Image iconImg in IconGrid.Children.OfType<Image>())
            {
                iconImg.Opacity = 1;
            }
        }
        private void MainWindow_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (settings == null) return;
            if (settings.IsDockLocked) return;
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.Save();
        }

        private void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (settings == null) return;
            if (settings.IsDockLocked) return;
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        protected override void OnDrop(System.Windows.DragEventArgs e)
        {
            e.Handled = true;
            base.OnDrop(e);

            if (settings == null)
                return;

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (filePaths.Length > 0)
                {
                    string filePath = filePaths[0];
                    string extension = Path.GetExtension(filePath).ToLower().Substring(1);
                    string savePath = Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + settings.NextEntry;

                    if(File.Exists(savePath + ".png"))
                    {
                        File.Delete(savePath + ".png");
                        File.Delete(savePath + "_over.png");
                    }

                    string URL = "";
                    IconInfo iconInfo = new IconInfo();

                    switch (extension)
                    {
                        case "url":
                            string[] lines = File.ReadAllLines(filePath);
                            URL = (lines.FirstOrDefault(x => x.StartsWith("URL")) ?? "").Split('=').Last();
                            string iconPath = (lines.FirstOrDefault(x => x.StartsWith("IconFile")) ?? "").Split('=').Last();
                            if (!File.Exists(iconPath)) return;
                            IconD.ExtractAssociatedIcon(iconPath)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            break;

                        case "lnk":
                            ShortcutInfo? info = ShortcutInfo.FromShortcutFile(filePath);
                            if (info == null) return;
                            URL = info.TargetPath;
                            if (info.TargetPath.Contains("Update.exe") && info.Arguments.Contains("--processStart Discord.exe"))
                            {
                                URL = info.WorkingDirectory + "\\" + "Discord.exe";
                            }
                            IconD.ExtractAssociatedIcon(URL)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            break;

                        case "exe":
                            URL = filePath;
                            IconD.ExtractAssociatedIcon(URL)?.ToBitmap().Save(savePath + ".png", System.Drawing.Imaging.ImageFormat.Png);
                            break;
                    }

                    if (!File.Exists(savePath + ".png")) return;

                    File.Copy(savePath + ".png", savePath + "_over.png");

                    iconInfo.IconImage = settings.NextEntry + ".png";
                    iconInfo.IconImageHover = settings.NextEntry + "_over.png";
                    iconInfo.Action = URL;
                    settings.AddEntry(iconInfo);
                    LoadButtons();
                }
            }
        }

        private void IconImg_MouseEnter(object sender, MouseEventArgs e)
        {
            if (settings == null) return;
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
                zoomedImg.AllowDrop = true;
                zoomedImg.Drop += (_s, _e) => OnDrop(_e);

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

        private void IconImg_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (settings == null) return;
            e.Handled = settings.IsDockLocked;
        }

        private void IconImg_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (settings == null) return;
            if (!settings.IsDockLocked) return;
            if (sender is Image iconImg)
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

        private ContextMenu CreateContextMenu(string iconKey)
        {
            ContextMenu contextMenu = new ContextMenu();
            MenuItem removeItem = new MenuItem()
            {
                Header = "Remove",
                Command = new RelayCommand<string>(settings!.RemoveEntry),
                CommandParameter = iconKey
            };
            MenuItem RefreshItem = new MenuItem()
            {
                Header = "Refresh",
                Command = new RelayCommand(LoadButtons)
            };
            MenuItem ModifyItem = new MenuItem()
            {
                Header = "Modify",
                Command = new RelayCommand<string>(ShowSettingsWindow),
                CommandParameter = iconKey
            };

            contextMenu.Items.Add(ModifyItem);
            contextMenu.Items.Add(RefreshItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(removeItem);

            return contextMenu;
        }

        private void ShowSettingsWindow(string infoKey)
        {
            if (settings == null) return;
            using (ModifyIcon modifyIcon = new ModifyIcon(infoKey))
            {
                modifyIcon.Owner = this;
                MainWindow_MouseLeave(null!, null!);
                canvasOverlay.Children.Clear();
                if (modifyIcon.ShowDialog() == true)
                {
                    if (modifyIcon.iconInfo == null) return;
                    IconInfo info = settings.GetIconInfo(infoKey)!;
                    if (modifyIcon.iconInfo.IconImage != info.IconImage) File.Delete(Properties.Resources.IconFolder + info!.IconImage);
                    if (modifyIcon.iconInfo.IconImageHover != info.IconImageHover) File.Delete(Properties.Resources.IconFolder + info!.IconImageHover);
                    settings.UpdateEntry(infoKey, modifyIcon.iconInfo);
                    LoadButtons();
                }
            }
        }

        private void ToggleDockLocking(object? sender, EventArgs e)
        {
            if (settings == null) return;
            settings.ToggleDockPositionLock();
            IconGrid.Background.Opacity = settings.IsDockLocked ? 0.005 : 0.1;
        }

        public void LoadButtons()
        {
            if (settings == null) return;
            ClearIconGrid();

            int column = 0;

            foreach (var iconEntry in settings.GetIconEntries())
            {
                IconInfo iconInfo = iconEntry.Value;
                string iconImagePath = Path.Combine(Directory.GetCurrentDirectory(), Properties.Resources.IconFolder, iconInfo.IconImage);

                if (File.Exists(iconImagePath))
                {
                    Image? iconImg = CreateIconImage(iconEntry, iconImagePath, column);
                    if (iconImg == null) throw new NullReferenceException(nameof(iconImg));
                    IconGrid.Children.Add(iconImg);

                    column++;
                }
                else if (File.Exists(Properties.Resources.MissingIconPath))
                {
                    Image? iconImg = CreateIconImage(iconEntry, Properties.Resources.MissingIconPath, column);
                    if (iconImg == null) throw new NullReferenceException(nameof(iconImg));
                    IconGrid.Children.Add(iconImg);

                    column++;
                }
            }

            AdjustGridSizes();
        }

        private void ClearIconGrid()
        {
            foreach (var item in IconGrid.Children.OfType<Image>()) item.Source = null;

            IconGrid.Children.Clear();
            IconGrid.ColumnDefinitions.Clear();
        }

        private Image? CreateIconImage(KeyValuePair<string, IconInfo> iconInfo, string imagePath, int column)
        {
            if (settings == null) return null;
            Image iconImg = new Image
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

            Grid.SetRow(iconImg, 0);
            Grid.SetColumn(iconImg, column);

            ColumnDefinition colDefinition = new ColumnDefinition();
            IconGrid.ColumnDefinitions.Add(colDefinition);

            return iconImg;
        }

        private void AdjustGridSizes()
        {
            if (settings == null) return;
            int iconSizeWithMargin = (int)settings.GetValue<UInt32>("Global", "IconSize") + settings.GetValue<int>("Global", "IconMargins");
            int iconCount = IconGrid.Children.Count;

            if (iconCount > 0)
            {
                this.Width = (settings.GetValue<UInt32>("Global", "IconSize") + (settings.GetValue<int>("Global", "IconMargins") * 2)) * iconCount;

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
    }
}
