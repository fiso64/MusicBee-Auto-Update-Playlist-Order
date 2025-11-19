using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;


namespace MusicBeePlugin
{
    public partial class Plugin
    {
        private static MusicBeeApiInterface mbApi;
        private PluginInfo about = new PluginInfo();
        private static readonly object _configLock = new object();
        private static bool isConfigOpen = false;
        private string configPath;
        private Config config = new Config();
        private Dictionary<string, HashSet<string>> playlistIndex = new Dictionary<string, HashSet<string>>();
        private Dictionary<string, System.Threading.Timer> debouncedTimers = new Dictionary<string, System.Threading.Timer>();
        private FileSystemWatcher m3uWatcher;
        private const int DEBOUNCE_MS = 500;

        public Plugin()
        {
            // taken from https://github.com/sll552/DiscordBee/blob/master/DiscordBee.cs
            AppDomain.CurrentDomain.AssemblyResolve += (object _, ResolveEventArgs args) =>
            {
                string assemblyFile = args.Name.Contains(",")
                    ? args.Name.Substring(0, args.Name.IndexOf(','))
                    : args.Name;

                assemblyFile += ".dll";

                string absoluteFolder = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                string targetPath = Path.Combine(absoluteFolder, "mb_AutoUpdatePlaylistOrder", assemblyFile);

                try
                {
                    return Assembly.LoadFile(targetPath);
                }
                catch (Exception ex)
                {
                    return null;
                }
            };
        }

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
            about.VersionMinor = 3;
            about.Revision = 0;
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
                case NotificationType.PlaylistCreated:
                    // If listener is enabled, we ignore MB events for playlists (to avoid loops/redundancy),
                    // relying instead on the file system watcher.
                    if (!config.M3uFileListenerEnabled)
                    {
                        UpdatePlaylistPlayOrder(sourceFileUrl, config);
                    }
                    break;
            }
        }

        private void Startup()
        {
            var dataPath = mbApi.Setting_GetPersistentStoragePath();
            configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
            config = Config.LoadFromPath(configPath);

            LoadManualDescendingPlaylists();
            InitializeFileListener();

            mbApi.MB_RegisterCommand("Auto Update Playlist Order: Open Configuration", (a, b) => Configure(IntPtr.Zero));
            mbApi.MB_RegisterCommand("Auto Update Playlist Order: Update All Playlists", (a, b) => UpdatePlaylistsAll(config));
        }

        private void InitializeFileListener()
        {
            if (m3uWatcher != null)
            {
                m3uWatcher.EnableRaisingEvents = false;
                m3uWatcher.Dispose();
                m3uWatcher = null;
            }

            if (config.M3uFileListenerEnabled)
            {
                var allPlaylists = GetAllPlaylists();
                var m3uPaths = allPlaylists
                    .Where(p => p.Path.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) || p.Path.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                    .Select(p => p.Path)
                    .ToList();

                string commonRoot = GetCommonRootPath(m3uPaths);

                if (!string.IsNullOrEmpty(commonRoot) && Directory.Exists(commonRoot))
                {
                    m3uWatcher = new FileSystemWatcher(commonRoot);
                    m3uWatcher.IncludeSubdirectories = true;
                    m3uWatcher.Filter = "*.*"; 
                    // We filter in the event handler to support multiple extensions or use specific filter if possible. 
                    // FSW only supports one filter string. We can just watch all and filter in code, or typically just "*.m3u*".
                    
                    m3uWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime;
                    m3uWatcher.Changed += OnM3uFileChanged;
                    m3uWatcher.Created += OnM3uFileChanged;
                    m3uWatcher.EnableRaisingEvents = true;
                }
            }
        }

        private void OnM3uFileChanged(object sender, FileSystemEventArgs e)
        {
            if (!e.Name.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) && !e.Name.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                return;

            Console.WriteLine($"Detected change in file {e.Name}");
            // Map file path to playlist URL/Name
            var allPlaylists = GetAllPlaylists();
            var playlist = allPlaylists.FirstOrDefault(p => p.Path.Equals(e.FullPath, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(playlist.Path))
            {
                UpdatePlaylistPlayOrder(playlist.Path, config);
            }
        }

        public static string GetCommonRootPath(List<string> paths)
        {
            if (paths == null || paths.Count == 0) return string.Empty;

            string common = Path.GetDirectoryName(paths[0]);

            foreach (string path in paths.Skip(1))
            {
                while (!string.IsNullOrEmpty(common))
                {
                    if (path.StartsWith(common, StringComparison.OrdinalIgnoreCase) &&
                        (path.Length == common.Length || path[common.Length] == Path.DirectorySeparatorChar || common.EndsWith(Path.DirectorySeparatorChar.ToString())))
                    {
                        break;
                    }
                    common = Path.GetDirectoryName(common);
                }
                if (string.IsNullOrEmpty(common)) break;
            }
            return common ?? string.Empty;
        }

        private void LoadManualDescendingPlaylists()
        {
            var allPlaylists = GetAllPlaylists();
            foreach (var playlist in config.PlaylistConfig.Where(x => x.Value.IsManualDescending))
            {
                var playlistInfo = allPlaylists.FirstOrDefault(p => p.Name == playlist.Key);
                if (playlistInfo.Path != null)
                {
                    if (QueryPlaylistFiles(playlistInfo.Path, out string[] files))
                    {
                        playlistIndex[playlist.Key] = new HashSet<string>(files);
                    }
                }
            }
        }

        public bool Configure(IntPtr panelHandle)
        {
            lock (_configLock)
            {
                if (isConfigOpen) return true;
                isConfigOpen = true;
            }

            var thread = new Thread(() =>
            {
                try
                {
                    var dataPath = mbApi.Setting_GetPersistentStoragePath();
                    configPath = Path.Combine(dataPath, "mb_AutoUpdatePlayOrder", "config.json");
                    var formConfig = Config.LoadFromPath(configPath);

                    using (var configForm = new ConfigForm(mbApi, formConfig))
                    {
                        configForm.UpdateAllPlaylists += (cfg) => UpdatePlaylistsAll(cfg);

                        Application.Run(configForm);

                        if (configForm.DialogResult == DialogResult.OK)
                        {
                            var oldConfig = config;
                            var newConfig = configForm.GetConfig();
                            
                            Config.SaveConfig(newConfig, configPath);
                            
                            config = newConfig;

                            if (oldConfig.M3uFileListenerEnabled != newConfig.M3uFileListenerEnabled)
                            {
                                InitializeFileListener();
                            }

                            var changedPlaylists = newConfig.GetModifiedPlaylists(oldConfig);
                            UpdatePlaylistsChanged(oldConfig, newConfig, changedPlaylists);
                        }
                    }
                }
                finally
                {
                    lock (_configLock)
                    {
                        isConfigOpen = false;
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

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
                var playlistInfo = allPlaylists.FirstOrDefault(p => p.Name == changed);
                if (playlistInfo.Path != null)
                {
                    if (QueryPlaylistFiles(playlistInfo.Path, out string[] files))
                    {
                        playlistIndex[changed] = new HashSet<string>(files);
                    }
                }
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
                //if (!force && playlistIndex.TryGetValue(playlistName, out var previousFilesHashSet))
                //{
                //    mbApi.Playlist_QueryFilesEx(url, out string[] currentFiles);
                //    var currentFilesHashSet = new HashSet<string>(currentFiles);

                //    if (currentFilesHashSet.IsSubsetOf(previousFilesHashSet))
                //    {
                //        playlistIndex[playlistName] = currentFilesHashSet;
                //        return; // No new items have been added, no need to sort
                //    }
                //}

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
                lock (debouncedTimers)
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
                            lock (debouncedTimers)
                            {
                                if (debouncedTimers.TryGetValue(playlistName, out var t))
                                {
                                    t.Dispose();
                                    debouncedTimers.Remove(playlistName);
                                }
                            }
                        }
                    }, null, DEBOUNCE_MS, System.Threading.Timeout.Infinite);

                    debouncedTimers[playlistName] = timer;
                }
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
                if (QueryPlaylistFiles(playlistUrl, out string[] currentFiles))
                {
                    var previousFiles = playlistIndex[playlistName];
                    var newFiles = currentFiles.Where(f => !previousFiles.Contains(f)).ToArray();

                    playlistIndex[playlistName] = new HashSet<string>(currentFiles);

                    if (newFiles.Any())
                    {
                        Debug.WriteLine($"Prepending new files to playlist {playlistName}");
                        var existingFiles = currentFiles.Except(newFiles).ToList();
                        var result = newFiles.Concat(existingFiles).ToList();
                        SetPlaylistFiles(playlistUrl, result.ToArray());
                    }
                }
                
                return;
            }

            Debug.WriteLine($"Updating playlist {playlistName} with sort order {orderConfig}");

            if (!QueryPlaylistFiles(playlistUrl, out string[] files)) return;
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
                SetPlaylistFiles(playlistUrl, orderedFiles.ToArray());
            }
        }

        private bool QueryPlaylistFiles(string playlistPath, out string[] files)
        {
            if (config.M3uFileListenerEnabled && (playlistPath.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) || playlistPath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var lines = File.ReadAllLines(playlistPath);
                    var dir = Path.GetDirectoryName(playlistPath);
                    var result = new List<string>();
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#")) continue;

                        try
                        {
                            string path = trimmed;
                            // Handle file URI if present
                            if (path.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
                            {
                                try { path = new Uri(path).LocalPath; } catch { }
                            }

                            if (!Path.IsPathRooted(path))
                            {
                                path = Path.GetFullPath(Path.Combine(dir, path));
                            }
                            result.Add(path);
                        }
                        catch { }
                    }
                    files = result.ToArray();
                    return true;
                }
                catch
                {
                    files = new string[0];
                    return false;
                }
            }
            return mbApi.Playlist_QueryFilesEx(playlistPath, out files);
        }

        private bool SetPlaylistFiles(string playlistPath, string[] files)
        {
            if (config.M3uFileListenerEnabled && (playlistPath.EndsWith(".m3u", StringComparison.OrdinalIgnoreCase) || playlistPath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    var encoding = System.Text.Encoding.Default;
                    if (playlistPath.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase))
                        encoding = System.Text.Encoding.UTF8;

                    var newContentLines = new List<string>();
                    newContentLines.AddRange(files.Select(f => f.Replace('\\', '/')));

                    if (File.Exists(playlistPath))
                    {
                        var existingLines = File.ReadAllLines(playlistPath);
                        var existingNonEmpty = existingLines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
                        
                        if (existingNonEmpty.SequenceEqual(newContentLines))
                        {
                            return true;
                        }
                    }

                    using (var sw = new StreamWriter(playlistPath, false, encoding))
                    {
                        foreach (var line in newContentLines)
                        {
                            sw.WriteLine(line);
                        }
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error writing M3U: {ex.Message}");
                    return false;
                }
            }
            return mbApi.Playlist_SetFiles(playlistPath, files);
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

            if (order == "ManualOrder")
                return currentOrder;

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

        public static List<(string Name, string Path)> GetAllPlaylists(bool staticOnly=true)
        {
            var res = new List<(string Name, string Path)>();
            if (mbApi.Playlist_QueryPlaylists())
            {
                var path = mbApi.Playlist_QueryGetNextPlaylist();
                while (!string.IsNullOrEmpty(path))
                {
                    if (staticOnly && path.EndsWith(".xautopf"))
                    {
                        path = mbApi.Playlist_QueryGetNextPlaylist();
                        continue;
                    }
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
