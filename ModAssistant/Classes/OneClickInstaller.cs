using Microsoft.Win32;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ModAssistant
{
    class OneClickInstaller
    {
        private static readonly string[] Protocols = new[] { "modelsaber", "beatsaver", "bsplaylist" };

        public static async Task InstallAsset(string link)
        {
            Uri uri = new Uri(link);
            if (!Protocols.Contains(uri.Scheme)) return;

            switch (uri.Scheme)
            {
                case "modelsaber":
                    await ModelSaber(uri);
                    break;
                case "beatsaver":
                    await BeatSaver(uri);
                    break;
                case "bsplaylist":
                    await Playlist(uri);
                    break;
            }
        }

        private static async Task BeatSaver(Uri uri)
        {
            string Key = uri.Host;
            await API.BeatSaver.GetFromKey(Key);
        }

        private static async Task ModelSaber(Uri uri)
        {
            await API.ModelSaber.GetModel(uri);
        }

        private static async Task Playlist(Uri uri)
        {
            await API.Playlists.DownloadAll(uri);
        }

        public static void Register(string Protocol, bool Background = false)
        {
            if (IsRegistered(Protocol) == true)
                return;
            try
            {
                if (Utils.IsAdmin)
                {
                    RegistryKey ProtocolKey = Registry.ClassesRoot.OpenSubKey(Protocol, true);
                    if (ProtocolKey == null)
                        ProtocolKey = Registry.ClassesRoot.CreateSubKey(Protocol, true);
                    RegistryKey CommandKey = ProtocolKey.CreateSubKey(@"shell\open\command", true);
                    if (CommandKey == null)
                        CommandKey = Registry.ClassesRoot.CreateSubKey(@"shell\open\command", true);

                    if (ProtocolKey.GetValue("OneClick-Provider", "").ToString() != "ModAssistant")
                    {
                        ProtocolKey.SetValue("URL Protocol", "", RegistryValueKind.String);
                        ProtocolKey.SetValue("OneClick-Provider", "ModAssistant", RegistryValueKind.String);
                        CommandKey.SetValue("", $"\"{Utils.ExePath}\" \"--install\" \"%1\"");
                    }

                    Utils.SendNotify(string.Format((string)Application.Current.FindResource("OneClick:ProtocolHandler:Registered"), Protocol));
                }
                else
                {
                    Utils.StartAsAdmin($"\"--register\" \"{Protocol}\"");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            if (Background)
                Application.Current.Shutdown();
            else
                Pages.Options.Instance.UpdateHandlerStatus();
        }

        public static void Unregister(string Protocol, bool Background = false)
        {
            if (IsRegistered(Protocol) == false)
                return;
            try
            {
                if (Utils.IsAdmin)
                {
                    using (RegistryKey ProtocolKey = Registry.ClassesRoot.OpenSubKey(Protocol, true))
                    {
                        if (ProtocolKey != null
                            && ProtocolKey.GetValue("OneClick-Provider", "").ToString() == "ModAssistant")
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(Protocol);
                        }
                    }

                    Utils.SendNotify(string.Format((string)Application.Current.FindResource("OneClick:ProtocolHandler:Unregistered"), Protocol));
                }
                else
                {
                    Utils.StartAsAdmin($"\"--unregister\" \"{Protocol}\"");
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            if (Background)
                Application.Current.Shutdown();
            else
                Pages.Options.Instance.UpdateHandlerStatus();
        }

        public static bool IsRegistered(string Protocol)
        {
            RegistryKey ProtocolKey = Registry.ClassesRoot.OpenSubKey(Protocol);
            if (ProtocolKey != null
                && ProtocolKey.GetValue("OneClick-Provider", "").ToString() == "ModAssistant")
                return true;
            else
                return false;
        }
    }
}
