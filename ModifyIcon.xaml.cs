using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Timer = System.Windows.Forms.Timer;

namespace CrystalDock
{
    /// <summary>
    /// Interaction logic for ModifyIcon.xaml
    /// </summary>
    public partial class ModifyIcon : Window, IDisposable
    {
        public IconInfo? iconInfo = null;
        private Timer refresher = new Timer();

        public ModifyIcon(string iconKey)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            iconInfo = Settings.settingsInstance?.GetIconInfo(iconKey);
            if(iconInfo == null)
            {
                this.DialogResult = false;
                this.Close();
            }
            // Load the image using a stream
            using (FileStream fileStream = new FileStream(Properties.Resources.IconFolder + iconInfo!.IconImage, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = fileStream;
                bitmapImage.EndInit();

                NormalIcon.Source = bitmapImage;
            }
            using (FileStream fileStream = new FileStream(Properties.Resources.IconFolder + iconInfo!.IconImageHover, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = fileStream;
                bitmapImage.EndInit();

                HoverIcon.Source = bitmapImage;
            }

            NormalIconTxt.Text = iconInfo.IconImage;
            HoverIconTxt.Text = iconInfo.IconImageHover;
            IconActionTxt.Text = iconInfo.Action;

            CancelBtn.Command = new RelayCommand(CancelBtn_Click);
            SaveBtn.Command = new RelayCommand(SaveBtn_Click);
            SaveBtn.IsEnabled = false;
            OpenIconFolder.Command = new RelayCommand(OpenIconFolder_Click);

            refresher.Tick += Refresher_Tick;
            refresher.Interval = 100;
            refresher.Enabled = true;
            refresher.Start();
        }

        private void OpenIconFolder_Click()
        {
            Process.Start("explorer.exe", Directory.GetCurrentDirectory() + "\\" + Properties.Resources.IconFolder);
        }

        private void Refresher_Tick(object? sender, EventArgs e)
        {
            if (!File.Exists(Properties.Resources.IconFolder + NormalIconTxt.Text))
            {
                using (FileStream fileStream = new FileStream(Properties.Resources.MissingIconPath, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();

                    NormalIcon.Source = bitmapImage;
                }
                SaveBtn.IsEnabled = false; return;
            }
            else
            {
                using (FileStream fileStream = new FileStream(Properties.Resources.IconFolder + NormalIconTxt.Text, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();

                    NormalIcon.Source = bitmapImage;
                }
            }

            if (!File.Exists(Properties.Resources.IconFolder + HoverIconTxt.Text))
            {
                using (FileStream fileStream = new FileStream(Properties.Resources.MissingIconPath, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();

                    HoverIcon.Source = bitmapImage;
                }
                SaveBtn.IsEnabled = false; return;
            }
            else
            {
                using (FileStream fileStream = new FileStream(Properties.Resources.IconFolder + HoverIconTxt.Text, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = fileStream;
                    bitmapImage.EndInit();

                    HoverIcon.Source = bitmapImage;
                }
            }

            if (!File.Exists(IconActionTxt.Text))
            { SaveBtn.IsEnabled = false; return; }
            SaveBtn.IsEnabled = true;
        }

        public void Dispose()
        {
            iconInfo = null;
            refresher.Stop();
            refresher.Dispose();
        }

        private void SaveBtn_Click()
        {
            iconInfo!.IconImage = NormalIconTxt.Text;
            iconInfo!.IconImageHover = HoverIconTxt.Text;
            iconInfo!.Action = IconActionTxt.Text;

            this.DialogResult = true;
            this.Close();
        }

        private void CancelBtn_Click()
        {
            this.DialogResult = false;
            this.Close();
        }
        private void ActionTxt_Changed(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox txt)
            {
                if (txt.Text.Contains("Placeholder")) return;
                if (!File.Exists(txt.Text))
                {
                    txt.BorderBrush = new SolidColorBrush(Colors.Red);
                } else
                {
                    txt.BorderBrush = new SolidColorBrush(Colors.Gray);
                }
            }
        }

        private void NormalIconTxt_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox txt)
            {
                if (txt.Text.Contains("Placeholder")) return;
                if (!File.Exists(Properties.Resources.IconFolder + txt.Text))
                {
                    txt.BorderBrush = new SolidColorBrush(Colors.Red);
                } else
                {
                    txt.BorderBrush = new SolidColorBrush(Colors.Gray);
                }
            }
        }
    }
}
