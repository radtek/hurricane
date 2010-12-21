#!/bin/bash
# This script installs the gatorshare software.

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

if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root" 1>&2
   exit 1
fi

iface=$1
namespace=$(hostname)

my_path=$(readlink -f "$0")
scripts_dir=$(dirname "$my_path")
proj_dir="$scripts_dir/.."

# Instance specific parameters.
dest_folder='/opt/gatorshare'
dest_folder_escaped='\/opt\/gatorshare'
gsserver_port=8080
dht_tracker_listening_port=23113
http_pins_listening_port=23114


mkdir -p "$dest_folder"
cp -rt "$dest_folder" "$proj_dir"

# Edit GatorShare.config file parameters based on the host information
sed "s/{BitTorrentManagerSelfNamespace}/$namespace/" -i "$dest_folder/server/etc/GatorShare.config"
sed "s/{DhtTrackerIFace}/$iface/" -i "$dest_folder/server/etc/GatorShare.config"
sed "s/{GatorShareHomeDir}/$dest_folder_escaped/" -i "$dest_folder/server/etc/GatorShare.config"
sed "s/{DhtTrackerListeningPort}/$dht_tracker_listening_port/" -i "$dest_folder/server/etc/GatorShare.config"
sed "s/{HttpPieceInfoServerListeningPort}/$http_pins_listening_port/" -i "$dest_folder/server/etc/GatorShare.config"
sed "s/{GSServerPort}/$gsserver_port/" -i "$dest_folder/server/etc/GatorShare.config"

# Edit GSClientApp.exe.config
sed "s/{GatorShareHomeDir}/$dest_folder_escaped/" -i "$dest_folder/client/etc/GSClientApp.exe.config"
sed "s/{GSServerPort}/$gsserver_port/" -i "$dest_folder/client/etc/GSClientApp.exe.config"

# Set up runtime configs.
ln -sf "$dest_folder/client/etc/GSClientApp.exe.config" "$dest_folder/client/bin/GSClientApp.exe.config" 
ln -sf "$dest_folder/server/etc/GatorShare.config" "$dest_folder/server/bin/GatorShare.config"

ln -sf "$dest_folder/client/etc/l4n.debug.config" "$dest_folder/client/etc/l4n.config"
ln -sf "$dest_folder/server/etc/l4n.debug.config" "$dest_folder/server/etc/l4n.config"

sed "s/{GatorShareHomeDir}/$dest_folder_escaped/" -i "$dest_folder/client/etc/l4n.config"
sed "s/{GatorShareHomeDir}/$dest_folder_escaped/" -i "$dest_folder/server/etc/l4n.config"

# Set up libraries
if [[ -n `uname -m | grep 64` ]]; then
  ln -sf $dest_folder/client/bin/libMonoFuseHelper.so.64 $dest_folder/client/bin/libMonoFuseHelper.so
  ln -sf $dest_folder/client/bin/libMonoPosixHelper.so.64 $dest_folder/client/bin/libMonoPosixHelper.so  
else 
  ln -sf $dest_folder/client/bin/libMonoFuseHelper.so.32 $dest_folder/client/bin/libMonoFuseHelper.so
  ln -sf $dest_folder/client/bin/libMonoPosixHelper.so.32 $dest_folder/client/bin/libMonoPosixHelper.so
fi