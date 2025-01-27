using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private static MusicBeeApiInterface mbApi;
        private PluginInfo about = new PluginInfo();
        private ConfigForm configForm;
        private string configPath;
        private Dictionary<string, List<(string Order, bool Descending)>> playlistConfig = new Dictionary<string, List<(string Order, bool Descending)>>();
        private Dictionary<string, HashSet<string>> playlistFilePaths = new Dictionary<string, HashSet<string>>();

        public PluginInfo Initialise(IntPtr apiInterfacePtr)
        {
            mbApi = new MusicBeeApiInterface();
            mbApi.Initialise(apiInterfacePtr);
            about.PluginInfoVersion = PluginInfoVersion;
            about.Name = "Auto Update Playlist Order";
            about.Description = "Allows to automatically update a playlist's play order";
            about.Author = "fiso64";
            about.TargetApplication = "";
            about.Type = PluginType.General;
            about.VersionMajor = 1;
            about.VersionMinor = 0;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = ReceiveNotificationFlags.TagEvents;
            about.ConfigurationPanelHeight = 0;
            return about;
        }

        public bool Configure(IntPtr panelHandle)
        {
            var dataPath = mbApi.Setting_GetPersistentStoragePath();
            configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
            LoadConfig();

            configForm = new ConfigForm(mbApi, playlistConfig);
            configForm.FormClosed += ConfigForm_FormClosed;
            configForm.ShowDialog();

            return true;
        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.PluginStartup:
                    Startup();
                    break;
                case NotificationType.PlaylistUpdated:
                    UpdatePlaylistPlayOrder(sourceFileUrl);
                    break;
            }
        }

        private void Startup()
        {
            var dataPath = mbApi.Setting_GetPersistentStoragePath();
            configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
            LoadConfig();
        }

        private void UpdatePlaylistPlayOrder(string url)
        {
            if (playlistConfig.Count == 0) return;

            var playlistName = mbApi.Playlist_GetName(url);
            if (!playlistConfig.ContainsKey(playlistName)) return;


            if (playlistFilePaths.TryGetValue(playlistName, out var previousFilesHashSet))
            {
                mbApi.Playlist_QueryFilesEx(url, out string[] currentFiles);
                var currentFilesHashSet = new HashSet<string>(currentFiles);
                
                if (currentFilesHashSet.IsSubsetOf(previousFilesHashSet))
                {
                    return; // No new items, no need to sort
                }
            }

            try
            {
                Debug.WriteLine($"Updating playlist {playlistName}");
                ProcessPlaylistUpdate(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing playlist {playlistName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ProcessPlaylistUpdate(string playlistUrl)
        {
            string playlistName = mbApi.Playlist_GetName(playlistUrl);
            if (!playlistConfig.ContainsKey(playlistName)) return;

            List<(string Order, bool Descending)> sortOrders = playlistConfig[playlistName];
            if (sortOrders.Count == 0) return;

            mbApi.Playlist_QueryFilesEx(playlistUrl, out string[] files);
            if (files == null || files.Length == 0) return;

            IOrderedEnumerable<string> orderedFiles = null;

            for (int i = 0; i < sortOrders.Count; i++)
            {
                var sortOrder = sortOrders[i];
                orderedFiles = ApplySortOrder(orderedFiles, files, sortOrder.Order, sortOrder.Descending);
            }

            if (orderedFiles != null)
            {
                playlistFilePaths[playlistName] = new HashSet<string>(orderedFiles);
                mbApi.Playlist_SetFiles(playlistUrl, orderedFiles.ToArray()); // This will send a PlaylistUpdated notification
            }
        }

        private IOrderedEnumerable<string> ApplySortOrder(IOrderedEnumerable<string> currentOrder, string[] files, string order, bool descending)
        {
            Func<string, object> sortKeySelector = file =>
            {
                if (Enum.TryParse(order, out FilePropertyType type))
                {
                    string propertyValue = mbApi.Library_GetFileProperty(file, type);
                    if (type == FilePropertyType.DateAdded || type == FilePropertyType.DateModified || type == FilePropertyType.LastPlayed)
                    {
                        if (DateTime.TryParse(propertyValue, out DateTime dateValue))
                        {
                            return dateValue;
                        }
                    }
                    return propertyValue;
                }
                else if (Enum.TryParse(order, out MetaDataType metaDataType))
                {
                    return mbApi.Library_GetFileTag(file, metaDataType);
                }
                return file;
            };

            if (currentOrder == null) // First sort in the chain - use OrderBy
            {
                return descending ? files.OrderByDescending(sortKeySelector) : files.OrderBy(sortKeySelector);
            }
            else // Subsequent sorts - use ThenBy
            {
                return descending ? currentOrder.ThenByDescending(sortKeySelector) : currentOrder.ThenBy(sortKeySelector);
            }
        }


        public static List<(string Name, string Path)> GetAllPlaylists()
        {
            var res = new List<(string Name, string Path)>();
            if (mbApi.Playlist_QueryPlaylists())
            {
                var path = mbApi.Playlist_QueryGetNextPlaylist();
                while (!string.IsNullOrEmpty(path))
                {
                    var name = mbApi.Playlist_GetName(path);
                    res.Add((name, path));
                    path = mbApi.Playlist_QueryGetNextPlaylist();
                }
            }
            return res;
        }

        private void LoadConfig()
        {
            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    playlistConfig = JsonConvert.DeserializeObject<Dictionary<string, List<(string Order, bool Descending)>>>(json) ?? new Dictionary<string, List<(string Order, bool Descending)>>();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    playlistConfig = new Dictionary<string, List<(string Order, bool Descending)>>();
                }
            }
            else
            {
                playlistConfig = new Dictionary<string, List<(string Order, bool Descending)>>();
            }
        }

        public void SaveConfig()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                string json = JsonConvert.SerializeObject(playlistConfig, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            var oldPlaylistConfig = playlistConfig;
            playlistConfig = configForm.GetPlaylistConfig();
            SaveConfig();

            var changedPlaylists = GetChangedPlaylists(oldPlaylistConfig, playlistConfig);
            var allPlaylists = GetAllPlaylists();

            foreach (var playlistName in changedPlaylists)
            {
                var playlistPath = allPlaylists.FirstOrDefault(p => p.Name == playlistName).Path;
                if (playlistPath != null)
                    UpdatePlaylistPlayOrder(playlistPath);
            }
        }

        private List<string> GetChangedPlaylists(Dictionary<string, List<(string Order, bool Descending)>> oldConfig, Dictionary<string, List<(string Order, bool Descending)>> newConfig)
        {
            bool compareSortOrders(List<(string Order, bool Descending)> list1, List<(string Order, bool Descending)> list2)
            {
                if (list1.Count != list2.Count) return false;
                for (int i = 0; i < list1.Count; i++)
                    if (list1[i].Order != list2[i].Order || list1[i].Descending != list2[i].Descending) return false;
                return true;
            }

            var changedPlaylists = new List<string>();

            foreach (var playlistName in newConfig.Keys)
            {
                if (!oldConfig.ContainsKey(playlistName))
                    changedPlaylists.Add(playlistName);
                else if (!compareSortOrders(oldConfig[playlistName], newConfig[playlistName]))
                    changedPlaylists.Add(playlistName);
            }
            return changedPlaylists;
        }

        public void SaveSettings()
        {
            // this does nothing, saving of settings is handled in the SaveConfig() call of ConfigForm
        }
    }
}