#!/bin/bash
# This script runs the gatorrent application.

if [[ $EUID -ne 0 ]]; then
   echo "This script must be run as root" 1>&2
   exit 1
fi

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
    xsp2 --root "$server_bin"
  else
    xsp2 --root "$server_bin" --nonstop &
  fi
fi

if [ "$run_client" ]; then
  if [[ `mount | grep "/dev/fuse"` ]]; then
    umount -vl "/dev/fuse"
  fi
  mkdir -p "$proj_dir/client/var/shadow"
  echo "Running client..."
  libdir="../client/bin"
  if [ "$foreground" ]; then
    LD_LIBRARY_PATH="${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}$libdir" \
    mono --debug "$client_bin/FushareApp.exe" -o allow_other -m "$mount_point" -s "$proj_dir/client/var/shadow"
  else
    LD_LIBRARY_PATH="${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}$libdir" \
    mono --debug "$client_bin/FushareApp.exe" -o allow_other -m "$mount_point" -s "$proj_dir/client/var/shadow" &
  fi
  if [ ! "$foreground" ]; then
    # Make sure the file system is fully started.
    sleep 2s
    namespace=$(hostname)
    mkdir -p "$mount_point/bittorrent/$namespace"
  fi
fi
