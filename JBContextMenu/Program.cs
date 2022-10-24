using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Management.Instrumentation;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
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
        public const string subCommandsRegistry = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell";
        
        
        //autostartstuff

        public static bool AutostartEnabled = false;
        
        public static int selection = 0;


        public static void getStartArgs(string[] args)
        {
            
            foreach (var arg in args)
            {
                Console.WriteLine("Arg: " + arg);
                if (arg == "-autostart")
                    AutostartEnabled = true;

                if (arg == "-mode1")
                    selection = 1;
                if (arg == "-mode2")
                    selection = 2;
            }
        }
        
        public static void Main(string[] args)
        {
            getStartArgs(args);
            
            bool input_success = false;
            if (!AutostartEnabled)
            {
                Console.WriteLine("----------------JBContextMenu----------------\n1)Create Items in Main Context Menu\n2)Create Items in SubMenu");
                Console.Write("\nSelect:");
                while (!input_success)
                {
                    try
                    {
                        string input =  Console.ReadLine();
                        selection = Convert.ToInt32(input);
                        input_success = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Wrong Input!");
                    }
                
                }
            }

            switch (selection)
            {
                case 1:
                    createInMainMenu();
                    break;
                case 2:
                    createInSubMenu();
                    break;
                default:
                    Console.WriteLine("No valid input");
                    Console.ReadKey();
                    Environment.Exit(0);
                    break;
            }

            
        }

        public static void createInMainMenu()
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
            if(!AutostartEnabled)
                Console.ReadKey();
        }
        public static void createInSubMenu()
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
                createEntriesAlt(file);
            }

            string toolboxpath = GetTargetFromShortcut(Path.Combine(finalpath, "JetBrains Toolbox.lnk"));
            Console.WriteLine(toolboxpath);
            Console.WriteLine();
            List<string> subcmd = new List<string>();
            foreach (var file in Files)
            {
                string name = Path.GetFileName(file);
                string DisplayName = name.Substring(0, name.Length - 4);
                subcmd.Add("JetBrains " + DisplayName);
            }

            writeToRegistryAlt(MenuType.FolderMenu, subcmd, toolboxpath + ",0");
            writeToRegistryAlt(MenuType.BackgroundMenu, subcmd, toolboxpath + ",0");
            writeToRegistryAlt(MenuType.FileMenu, subcmd, toolboxpath + ",0");
            Console.WriteLine("Press and Key to close...");
            if(!AutostartEnabled)
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
        static void createEntriesAlt(string path)
        {
            string name = Path.GetFileName(path);
            string exepath = GetTargetFromShortcut(path);
            string DisplayName = name.Substring(0, name.Length - 4);
            if (path != null)
            {
                Console.WriteLine("-------------------------" + DisplayName + "-------------------------");
                writeToRegistrySubCommands(MenuType.FileMenu, DisplayName, exepath, exepath + ",0");
                writeToRegistrySubCommands(MenuType.FolderMenu, DisplayName, exepath, exepath + ",0");
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
        public static void writeToRegistryAlt(MenuType type, List<string> subcommands, string icon)
        {
            
            string key;
            string subcommandSuffix = "";
            
            if (type == MenuType.FileMenu)
            {
                key = fileMenuRegistry;
                subcommandSuffix = "F";
            }
            else if (type == MenuType.BackgroundMenu)
            {
                key = bgMenuRegistry;
                subcommandSuffix = "D";
            }
            else if (type == MenuType.FolderMenu)
            {
                key = folderMenuRegistry;
                subcommandSuffix = "D";
            }
            else
            {
                key = fileMenuRegistry;
            }
            string subcommandsStr = "";
            foreach (var s in subcommands)
            {
                subcommandsStr = subcommandsStr + s + subcommandSuffix + ";";
            }
            subcommandsStr = subcommandsStr.Substring(0, subcommandsStr.Length - 1);

            using (var shellKey = Registry.ClassesRoot.OpenSubKey(key, true))
            {
                shellKey.CreateSubKey("JetBrains ToolBox");
                using (var menuEntryKey = shellKey.OpenSubKey("JetBrains ToolBox", true))
                {
                    menuEntryKey.SetValue("icon", icon);
                    menuEntryKey.SetValue("SubCommands", subcommandsStr); 
                }

            }
        }
        public static void writeToRegistrySubCommands(MenuType type, string text, string command, string icon)
        {
            string suffix;
            string indicator;
            if (type == MenuType.FileMenu)
            {
                suffix = "\"%1\"";
                indicator = "F";
            }
            else if (type == MenuType.FolderMenu)
            {
                suffix = "\"%V\"";
                indicator = "D";
            }
            else
            {
                suffix = "\"%1\"";
                indicator = "D";
            }
            RegistryKey myKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            myKey = myKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl);
            myKey.CreateSubKey("testplswork");
            
            using (RegistryKey BaseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
            {

                using(RegistryKey shellKey = BaseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
                {
                    shellKey.CreateSubKey("JetBrains " + text + indicator);
                    using (var menuEntryKey = shellKey.OpenSubKey("JetBrains " + text + indicator, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
                    {
                        menuEntryKey.SetValue("", "Open in " + text);
                        menuEntryKey.SetValue("icon", icon);
                        menuEntryKey.CreateSubKey("Command");
                        using (var commandKey = menuEntryKey.OpenSubKey("Command", RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryRights.FullControl))
                        {
                            commandKey.SetValue("", "\"" + command + "\" " + suffix);
                            Console.WriteLine(type + " for " + text + " has been created");
                        }
                    }
                }

                
            }



        }
        
    }
}