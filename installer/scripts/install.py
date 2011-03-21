#!/usr/bin/env python

import sys, getopt, os, tarfile
import setupconfig as config
from os import path
from os.path import join, dirname
from subprocess import call

usage = """Usage: %s -ruv iface
-r  Reinstall
-u  Uninstall
-v  Verbose
iface  Interface used with GatorShare
""" % sys.argv[0]

class GatorShareInstaller:
  
  def reinstall(self, iface):
    this_dir = dirname(path.abspath(__file__))
    
    install_sh = join(this_dir, "install.sh")
    call([install_sh, iface])
  
  def install(self, iface):
    if path.exists(config.install_dest_unix):
      print "GatorShare is already installed at %s" % config.install_dest_unix
      exit(1)
    else:
      self.reinstall(iface)
  
  def uninstall(self):
    this_dir = dirname(path.abspath(__file__))
    uninstall_sh = join(this_dir, "uninstall.sh")
    call([uninstall_sh])

def main():
  installer = GatorShareInstaller()
  try:
    optlist, args = getopt.getopt(sys.argv[1:], "ruvb")
    
    if len(args) == 0:
      assert False
      
    installer.verbose = False
    do_install_brunet = do_resintall = do_uninstall = False
    for k,v in optlist:
      if k == "-r":
        do_resintall = True
      elif k == "-u":
        do_uninstall = True
      elif k == "-v":
        installer.verbose = True
      elif k == "-b":
        do_install_brunet = True
      else:
        assert False, "unhandled option"
  except:
    print usage
    sys.exit(2)
  
  iface = args[0]
  
  if do_resintall:
    installer.reinstall(iface)
  elif do_uninstall:
    installer.uninstall()
  else:
    installer.install(iface)

if __name__ == "__main__":
  main()
  