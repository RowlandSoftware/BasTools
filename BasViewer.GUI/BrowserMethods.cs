using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BasViewer.GUI
{
    internal static class CBrowserMethods
    {
        public static void OpenURL(string Url)
        {
            if (string.IsNullOrEmpty(Url)) return;

            System.Diagnostics.Process.Start(getDefaultBrowserPath(), Url);
        }
        private static string getDefaultBrowserPath()
        {
            string browserPath = string.Empty;
            using (RegistryKey? userChoiceKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.html\UserChoice"))
            //using (RegistryKey ?userChoiceKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\Shell\Associations\UrlAssociations\https\UserChoice"))
            {
                //MessageBox.Show(userChoiceKey.ToString());
                if (userChoiceKey != null)
                {
                    object? progIdValue = userChoiceKey.GetValue("Progid");
                    if (progIdValue != null)
                    {
                        //MessageBox.Show((string)progIdValue);
                        using (RegistryKey? browserCmd = Registry.ClassesRoot.OpenSubKey(progIdValue.ToString() + @"\shell\open\command"))
                        {
                            if (browserCmd != null)
                            {
                                object? temp = browserCmd.GetValue("");
                                //if (temp != null)
                                browserPath = temp.ToString() ?? string.Empty;
                                browserCmd.Close();
                            }
                        }
                    }
                    userChoiceKey.Close();
                }
            }
            if (browserPath != string.Empty)
            {
                if (browserPath.IndexOf(@"%1") > 0)
                {
                    browserPath = browserPath.Substring(0, browserPath.LastIndexOf(" "));
                }
                if (browserPath.IndexOf("-") > 0)
                {
                    browserPath = browserPath.Substring(0, browserPath.IndexOf("-")).Trim();
                }
            }
            return browserPath;
        }
    }

}
