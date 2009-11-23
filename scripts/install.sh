#!/bin/bash
# This script installs the gatorrent software.

print_help_and_exit() {
  printf "Usage: %s: iface \n" $(basename $0) >&2; 
  cat <<EOF
  iface The interface to run server on.
EOF
  exit 2
}

if [[ -z $1 ]]; then
  print_help_and_exit
fi

iface=$1
namespace=$(hostname)

my_path=$(readlink -f "$0")
scripts_dir="$(dirname $my_path)"
proj_dir="$scripts_dir/.."
dest_folder="/opt/gatorrent"

sudo mkdir -p "$dest_folder"
sudo cp -rt "$dest_folder" "$proj_dir"

sudo sed "s/{BitTorrentManagerSelfNamespace}/$namespace/" -i "$dest_folder/server/etc/Fushare.config"
sudo sed "s/{DhtTrackerIFace}/$iface/" -i "$dest_folder/server/etc/Fushare.config"

sudo ln -sf "$dest_folder/client/etc/FushareApp.exe.config" "$dest_folder/client/bin/FushareApp.exe.config" 
sudo ln -sf "$dest_folder/server/etc/Fushare.config" "$dest_folder/server/bin/Fushare.config"
