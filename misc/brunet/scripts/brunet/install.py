#!/usr/bin/env python

# This file handles brunet related operations.
from __future__ import with_statement
from subprocess import Popen, call, PIPE
import tarfile, os, urllib2
from os import path
from os.path import dirname, join
import configuration as config
import shutil, sys, getopt, fileinput

usage =  """Usage: %s [-n <brunet_namespace>]
""" % sys.argv[0]

def install_brunet_bundle(bundle_download_url):
  """ Downloads the bundle from GA.org and decompresses it. """
  install_tgz = "install.tgz"
  download_to = join(config.brunet_basedir, install_tgz)
  
  # If it's already downloaded, abort the installation process.
  if path.exists(download_to):
    return False
  
#  download_cmd = "curl -o %s %s" % (download_to, config.brunet_bundle_url)
#  call(download_cmd, shell=True)
  
  bundle = urllib2.urlopen(bundle_download_url)
  with open(download_to,'wb') as f:
    f.write(bundle.read())
  
  tar = tarfile.TarFile.open(join(config.brunet_basedir, install_tgz))
  tar.extractall(config.brunet_basedir)
  
  cronolog_path = join(config.brunet_basedir, "node", "cronolog")
  cmd = "chmod +x %s" % cronolog_path
  call(cmd, shell=True)
  
  return True
    
def configure_brunet(brunet_ns, remote_tas_file, edge_listener_port=0):
  """ Choosing edge_listener_port=0 picks a random available port. """
  with open(remote_tas_file, 'r') as f:
    remote_tas = f.read()
  
  for line in fileinput.input(config.node_config_file, inplace=1):
    print line.replace('{BrunetNamespace}', brunet_ns) \
      .replace('{RemoteTAs}', remote_tas) \
      .replace('{EdgeListenerPort}', edge_listener_port),

def main():
  pass

if __name__ == "__main__":
  main()
