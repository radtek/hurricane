#!/bin/bash

while getopts "bscv" optNames; do
case "$optNames" in
  b) build=1;;
  s) run_server=1;;
  c) run_client=1;;
  v) verbose=1;;
  esac
done

client_proj_name="FushareApp"
web_proj_name="Fushare.Web"
sln_dir="$PWD/../src"
client_dir="$sln_dir/$client_proj_name/bin/l4n"
web_dir="$sln_dir/$web_proj_name"
sln_bin="$sln_dir/../bin"

if [ "$build" ]; then
  echo "Building solution..."
  cd $sln_dir
  mdtool build -c:l4n
  if [ $? -ne 0 ]; then
    echo "Build failed. Exiting..."
    exit 1
  else
    echo "Build succeeded. Copying binaries to $sln_bin..."
    rm -rf "$sln_bin/*"
    cp -r $client_dir "$sln_bin/$client_proj_name"
    cp -r $web_dir "$sln_bin/$web_proj_name"
  fi
fi

if [ "$run_server" ]; then
  echo "Running server..."
  cd $web_dir
  MONO_OPTIONS=--debug xsp2
fi

if [ "$run_client" ]; then
  echo "Running client..."
  cp $sln_dir/"FushareApp/App.config" $client_dir"/FushareApp.exe.config" -v
  if [[ `mount | grep "/dev/fuse"` ]]; then
    sudo umount -vl "/dev/fuse"
  fi
  cd $client_dir
  if [ "$verbose" ]; then
    sudo MONO_TRACE_LISTENER=Console.Out### mono --debug FushareApp.exe -odebug -o allow_other -m "$HOME/ffs/vfs" -s "$HOME/fushare/client/var/shadow"
  else
    sudo mono --debug FushareApp.exe -o allow_other -m "$HOME/ffs/vfs" -s "$HOME/fushare/client/var/shadow"
  fi
fi
