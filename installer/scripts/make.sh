#!/bin/bash
# This script builds the gatorshare project and copies binaries to make a package.

print_help_and_exit() {
  printf "Usage: %s: [-bvc] \n" $(basename $0) >&2; 
  cat <<EOF
  -b Build application.
  -v Verbose.
  -c Compress and make a tarball.
EOF
  exit 2
}

if [[ -z $1 ]]; then
  print_help_and_exit
fi

while getopts "bvc" optNames; do
case "$optNames" in
  b) build=1;;
  v) verbose=1;;
  c) compress=1;;
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
fi

if [ "$compress" ]; then
  # Make a tarball gatorshare.tar.gz with gatorshare as the root directory inside.
  tar -C $installer_dir -czvf "$installer_dir/../../gatorshare.tar.gz" . --exclude="*~"
fi