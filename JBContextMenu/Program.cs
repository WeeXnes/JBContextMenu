using System;
using System.Collections.Generic;
using System.IO;
using IWshRuntimeLibrary;
using Microsoft.Win32;
using File = System.IO.File;

namespace JBContextMenu
{
    internal class Program
    {
        public const string LinkPath = @"Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox";
        
        public const string fileMenuRegistry = @"\*\shell";
        public const string bgMenuRegistry = @"\Directory\Background\shell";
        public const string folderMenuRegistry = @"\Directory\shell";
        
        
        
        public static void Main(string[] args)
        {
            string appdatapath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string subpath = LinkPath;
            string finalpath = Path.Combine(appdatapath, subpath);
            string [] fileEntries = Directory.GetFiles(finalpath);
            List<string> Files = new List<string>();
            foreach (string s in fileEntries)
            {
                if (Path.GetFileName(s) != "JetBrains Toolbox.lnk")
                {
                    Files.Add(s);
                }
            }

            foreach (var file in Files)
            {
                createEntries(file);
            }

            Console.WriteLine("Press and Key to close...");
            Console.ReadKey();
        }

        static void createEntries(string path)
        {
            string name = Path.GetFileName(path);
            string exepath = GetTargetFromShortcut(path);
            string DisplayName = name.Substring(0, name.Length - 4);
            if (path != null)
            {
                Console.WriteLine("-------------------------" + DisplayName + "-------------------------");
                writeToRegistry(MenuType.FileMenu, DisplayName, exepath, exepath + ",0");
                writeToRegistry(MenuType.BackgroundMenu, DisplayName, exepath, exepath + ",0");
                writeToRegistry(MenuType.FolderMenu, DisplayName, exepath, exepath + ",0");
            }
        }

        public static string GetTargetFromShortcut(string filename)
        {
            if (System.IO.File.Exists(filename))
            {
                IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
                IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(filename);
                return link.TargetPath;
            }

            return null;
        }
        
        public enum MenuType
        {
            FileMenu,
            BackgroundMenu,
            FolderMenu
        }
        public static void writeToRegistry(MenuType type, string text, string command, string icon)
        {
            string key;
            string suffix;
            
            if (type == MenuType.FileMenu)
            {
                key = fileMenuRegistry;
                suffix = "\"%1\"";
            }
            else if (type == MenuType.BackgroundMenu)
            {
                key = bgMenuRegistry;
                suffix = "\"%V\"";
            }
            else if (type == MenuType.FolderMenu)
            {
                key = folderMenuRegistry;
                suffix = "\"%V\"";
            }
            else
            {
                key = fileMenuRegistry;
                suffix = "\"%1\"";
            }

            using (var shellKey = Registry.ClassesRoot.OpenSubKey(key, true))
            {
                
                shellKey.CreateSubKey("JetBrains " + text);
                using (var menuEntryKey = shellKey.OpenSubKey("JetBrains " + text, true))
                {
                    menuEntryKey.SetValue("", "Open in " + text);
                    menuEntryKey.SetValue("icon", icon);
                    menuEntryKey.CreateSubKey("Command");
                    using (var commandKey = menuEntryKey.OpenSubKey("Command", true))
                    {
                        commandKey.SetValue("", "\"" + command + "\" " + suffix);
                        Console.WriteLine(type + " for " + text + " has been created");
                    }
                }

            }
        }
    }
}