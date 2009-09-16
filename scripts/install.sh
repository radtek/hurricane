#!/bin/bash
# This script installs the gatorrent software.

namespace=$(hostname)

my_path=$(readlink -f "$0")
scripts_dir="$(dirname $my_path)"
proj_dir="$scripts_dir/.."
dest_folder="/opt/gatorrent"

sudo mkdir -p "$dest_folder"
sudo cp -rt "$dest_folder" "$proj_dir"

sudo sed "s/{BitTorrentManagerSelfNamespace}/$namespace/" -i "$dest_folder/server/etc/Fushare.config"

sudo ln -sf "$dest_folder/client/etc/FushareApp.exe.config" "$dest_folder/client/bin/FushareApp.exe.config" 
sudo ln -sf "$dest_folder/server/etc/Fushare.config" "$dest_folder/server/bin/Fushare.config"
