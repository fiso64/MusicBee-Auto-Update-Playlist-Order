using System;
using System.Collections.Generic;
using System.Linq;

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

        public HashSet<string> GetChangedPlaylists(Config oldConfig)
        {
            var changedPlaylists = new HashSet<string>();

            foreach (var playlistName in PlaylistConfig.Keys.Union(oldConfig.PlaylistConfig.Keys))
            {
                var hasOldConfig = oldConfig.PlaylistConfig.TryGetValue(playlistName, out var oldOrders);
                var hasNewConfig = PlaylistConfig.TryGetValue(playlistName, out var newOrders);

                if (!hasOldConfig || !hasNewConfig || !oldOrders.Equals(newOrders))
                {
                    changedPlaylists.Add(playlistName);
                }
            }

            return changedPlaylists;
        }
    }
}
