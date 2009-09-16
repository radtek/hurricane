#!/bin/bash
# This script runs the gatorrent application.

while getopts "scfv" optNames; do
case "$optNames" in
  s) run_server=1;;
  c) run_client=1;;
  f) foreground=1;;
  v) verbose=1;;
  esac
done

if [ "$foreground" -a "$run_server" -a "$run_client" ]; then
  echo "Cannot run both client and server as foreground process. Exiting..."
  exit 1
fi

my_path=$(readlink -f "$0")
scripts_dir="$(dirname $my_path)"
proj_dir="$scripts_dir/.."
client_bin="$proj_dir/client/bin"
server_bin="$proj_dir/server/bin"
mount_point="/mnt/gatorrent"

if [ "$run_server" ]; then
  echo "Running server..."
  if [ "$foreground" ]; then
    sudo xsp2 --root "$server_bin"
  else
    sudo xsp2 --root "$server_bin" &
  fi
fi

if [ "$run_client" ]; then
  if [[ `mount | grep "/dev/fuse"` ]]; then
    sudo umount -vl "/dev/fuse"
  fi
  echo "Running client..."
  if [ "$foreground" ]; then
    sudo mono --debug "$client_bin/FushareApp.exe" -o allow_other -m "$mount_point" -s "$proj_dir/client/var/shadow"
  else
    sudo mono --debug "$client_bin/FushareApp.exe" -o allow_other -m "$mount_point" -s "$proj_dir/client/var/shadow" &
  fi
  if [ ! "$foreground" ]; then
    sleep 2s
    namespace=$(hostname)
    mkdir -p "$mount_point/bittorrent/$namespace"
  fi
fi
