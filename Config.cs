using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace MusicBeePlugin
{
    public class OrderItem
    {
        public string Order { get; set; }
        public bool Descending { get; set; }

        public OrderItem()
        {
        }

        public OrderItem(string order, bool descending)
        {
            Order = order;
            Descending = descending;
        }

        public OrderItem(OrderItem other)
        {
            Order = other.Order;
            Descending = other.Descending;
        }

        public override string ToString()
        {
            return $"{Order}{(Descending ? " (desc)" : "")}";
        }
    }

    public class OrdersConfig
    {
        public List<OrderItem> Orders { get; set; } = new List<OrderItem>();

        [JsonIgnore]
        public bool IsManualNormal => Orders.Count == 1 && Orders[0].Order == "ManualOrder" && !Orders[0].Descending;
        [JsonIgnore]
        public bool IsManualDescending => Orders.Count == 1 && Orders[0].Order == "ManualOrder" && Orders[0].Descending;

        public OrdersConfig()
        {
        }

        public OrdersConfig(OrdersConfig other)
        {
            Orders = other.Orders.Select(o => new OrderItem(o)).ToList();
        }

        public override string ToString()
        {
            return string.Join(", ", Orders.Select(o => o.ToString()));
        }

        public bool Equals(OrdersConfig config2)
        {
            if (this.Orders.Count != config2.Orders.Count) return false;

            for (int i = 0; i < this.Orders.Count; i++)
            {
                if (this.Orders[i].Order != config2.Orders[i].Order ||
                    this.Orders[i].Descending != config2.Orders[i].Descending)
                    return false;
            }

            return true;
        }
    }

    public class Config
    {
        public Dictionary<string, OrdersConfig> PlaylistConfig { get; set; } = new Dictionary<string, OrdersConfig>();

        public Config()
        {
        }

        public Config(Config other)
        {
            foreach (var kvp in other.PlaylistConfig)
            {
                PlaylistConfig[kvp.Key] = new OrdersConfig(kvp.Value);
            }
        }

        public OrdersConfig GetOrderConfigForPlaylist(string playlistName)
        {
            return PlaylistConfig.TryGetValue(playlistName, out var config) ? config : null;
        }

        public void SetOrderConfigForPlaylist(string playlistName, OrdersConfig config)
        {
            if (config != null && config.Orders.Any())
            {
                PlaylistConfig[playlistName] = config;
            }
            else
            {
                PlaylistConfig.Remove(playlistName);
            }
        }

        public HashSet<string> GetModifiedPlaylists(Config oldConfig)
        {
            var changedPlaylists = new HashSet<string>();

            foreach (var playlistName in PlaylistConfig.Keys.Union(oldConfig.PlaylistConfig.Keys))
            {
                var hasOldConfig = oldConfig.PlaylistConfig.TryGetValue(playlistName, out var oldOrders);
                var hasNewConfig = PlaylistConfig.TryGetValue(playlistName, out var newOrders);

                if (hasNewConfig && (!hasOldConfig || !oldOrders.Equals(newOrders)))
                {
                    changedPlaylists.Add(playlistName);
                }
                else if (hasOldConfig && !hasNewConfig && playlistName != "AllPlaylists" && PlaylistConfig.ContainsKey("AllPlaylists"))
                {
                    changedPlaylists.Add(playlistName);
                }
            }

            return changedPlaylists;
        }

        public static Config LoadFromPath(string configPath)
        {
            Config config;

            if (File.Exists(configPath))
            {
                try
                {
                    string json = File.ReadAllText(configPath);
                    config = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    config = new Config();
                }
            }
            else
            {
                config = new Config();
            }

            return config;
        }

        public static void SaveConfig(Config config, string configPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath));
                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
