#!/usr/bin/env python

import testconfig as config
from os import path
from os.path import dirname, join
from subprocess import call
import getopt, sys

installer_make = join(config.installer_scripts, "make.sh")

def make_test_archive(bundle):
  """ Creates an archive file for with test scripts included. """
  if bundle:
    call([installer_make, "-da"])
  else:
    call([installer_make, "-a"])
  cmd = "tar -C %(sln_dir)s -rvf %(installer_archive)s tests/scripts/ --exclude=*~ " % {
    "sln_dir": config.sln_dir, "installer_archive": config.installer_archive }
  call(cmd, shell=True)
  call(["gzip", config.installer_archive, "-f"])
  

def main():
  try:
    optlist, args = getopt.getopt(sys.argv[1:], "b")
    bundle = False
    for k,v in optlist:
      if k == "-b":
        bundle = True
      else:
        assert False, "unhandled option"
  except:
    print "Usage: %s [-b] \n -b: Bundle binaries." % __file__
    sys.exit(2)
  
  make_test_archive(bundle)
  
if __name__ == "__main__":
  main()