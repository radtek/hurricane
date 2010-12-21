#!/usr/bin/env python

import sys
from subprocess import call
import testconfig as config
import os
from os.path import dirname, join
from run_second_instance import run_second_instance

def run_two_instances(iface):
  call("sudo " + join(config.installer_scripts, "stop.sh"), shell=True)
  call("sudo " + join(config.installer_scripts, "install.sh") + " " + iface, shell=True)
  call("sudo /opt/gatorshare/scripts/run.py -scb", shell=True)
  
  run_second_instance(iface)

def main():
  if len(sys.argv) < 2:
    print "Usage: " + os.path.basename(__file__) + " iface"
    exit(2)
  
  iface = sys.argv[1]
  run_two_instances(iface)
  
if __name__ == "__main__":
  main()