# This file handles brunet related operations.
from __future__ import with_statement
from subprocess import Popen, call, PIPE
import tarfile, os, urllib2
from os import path
from os.path import dirname, join
from . import configuration as config
import shutil, sys, getopt, fileinput

usage =  """Usage: %s [-n <brunet_namespace>]
""" % sys.argv[0]

def install_brunet_bundle(tar_decompressed_dir):
  """ Takes the descompressed directory of the bundle files and installs it. 
  """
    
  cmd =  "cp -R %s %s" % (tar_decompressed_dir, config.brunet_basedir)
  call(cmd, shell=True)
  
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
