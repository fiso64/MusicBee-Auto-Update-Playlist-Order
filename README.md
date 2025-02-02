## Auto Update Playlist Order Plugin for MusicBee

This MusicBee plugin automatically updates the "natural" play order of static playlists based on your custom configurations.

## Problem

MusicBee static playlists have a "natural play order" which is initially determined by the order tracks were added.  Sorting or shuffling playlists in MusicBee does not change this natural order. As explained in the MusicBee wiki:

> Every static playlist has an official track order, called its "natural order" or "playlist order", which by default is the order in which you added the tracks. When tracks are dragged to or sent to a playlist, they are always added to the end of Playlist Order. You can see the order if the # column is displayed in the main panel. Shuffling or sorting tracks will not change the natural order.
> If it is not in order, you can choose "Update Play Order" in the List menu and it will be renumbered to match the current order.

This can lead to unexpected results when exporting playlists (e.g., to m3u files) or syncing to external devices, as they will follow the natural play order, not the sorted view you see in MusicBee. You might have to manually use "List -> Update Play Order" for each playlist to fix this before syncing or exporting.

This plugin can automate this "Update Play Order" step whenever a playlist is modified.

## How it works

This plugin allows you to define sorting rules for specific playlists. When a configured playlist is updated (tracks added or removed), the plugin will:

1. **Apply your defined sort criteria.**
2. **Update the playlist's "natural play order"** to reflect the sorted order.

This ensures that the play order is always what you expect, even when exported or synced.

## Configuration

1.  Go to **Preferences > Plugins**.
2.  Find "Auto Update Playlist Order" and click **Configure**.
3.  In the configuration window:
    *   **Playlist Name**: Select a playlist from the dropdown.
        * Select "AllPlaylists" to define default sorting rules for all playlists.
        * Individual playlist configurations override the "AllPlaylists" default configuration.
    *   **Order Configuration**: Click the "Configure" button to set up sorting rules for the selected playlist.
        *   In the "Configure Playlist Order" window, you can add multiple sorting criteria. For each criterion:
            *   **Order By**: Choose a tag or file property to sort by (e.g., "Date Added", "Album", "Artist").
            *   **Descending**: Check if you want to sort in descending order.
            *   **Manual Order**: Select this to maintain the order in which tracks are added (ascending) or to add new tracks to the beginning of the playlist (descending).

**Notes:**
* You will need to update your configuration whenever you rename the playlists.
* Individual playlist configurations override the "AllPlaylists" default configuration.
* Use "AllPlaylists" to set default sorting rules for playlists that don't have specific configurations.
* Use "Manual Order (descending)" to automatically add new tracks to the beginning of a playlist.

## Usage

Once configured, the plugin will automatically update the play order of the specified playlists in the following situations:

*   **Playlist Updated**: When you add or remove tracks from a configured playlist.
*   **Configuration Changes**: When you modify the playlist configurations in the plugin's settings and click "Ok".
*   **Manual Update**: Click the "Update All" button in the configuration window to force an update of all configured playlists.

## Installation

**Note: The plugin is new and little testing has been done; it's recommended to back up your playlists first.**

1.  Download the the latest release [here](https://github.com/fiso64/MusicBee-Auto-Update-Playlist-Order/releases).
2.  Extract the files into the MusicBee Plugins folder (usually located at `MusicBee\Plugins`).
3.  Restart MusicBee.
4.  The plugin should now be available in Preferences > Plugins.
