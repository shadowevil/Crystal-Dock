using CrystalDock.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CrystalDock
{
    public class Settings
    {
        private Dictionary<string, Dictionary<string, string>> sections;

        public Settings(string filePath)
        {
            sections = new Dictionary<string, Dictionary<string, string>>();
            LoadSettings(filePath);
        }

        public void RemoveEntry(string key)
        {
            if (IsDockLocked) return;
            if (sections.TryGetValue(key, out Dictionary<string, string>? v))
            {
                if (File.Exists(Properties.Resources.IconFolder + v["IconImage"]))
                {
                    File.Delete(Properties.Resources.IconFolder + v["IconImage"]);
                    File.Delete(Properties.Resources.IconFolder + v["IconImageHover"]);
                }
                sections.Remove(key);
            }
            MainWindow._instance?.LoadButtons();
        }

        public void AddEntry(IconInfo info)
        {
            sections.Add(NextEntry, info.ToDictionary());
        }

        public string NextEntry
        {
            get
            {
                int count = GetIconEntries().Keys.Where(x => x.StartsWith("Icon")).Count();
                while (sections.TryGetValue("Icon" + count.ToString(), out var value)) count++;
                return "Icon" + count.ToString();
            }
        }

        public bool IsDockLocked => GetValue<bool>("Global", "PositionLocked");

        public void ToggleDockPositionLock()
        {
            if(IsDockLocked == true)
                sections["Global"]["PositionLocked"] = "false";
            else
                sections["Global"]["PositionLocked"] = "true";
        }

        private void LoadSettings(string filePath)
        {
            if(!File.Exists(filePath))
            {
                File.CreateText(filePath).Close();

                List<string> _lines = new List<string>
                {
                    "[Global]",
                    "PositionLocked=" + Properties.Settings.Default.PositionLocked,
                    "IconSize=" + Properties.Settings.Default.IconSize,
                    "IconMargins=" + Properties.Settings.Default.IconMargins
                };

                File.WriteAllLines(filePath, _lines);
            }

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            string? currentSection = null;
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(";"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                    sections[currentSection] = new Dictionary<string, string>();
                }
                else if (currentSection != null)
                {
                    string[] parts = line.Split(new char[] { '=' }, 2);
                    if (parts.Length == 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1].Trim().Trim('\"');
                        sections[currentSection][key] = value;
                    }
                }
            }
        }

        public T? GetValue<T>(string section, string key)
        {
            if(sections.TryGetValue(section, out var properties) &&
                properties.TryGetValue(key, out var value))
            {
                return (T?)TypeDescriptor.GetConverter(typeof(T))?.ConvertFromString(value);
            }
            return default(T?);
        }

        public Dictionary<string, IconInfo> GetIconEntries()
        {
            Dictionary<string, IconInfo> rtn = new Dictionary<string, IconInfo>();
            var iconEntries = sections.Where(x => x.Key != "Global");
            foreach(var iconEntry in iconEntries)
            {
                rtn.Add(iconEntry.Key, new IconInfo
                {
                    IconImage = iconEntry.Value["IconImage"],
                    IconImageHover = iconEntry.Value["IconImageHover"],
                    Action = iconEntry.Value["Action"]
                });
            }
            return rtn;
        }

        public void SaveSettings()
        {
            List<string> lines = new List<string>();

            foreach (var section in sections)
            {
                lines.Add($"[{section.Key}]");
                foreach (var property in section.Value)
                {
                    if (property.Key == "IconImage" || property.Key == "IconImageHover" || property.Key == "Action")
                    {
                        lines.Add($"{property.Key}=\"{property.Value}\"");
                    }
                    else
                    {
                        lines.Add($"{property.Key}={property.Value}");
                    }
                }
            }

            File.WriteAllLines(Properties.Resources.SettingsIniFile, lines, Encoding.UTF8);
        }
    }

    public class IconInfo
    {
        public string IconImage { get; set; } = "";
        public string IconImageHover { get; set; } = "";
        public string Action { get; set; } = "";

        public Dictionary<string, string> ToDictionary()
        {
            return new Dictionary<string, string>()
            {
                { "IconImage", IconImage },
                { "IconImageHover", IconImageHover },
                { "Action", Action }
            };
        }
    }
}
