#!/bin/bash
# This script builds and runs the solution in place.

print_help_and_exit() {
  printf "Usage: %s: [-bscv] \n" $(basename $0) >&2; 
  cat <<EOF
  -s  Run server.
  -c  Run client.
  -v  Verbose.
  -b  Build application
EOF
  exit 2
}

while getopts "bscv" optNames; do
case "$optNames" in
  b) build=1;;
  s) run_server=1;;
  c) run_client=1;;
  v) verbose=1;;
  ?) print_help_and_exit;;
  esac
done

client_proj_name="FushareApp"
web_proj_name="Fushare.Web"
app_name=gatorshare

my_path=$(readlink -f "$0")
sln_dir="$(dirname $my_path)/.."
sln_src="$sln_dir/src"
sln_bin="$sln_dir/bin"
sln_lib=$sln_dir/lib
sln_etc=$sln_dir/.etc

client_dir="$sln_src/$client_proj_name/bin/l4n"
web_dir="$sln_src/$web_proj_name"

if [ "$build" ]; then
  echo "Building solution..."
  cd $sln_src
  xbuild /p:Configuration=l4n
  if [ $? -ne 0 ]; then
    echo "Build failed. Exiting..."
    exit 1
  else
    echo "Build succeeded."
  fi
fi

if [ "$run_server" ]; then
  echo "Running server..."
  sudo MONO_OPTIONS=--debug xsp2 --root $web_dir
fi

if [ "$run_client" ]; then
  echo "Running client..."
  cp $sln_etc/FushareApp/FushareApp.exe.config $client_dir -v

  # Umount the mounting-point first if already mounted.
  if [[ `mount | grep "/dev/fuse"` ]]; then
    sudo umount -vl "/dev/fuse"
  fi

  # Prepare folders
  sudo mkdir -p /mnt/$app_name
  sudo mkdir -p /opt/$app_name/client/var/shadow
  sudo mkdir -p /opt/$app_name/server/var/cache/bittorrent

  libdir=$sln_lib
  if [ "$verbose" ]; then
    sudo LD_LIBRARY_PATH="${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}$libdir" \
      MONO_TRACE_LISTENER=Console.Out### mono --debug "$client_dir/FushareApp.exe" -odebug -o allow_other -m /mnt/$app_name -s /opt/$app_name/client/var/shadow
  else
    sudo LD_LIBRARY_PATH="${LD_LIBRARY_PATH:+$LD_LIBRARY_PATH:}$libdir" \
      mono --debug "$client_dir/FushareApp.exe" -o allow_other -m /mnt/$app_name -S /opt/$app_name/client/var/shadow -s
  fi
fi