using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public Dictionary<string, string> GetGlobalSettings
        {
            get
            {
                return sections["Global"];
            }
        }

        public void RemoveEntry(string key)
        {
            if (sections.TryGetValue(key, out Dictionary<string, string>? v))
            {
                sections.Remove(key);
            }
        }

        public void ToggleDockPositionLock()
        {
            if(Convert.ToBoolean(sections["Global"]["PositionLocked"]) == true)
                sections["Global"]["PositionLocked"] = "false";
            else
                sections["Global"]["PositionLocked"] = "true";
        }

        private void LoadSettings(string filePath)
        {
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

        public bool GetBoolean(string section, string key, bool defaultValue = false)
        {
            if (sections.TryGetValue(section, out var properties) &&
                properties.TryGetValue(key, out var value) &&
                bool.TryParse(value, out var result))
            {
                return result;
            }

            return defaultValue;
        }

        public string GetString(string section, string key, string defaultValue = "")
        {
            if (sections.TryGetValue(section, out var properties) &&
                properties.TryGetValue(key, out var value))
            {
                return value;
            }

            return defaultValue;
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

        public IconInfo[] GetIcons()
        {
            List<IconInfo> icons = new List<IconInfo>();

            foreach (var section in sections.Keys)
            {
                if (section.StartsWith("Icon"))
                {
                    string iconImage = GetString(section, "IconImage", "");
                    string iconImageHover = GetString(section, "IconImageHover", "");
                    string action = GetString(section, "Action", "");

                    icons.Add(new IconInfo
                    {
                        IconImage = iconImage,
                        IconImageHover = iconImageHover,
                        Action = action
                    });
                }
            }

            return icons.ToArray();
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
    }
}
