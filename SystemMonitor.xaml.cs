using CrystalDock.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CrystalDock
{
    /// <summary>
    /// Interaction logic for SystemMonitor.xaml
    /// </summary>
    public partial class SystemMonitor : Window
    {
        public static SystemMonitor? _instance;
        private bool isResizing = false;
        private bool EnteredResizing = false;
        private Point startPoint;

        private Grid driveEntryGrid = new Grid();
        private Line bottomLine = new Line();

        public class DriveEntry
        {
            public BitmapImage? DriveIcon { get; set; } = null;
            public string DriveLetter { get; set; } = "";
            public string DriveInfoText { get; set; } = "";
            public double TotalSpace { get; set; }
            public double UsedSpace { get; set; }
        }

        public SystemMonitor()
        {
            InitializeComponent();
            _instance = this;

            LoadUIInformation();
        }

        private void LoadUIInformation()
        {
            if (!App.settings!.IsDockLocked)
            {
                mainStackPanel.Background = new SolidColorBrush(Color.FromArgb(50, 255, 255, 255));
                resizeGripBottomRight.Visibility = Visibility.Visible;
            }
            if(App.settings!.IsDockLocked)
            {
                resizeGripBottomRight.Visibility = Visibility.Hidden;
            }
            mainStackPanel.VerticalAlignment = VerticalAlignment.Bottom;
            CreateDriveEntries();
        }

        private void CreateDriveEntries()
        {
            DriveInfo[] drives = DriveInfo.GetDrives();

            foreach (DriveInfo drive in drives)
            {
                if (drive.IsReady)
                {
                    string driveName = drive.Name;
                    long totalSize = drive.TotalSize;
                    long freeSpace = drive.AvailableFreeSpace;

                    string driveInfo = $"Drive: {driveName}\nTotal Space: {totalSize / (1024 * 1024 * 1024)} GB\nFree Space: {freeSpace / (1024 * 1024 * 1024)} GB\n";

                    // Create the main grid for the drive entry
                    driveEntryGrid = new Grid();
                    driveEntryGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Column for icon
                    driveEntryGrid.ColumnDefinitions.Add(new ColumnDefinition()); // Column for info

                    // Create the image for the drive icon
                    Image driveImage = new Image
                    {
                        Width = 75,
                        Height = 75,
                        Margin = new Thickness(10)
                    };

                    // Set the drive icon image source here
                    BitmapImage bitmapImage = new BitmapImage(new Uri(Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder + "DriveIcon.png"));
                    driveImage.Source = bitmapImage;

                    // Create the grid for progress bar and drive info
                    Grid infoGrid = new Grid();
                    infoGrid.RowDefinitions.Add(new RowDefinition()); // Row for progress bar
                    infoGrid.RowDefinitions.Add(new RowDefinition()); // Row for text info

                    // Create progress bar
                    ProgressBar progressBar = new ProgressBar
                    {
                        Height = 20,
                        MaxWidth = this.Width - driveEntryGrid.ColumnDefinitions[0].Width.Value - 20,
                        Maximum = totalSize,
                        Value = totalSize - freeSpace,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };

                    // Create text block for drive info
                    TextBlock textBlock = new TextBlock
                    {
                        Text = driveInfo,
                        Foreground = new SolidColorBrush(Colors.White),
                        Margin = new Thickness(1),
                        TextWrapping = TextWrapping.Wrap
                    };

                    // Add progress bar and text block to info grid
                    Grid.SetRow(progressBar, 0);
                    infoGrid.Children.Add(progressBar);
                    Grid.SetRow(textBlock, 1);
                    infoGrid.Children.Add(textBlock);

                    // Add drive image and info grid to drive entry grid
                    driveEntryGrid.Children.Add(driveImage);
                    Grid.SetColumn(infoGrid, 1);
                    driveEntryGrid.Children.Add(infoGrid);

                    // Add drive entry grid to main stack panel
                    mainStackPanel.Children.Add(driveEntryGrid);
                }
            }
        }

        private void SysMonWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //Variable to hold the handle for the form
            var helper = new WindowInteropHelper(this).Handle;
            //Performing some magic to hide the form from Alt+Tab
            App.SetWindowLong(helper, App.GWL_EX_STYLE, (App.GetWindowLong(helper, App.GWL_EX_STYLE) | App.WS_EX_TOOLWINDOW) & ~App.WS_EX_APPWINDOW);

            this.Top = Properties.Settings.Default.SystemMonitorTop;
            this.Left = Properties.Settings.Default.SystemMonitorLeft;
            this.Width = Properties.Settings.Default.SystemMonitorWidth;
            this.Height = Properties.Settings.Default.SystemMonitorHeight;

            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);

            Properties.Settings.Default.SystemMonitorTop = this.Top;
            Properties.Settings.Default.SystemMonitorLeft = this.Left;
            Properties.Settings.Default.SystemMonitorHeight = this.Height;
            Properties.Settings.Default.SystemMonitorWidth = this.Width;
            Properties.Settings.Default.Save();

            bottomLine = new Line
            {
                Name = "BottomLine",
                Stroke = Brushes.White,
                StrokeThickness = 4,
                X1 = Settings.settingsInstance!.GetValue<UInt32>("Global", "IconMargins"),
                X2 = this.Width - Settings.settingsInstance!.GetValue<UInt32>("Global", "IconMargins"),
                Y1 = this.Height-8,
                Y2 = this.Height-8,
                Margin = new Thickness(0)
            };

            mainGrid.Children.Add(bottomLine);

            foreach (var grid in mainStackPanel.Children.OfType<Grid>())
            {
                foreach (var grid2 in grid.Children.OfType<Grid>())
                {
                    foreach (var progressBar in grid2.Children.OfType<ProgressBar>())
                    {
                        progressBar.Width = this.Width - driveEntryGrid.ColumnDefinitions[0].Width.Value - 20;
                        progressBar.HorizontalAlignment = HorizontalAlignment.Left;
                    }
                }
            }

            this.MinHeight = mainStackPanel.ActualHeight;
            this.MinWidth = 350;
        }

        private void SystemMonitor_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isResizing) return;
            if (EnteredResizing) return;
            if (App.settings == null) return;
            if (App.settings.IsDockLocked) return;
            ShiftWindowOntoScreenHelper.ShiftWindowOntoScreen(this);
            Properties.Settings.Default.SystemMonitorTop = this.Top;
            Properties.Settings.Default.SystemMonitorLeft = this.Left;
            Properties.Settings.Default.Save();
        }

        private void SystemMonitor_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isResizing) return;
            if (EnteredResizing) return;
            if (App.settings == null) return;
            if (App.settings.IsDockLocked) return;
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isResizing = true;
            startPoint = e.GetPosition(this);
            Mouse.Capture(resizeGripBottomRight);
        }

        private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                double deltaX = e.GetPosition(this).X - startPoint.X;
                double deltaY = e.GetPosition(this).Y - startPoint.Y;

                double newWidth = Width + deltaX;
                double newHeight = Height + deltaY;

                if (newWidth > MinWidth)
                    Width = newWidth;

                if (newHeight > MinHeight)
                    Height = newHeight;

                startPoint = e.GetPosition(this);

                foreach (var grid in mainStackPanel.Children.OfType<Grid>())
                {
                    foreach (var grid2 in grid.Children.OfType<Grid>())
                    {
                        foreach (var progressBar in grid2.Children.OfType<ProgressBar>())
                        {
                            progressBar.MaxWidth = this.Width - driveEntryGrid.ColumnDefinitions[0].Width.Value - 20;
                        }
                    }
                }
                foreach (var item in mainGrid.Children.OfType<Line>())
                {
                    item.X1 = Settings.settingsInstance!.GetValue<UInt32>("Global", "IconMargins");
                    item.X2 = this.Width - Settings.settingsInstance!.GetValue<UInt32>("Global", "IconMargins");
                    item.Y1 = this.Height - 8;
                    item.Y2 = this.Height - 8;
                }
            }
        }

        private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false;
            Mouse.Capture(null);

            Properties.Settings.Default.SystemMonitorHeight = this.Height;
            Properties.Settings.Default.SystemMonitorWidth = this.Width;
            Properties.Settings.Default.Save();
        }

        private void resizeGripBottomRight_MouseEnter(object sender, MouseEventArgs e)
        {
            EnteredResizing = true;
        }

        private void resizeGripBottomRight_MouseLeave(object sender, MouseEventArgs e)
        {
            EnteredResizing = false;
        }
    }
}
