#!/usr/bin/env python

import os, shutil, getopt
from os.path import join, dirname
from subprocess import call
import sys
import configuration as config

brunet_install_dest = "/opt"
brunet_basedir = join(config.sln_dir, "misc", "brunet")

def main():
  
  try:
    optlist, args = getopt.getopt(sys.argv[1:], "vn:t:l:")
  except getopt.GetoptError, err:
    # print help information and exit:
    print str(err)
    print usage
    sys.exit(2)
    
  verbose = False
  brunet_ns = remote_tas_file = download_link = None
  
  for k,v in optlist:
    if k == "-v":
      verbose = True
    elif k == "-n":
      brunet_ns = v
    elif k == "-t":
      remote_tas_file = v
    elif k == "-l":
      download_link = v
    else:
      assert False, "unhandled option"
  
  if brunet_ns is None or remote_tas_file is None:
    print "Warning: Brunet namespace or RemoteTAs is not specified."
    
  if download_link is None:
    sys.path.append(config.sln_etc)
    import localconfig
    try:
      download_link = localconfig.brunet_bundle_url
    except:
      print "No brunet bundle url defined. Exiting..."
      exit()
    
  install_brunet_basedir()
  sys.path.append(join(brunet_install_dest, "scripts"))
  import brunet.install
  brunet.install.install_brunet_bundle()
  
  # If not specified here, the user needs to configure brunet manually.
  if brunet_ns is not None and remote_tas_file is not None:
    brunet.install.configure_brunet(brunet_ns, remote_tas_file)
  
def install_brunet_basedir():
#  if os.path.exists(brunet_install_dest):
#    return
#  else:
#    shutil.copytree(brunet_basedir, brunet_install_dest)
  cmd = "cp -r %s %s" % (brunet_basedir, brunet_install_dest)
  call(cmd, shell=True)
    
if __name__ == "__main__":
  main()