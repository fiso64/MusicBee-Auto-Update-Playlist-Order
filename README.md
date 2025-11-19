## Auto Update Playlist Order Plugin for MusicBee

This MusicBee plugin automatically updates the "natural" play order of static playlists based on your custom configurations.

There are two primary reasons why you might need this plugin:

## Problem #1

If you configured MusicBee to store playlists as M3U files instead of the proprietary MBP format (maybe because you want to sync playlists from elsewhere), it is not possible to set per-playlist sorting. It is also not possible to make MusicBee use forward slashes for these (non-exported) M3U playlists.

## Problem #2

If you store playlists in MBP format and use the "auto-export static copy" feature, and if your playlists are not sorted in manual order, then the exported copy will not automatically update its order to match the sorted view in MusicBee.

MusicBee playlists have a "natural play order" which is initially determined by the order tracks were added. Sorting or shuffling playlists in MusicBee does not change this natural order. As explained in the MusicBee wiki:

> Every static playlist has an official track order, called its "natural order" or "playlist order", which by default is the order in which you added the tracks. When tracks are dragged to or sent to a playlist, they are always added to the end of Playlist Order. You can see the order if the # column is displayed in the main panel. Shuffling or sorting tracks will not change the natural order.
> If it is not in order, you can choose "Update Play Order" in the List menu and it will be renumbered to match the current order.

This can lead to unexpected results when exporting playlists (e.g., to m3u files) or syncing to external devices, as they will follow the natural play order, not the sorted view you see in MusicBee. You have to manually use "List -> Update Play Order" for each playlist to fix this before syncing or exporting.

This plugin can automate this "Update Play Order" step whenever a playlist is modified.

## How it works

Go to **Preferences > Plugins**. Find "Auto Update Playlist Order" and click **Configure**.

This plugin allows you to define sorting rules for specific playlists. When a configured playlist is updated (tracks added or removed), the plugin will update the playlist's "natural play order".

This ensures that the play order is always what you expect, even when exported or synced.

**Note:** You will need to update your configuration whenever you rename the playlists.

**Filesystem watcher mode:**
If you have configured your playlists to be stored in M3U format (e.g., syncing), then via the cogwheel in the settings page you can enable the file system watcher mode. In this mode, the plugin will detect and re-sort playlists even when they're modified externally. You can also enforce forward slashes for all M3U playlists to ensure cross-platform compatibility.

## Installation

1.  Download the the latest release [here](https://github.com/fiso64/MusicBee-Auto-Update-Playlist-Order/releases).
2.  Extract the files into the MusicBee Plugins folder (usually located at `MusicBee\Plugins`).
3.  Restart MusicBee.
4.  The plugin should now be available in Preferences > Plugins.
