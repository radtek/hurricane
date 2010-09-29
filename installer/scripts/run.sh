#!/bin/bash
# This script runs the gatorshare application.

print_help_and_exit() {
  printf "Usage: %s: [-scfv] \n" $(basename $0) >&2; 
  cat <<EOF
  -s  Run server.
  -c  Run client.
  -v  Verbose.
  -f  Run applications as foreground processes.
EOF
  exit 2
}

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
  ?) print_help_and_exit;;
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
mount_point="/mnt/gatorshare"

test ! $foreground && bg_suffix="&"

if [ "$run_server" ]; then
  echo "Running server..."
  MONO_OPTIONS=--debug 
  eval xsp2 --root "$server_bin" --verbose --nonstop $bg_suffix
fi

if [ "$run_client" ]; then
  if [[ `mount | grep "/dev/fuse"` ]]; then
    umount -vl "/dev/fuse"
  fi

  mkdir -p $proj_dir/client/var/shadow
  mkdir -p $mount_point 

  echo "Running client..."
  libdir=$client_bin
    
  # Make sure module loaded.
#   modprobe fuse
#   while [[ ! -e /dev/fuse ]]; do
#     sleep 1s
#   done

  eval LD_LIBRARY_PATH="${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}$libdir" \
    mono --debug "$client_bin/FushareApp.exe" -o allow_other -m "$mount_point" -s "$proj_dir/client/var/shadow" $bg_suffix

  # Make sure the file system is fully started.
  while [[ -z `mount | grep "/dev/fuse"` ]]; do
    echo "FUSE is not up yet. Sleeping 1s..."
    sleep 1s
  done

  if [ ! "$foreground" ]; then
    namespace=$(hostname)
    mkdir -p "$mount_point/bittorrent/$namespace"
  fi
fi
