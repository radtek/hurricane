#!/usr/bin/env python

import os
from subprocess import Popen, PIPE
from os.path import join
import configuration as config

def _is_brunet_xmlrpc_running():
  """ Check the port """
  xmlrpc_port = 10000
  cmd = "netstat -nlt | grep :%s" % xmlrpc_port
  output = Popen(cmd, shell=True, stdout=PIPE).communicate()[0]
  if output is None or len(output) == 0:
    return False
  else:
    return True 
  
def run_brunet_bundle(count=1):
  if _is_brunet_xmlrpc_running():
    print "Brunet is already running."
    return
  
  p2pnode_dir = join(config.brunet_basedir, "node")
  p2pnode = join(p2pnode_dir, "p2pnode")
  
  lib_path = "/lib:/usr/lib:/usr/local/lib" + os.pathsep + p2pnode_dir
  
  environment = os.environ
  try:
    environment["LD_LIBRARY_PATH"] += lib_path
  except KeyError:
    environment["LD_LIBRARY_PATH"] = lib_path
  environment["MONO_NO_SMP"] = "1"
  
  cmd = \
    "nohup %(exe)s -n %(conf)s -c %(count)s 2>&1 | %(cronolog)s --period='1 day' %(log)s" % \
    { "exe": p2pnode, "conf": config.node_config_file, "count": count,
    "cronolog": join(p2pnode_dir, "cronolog"), 
    "log": join(p2pnode_dir, "node.log.%y%m%d.txt") }
    
  print cmd, environment
  
  Popen(cmd, shell=True, env=environment, cwd=p2pnode_dir)
  
def main():
  pass

if __name__ == "__main__":
  main()

