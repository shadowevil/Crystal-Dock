using IWshRuntimeLibrary;
using System.Drawing;
using System.IO;
using File = System.IO.File;

namespace CrystalDock
{
    public class ShortcutInfo
    {
        public string TargetPath { get; private set; } = "";
        public string Arguments { get; private set; } = "";
        public string WorkingDirectory { get; private set; } = "";
        public string Description { get; private set; } = "";
        public Icon? _icon { get; private set; } = null;

        private ShortcutInfo()
        {
        }

        public static ShortcutInfo? FromShortcutFile(string shortcutPath)
        {
            if (!File.Exists(shortcutPath))
            {
                throw new FileNotFoundException("Shortcut file not found.", shortcutPath);
            }

            WshShell shell = new WshShell();
            IWshShortcut shortcut = (IWshShortcut)shell.CreateShortcut(shortcutPath);

            if (string.IsNullOrEmpty(shortcut.TargetPath))
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

                return null;
            }

            Icon? icon = Icon.ExtractAssociatedIcon(shortcut.TargetPath);

            ShortcutInfo info = new ShortcutInfo
            {
                TargetPath = shortcut.TargetPath,
                Arguments = shortcut.Arguments,
                WorkingDirectory = shortcut.WorkingDirectory,
                Description = shortcut.Description,
                _icon = icon
            };

            System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);

            return info;
        }
    }
}
