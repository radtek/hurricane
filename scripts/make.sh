#!/bin/bash
# This script builds the gatorshare project and copies binaries to make a package.

print_help_and_exit() {
  printf "Usage: %s: [-bvc] \n" $(basename $0) >&2; 
  cat <<EOF
  -b Build application.
  -v Verbose.
  -c Compress and make a tarball.
  -a Make a tarball.
  -d Bundle the compiled artifacts.
EOF
  exit 2
}

if [[ -z $1 ]]; then
  print_help_and_exit
fi

while getopts "bvcda" optNames; do
case "$optNames" in
  b) build=1;;
  v) verbose=1;;
  c) compress=1;;
  a) archive=1;;
  d) bundle=1;;
  ?) print_help_and_exit;;
  esac
done

my_path=$(readlink -f "$0")
scripts_dir="$(dirname $my_path)"
installer_dir="$scripts_dir/.."
gatorshare_sln="$installer_dir/.."
gatorshare_scripts="$gatorshare_sln/scripts"
gatorshare_lib="$gatorshare_sln/lib"
gatorshare_src="$gatorshare_sln/src"

if [ "$build" ]; then
  echo "Building Solution..."
  "$gatorshare_scripts/make_sln.sh" -b
  if [ $? -ne 0 ]; then
    echo "Build failed. Exiting..."
    exit 1
  fi

  client_proj_name="GSClientApp"
  web_proj_name="GatorShare.Web"
  client_dir="$gatorshare_src/$client_proj_name/bin/l4n"
  web_dir="$gatorshare_src/$web_proj_name"

  rsync -avLm --delete --include-from="$scripts_dir/rsync.rules" --exclude-from="$scripts_dir/rsync.rules" "$client_dir/" "$installer_dir/client/bin/"
  rsync -avLm --delete --include-from="$scripts_dir/rsync.rules" --exclude-from="$scripts_dir/rsync.rules" "$web_dir/" "$installer_dir/server/bin/"
  rsync -avLm --delete $gatorshare_lib/libMonoFuseHelper.so.* "$installer_dir/client/bin/"
  rsync -avLm --delete $gatorshare_lib/libMonoPosixHelper.so.* "$installer_dir/client/bin/"
fi

if [[ $bundle ]]; then
  $scripts_dir/makebundle.py
fi 

if [[ $archive ]] || [[ $compress ]]; then
  if [[ $bundle ]]; then
    # Make a tarball gatorshare.tar.gz with gatorshare as the root directory inside.
    tar -C $gatorshare_sln -cvf "$installer_dir/../gshare.tar" "./installer" --exclude="*~"
  else
    # Make a tarball gatorshare.tar.gz with gatorshare as the root directory inside.
    tar -C $gatorshare_sln -cvf "$installer_dir/../gshare.tar" "./installer" --exclude="*~" --exclude="*bundle"
  fi

  if [[ $compress ]]; then
    gzip -f "$installer_dir/../gshare.tar"
  fi 
fi