using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;


namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private static MusicBeeApiInterface mbApi;
        private PluginInfo about = new PluginInfo();
        private ConfigForm configForm;
        private string configPath;
        private Config config = new Config();
        private Dictionary<string, HashSet<string>> playlistIndex = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, System.Threading.Timer> debouncedTimers = new Dictionary<string, System.Threading.Timer>();
        private const int DEBOUNCE_MS = 500;

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
            about.VersionMinor = 2;
            about.Revision = 1;
            about.MinInterfaceVersion = MinInterfaceVersion;
            about.MinApiRevision = MinApiRevision;
            about.ReceiveNotifications = ReceiveNotificationFlags.TagEvents;
            about.ConfigurationPanelHeight = 0;
            return about;
        }

        public void ReceiveNotification(string sourceFileUrl, NotificationType type)
        {
            switch (type)
            {
                case NotificationType.PluginStartup:
                    Startup();
                    break;
                case NotificationType.PlaylistUpdated:
                    UpdatePlaylistPlayOrder(sourceFileUrl, config);
                    break;
            }
        }

        private void Startup()
        {
            var dataPath = mbApi.Setting_GetPersistentStoragePath();
            configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
            config = Config.LoadFromPath(configPath);

            LoadManualDescendingPlaylists();   
        }

        private void LoadManualDescendingPlaylists()
        {
            var allPlaylists = GetAllPlaylists();
            foreach (var playlist in config.PlaylistConfig.Where(x => x.Value.IsManualDescending))
            {
                var playlistPath = allPlaylists.FirstOrDefault(p => p.Name == playlist.Key).Path;
                mbApi.Playlist_QueryFilesEx(playlistPath, out string[] files);
                playlistIndex[playlist.Key] = new HashSet<string>(files);
            }
        }

        public bool Configure(IntPtr panelHandle)
        {
            var dataPath = mbApi.Setting_GetPersistentStoragePath();
            configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
            config = Config.LoadFromPath(configPath);

            configForm = new ConfigForm(mbApi, config);
            configForm.UpdateAllPlaylists += (formConfig) => UpdatePlaylistsAll(formConfig);

            DialogResult result = configForm.ShowDialog();

            if (result == DialogResult.OK)
            {
                var oldConfig = config;
                config = configForm.GetConfig();
                Config.SaveConfig(config, configPath);

                var changedPlaylists = config.GetModifiedPlaylists(oldConfig);
                UpdatePlaylistsChanged(oldConfig, config, changedPlaylists);
            }

            configForm.Dispose();
            configForm = null;

            return true;
        }

        private void UpdatePlaylistsAll(Config config)
        {
            UpdatePlaylistsChanged(config, config, config.PlaylistConfig.Keys.ToHashSet());
        }

        private void UpdatePlaylistsChanged(Config oldConfig, Config newConfig, HashSet<string> changedPlaylists)
        {
            var allPlaylists = GetAllPlaylists();
            var modifiedConfigPlaylists = changedPlaylists.Where(x => newConfig.PlaylistConfig.ContainsKey(x));

            foreach (var changed in modifiedConfigPlaylists.Where(x => newConfig.PlaylistConfig[x].IsManualDescending))
            {
                var playlistPath = allPlaylists.FirstOrDefault(p => p.Name == changed).Path;
                mbApi.Playlist_QueryFilesEx(playlistPath, out string[] files);
                playlistIndex[changed] = new HashSet<string>(files);
            }

            if (changedPlaylists.Contains("AllPlaylists"))
            {
                foreach (var playlist in allPlaylists)
                {
                    if (!newConfig.PlaylistConfig.ContainsKey(playlist.Name))
                        UpdatePlaylistPlayOrder(playlist.Path, newConfig, force: true);
                }
            }

            foreach (var playlistName in changedPlaylists.Where(p => p != "AllPlaylists"))
            {
                var playlistPath = allPlaylists.FirstOrDefault(p => p.Name == playlistName).Path;
                if (playlistPath != null)
                    UpdatePlaylistPlayOrder(playlistPath, newConfig, force: true);
            }
        }

        private void UpdatePlaylistPlayOrder(string url, Config config, bool force = false)
        {
            if (config.PlaylistConfig.Count == 0) 
                return;

            var playlistName = mbApi.Playlist_GetName(url);

            void ProcessWithErrorHandling()
            {
                if (!force && playlistIndex.TryGetValue(playlistName, out var previousFilesHashSet))
                {
                    mbApi.Playlist_QueryFilesEx(url, out string[] currentFiles);
                    var currentFilesHashSet = new HashSet<string>(currentFiles);

                    if (currentFilesHashSet.IsSubsetOf(previousFilesHashSet))
                    {
                        playlistIndex[playlistName] = currentFilesHashSet;
                        return; // No new items have been added, no need to sort
                    }
                }

                try
                {
                    ProcessPlaylistUpdate(url, config);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error processing playlist {playlistName}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            if (!force && DEBOUNCE_MS > 0)
            {
                if (debouncedTimers.TryGetValue(playlistName, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                var timer = new System.Threading.Timer(_ =>
                {
                    try 
                    {
                        ProcessWithErrorHandling();
                    }
                    finally
                    {
                        if (debouncedTimers.TryGetValue(playlistName, out var t))
                        {
                            t.Dispose();
                            debouncedTimers.Remove(playlistName);
                        }
                    }
                }, null, DEBOUNCE_MS, System.Threading.Timeout.Infinite);

                debouncedTimers[playlistName] = timer;
                return;
            }

            ProcessWithErrorHandling();
        }

        private void ProcessPlaylistUpdate(string playlistUrl, Config config)
        {
            string playlistName = mbApi.Playlist_GetName(playlistUrl);

            if (!config.PlaylistConfig.TryGetValue(playlistName, out OrdersConfig orderConfig) &&
                config.PlaylistConfig.TryGetValue("AllPlaylists", out OrdersConfig allPlaylistsConfig))
            {
                orderConfig = allPlaylistsConfig;
            }
            
            if (orderConfig == null)
                return;

            if (orderConfig.Orders.Count == 0 || orderConfig.IsManualNormal)
                return;

            if (orderConfig.IsManualDescending)
            {
                mbApi.Playlist_QueryFilesEx(playlistUrl, out string[] currentFiles);

                var previousFiles = playlistIndex[playlistName];
                var newFiles = currentFiles.Where(f => !previousFiles.Contains(f)).ToArray();
                
                playlistIndex[playlistName] = new HashSet<string>(currentFiles);
                
                if (newFiles.Any())
                {
                    Debug.WriteLine($"Prepending new files to playlist {playlistName}");
                    var existingFiles = currentFiles.Except(newFiles).ToList();
                    var result = newFiles.Concat(existingFiles).ToList();
                    mbApi.Playlist_SetFiles(playlistUrl, result.ToArray());
                }
                
                return;
            }

            Debug.WriteLine($"Updating playlist {playlistName} with sort order {orderConfig}");

            mbApi.Playlist_QueryFilesEx(playlistUrl, out string[] files);
            if (files == null || files.Length == 0) return;

            IOrderedEnumerable<string> orderedFiles = null;

            for (int i = 0; i < orderConfig.Orders.Count; i++)
            {
                var sortOrder = orderConfig.Orders[i];
                orderedFiles = ApplySortOrder(orderedFiles, files, sortOrder.Order, sortOrder.Descending);
            }

            if (orderedFiles != null)
            {
                playlistIndex[playlistName] = new HashSet<string>(orderedFiles);
                mbApi.Playlist_SetFiles(playlistUrl, orderedFiles.ToArray()); // This will send a PlaylistUpdated notification
            }
        }

        private IOrderedEnumerable<string> ApplySortOrder(IOrderedEnumerable<string> currentOrder, string[] files, string order, bool descending)
        {
            string getDate(string s)
            {
                var parts = s.Split(new[] { ' ' }, 2);
                if (int.TryParse(parts[0], out int year))
                {
                    var yearDate = new DateTime(year, 1, 1);
                    return yearDate.Ticks.ToString("D19") + (parts.Length > 1 ? " " + parts[1] : "");
                }
                if (DateTime.TryParse(parts[0], out DateTime date))
                {
                    return date.Ticks.ToString("D19") + (parts.Length > 1 ? " " + parts[1] : "");
                }
                return s;
            }

            FilePropertyType filePropertyType = 0;
            MetaDataType metaDataType = 0;
            
            bool isFileProperty = Enum.TryParse(order, out filePropertyType);
            bool isMetaData = !isFileProperty && Enum.TryParse(order, out metaDataType);

            if (!isFileProperty && !isMetaData)
            {
                throw new Exception($"Invalid order type {order}");
            }

            // Note: Linq is already optimized for slow key selectors
            object sortKeySelector(string file)
            {
                if (isFileProperty)
                {
                    string propertyValue = mbApi.Library_GetFileProperty(file, filePropertyType);
                    if (filePropertyType == FilePropertyType.DateAdded ||
                        filePropertyType == FilePropertyType.DateModified ||
                        filePropertyType == FilePropertyType.LastPlayed)
                    {
                        if (DateTime.TryParse(propertyValue, out DateTime dateValue))
                        {
                            return dateValue;
                        }
                    }
                    return propertyValue;
                }
                else
                {
                    string value = mbApi.Library_GetFileTag(file, metaDataType);
                    if (metaDataType == MetaDataType.Year || metaDataType == MetaDataType.OriginalYear)
                    {
                        return getDate(value);
                    }
                    else if (metaDataType == MetaDataType.TrackCount ||
                             metaDataType == MetaDataType.DiscCount ||
                             metaDataType == MetaDataType.TrackNo ||
                             metaDataType == MetaDataType.DiscNo)
                    {
                        return int.TryParse(value, out int numValue) ? numValue : 0;
                    }
                    return value;
                }
            }

            if (currentOrder == null)
            {
                return descending ? files.OrderByDescending(sortKeySelector) : files.OrderBy(sortKeySelector);
            }
            else
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

        public void SaveSettings()
        {
            // this does nothing, saving of settings is handled in the SaveConfig() call of ConfigForm
        }
    }
}
