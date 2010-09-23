#!/bin/bash
# This script builds the fushare project and copies binaries to make a package.

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
proj_dir="$scripts_dir/.."
fushare_proj="$proj_dir/../fushare"
fushare_scripts="$fushare_proj/scripts"
fushare_src="$fushare_proj/src"

if [ "$build" ]; then
  echo "Building Fushare..."
  "$fushare_scripts/make_fushare.sh" -b
  if [ $? -ne 0 ]; then
    echo "Build failed. Exiting..."
    exit 1
  fi

  client_proj_name="FushareApp"
  web_proj_name="Fushare.Web"
  client_dir="$fushare_src/$client_proj_name/bin/l4n"
  web_dir="$fushare_src/$web_proj_name"

  rsync -avLm --delete --include-from="$scripts_dir/rsync.rules" --exclude-from="$scripts_dir/rsync.rules" "$client_dir/" "$proj_dir/client/bin/"
  rsync -avLm --delete --include-from="$scripts_dir/rsync.rules" --exclude-from="$scripts_dir/rsync.rules" "$web_dir/" "$proj_dir/server/bin/"
fi

if [ "$compress" ]; then
  # Make a tarball gatorrent.tar.gz with gatorrent as the root directory inside.
  tar -C $proj_dir/.. -czvf "$proj_dir/../gatorrent.tar.gz" gatorrent --exclude=".git*" --exclude="*~"
fi

