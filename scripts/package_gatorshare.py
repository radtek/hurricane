#!/usr/bin/env python

import sys, getopt, os, tarfile, logging
from os.path import join, dirname
from subprocess import call
import configuration as config

tar_output = join(config.sln_dir, "gshare.tar.gz")

usage = """ Packages the binaries and scripts in this project.
Usage: %s [-tbd]
-t  Include test scripts
-b  Include Brunet
-d  Exclude Bundles
-a  Include all
-v  Verbose
""" % sys.argv[0]


def package_gatorshare(include_tests=True, include_brunet=True, 
                       exclude_bundle=False, include_this=True):
  
  includes = "gatorshare/installer"
  if include_tests:
    includes += " gatorshare/tests/scripts"
  if include_brunet:
    includes += " gatorshare/misc/brunet"
  if include_this:
    includes += " gatorshare/scripts"
  excludes = "--exclude='*~' --exclude='*.pyc'"
  if exclude_bundle:
    excludes += "--exclude='*.bundle'"
    
  cmd = "tar -C %s -cz%sf %s %s %s" % \
        (dirname(config.sln_dir), 
         logging.getLogger().getEffectiveLevel() == logging.DEBUG and "v" or "", 
         tar_output, includes, excludes)
  logging.info(cmd)
  call(cmd, shell=True)

def main():
  """ Makes an archive from selected directories """
  try:
    optlist, args = getopt.getopt(sys.argv[1:], "tbdav", ["include-this"])
  except getopt.GetoptError, err:
    # print help information and exit:
    print str(err)
    print usage
    sys.exit(2)
  
  include_tests = include_brunet = exclude_bundle = False
  include_this = False
  verbose = False
  for k,v in optlist:
    if k == "-t":
      include_tests = True
    elif k == "-b":
      include_brunet = True
    elif k == "-d":
      exclude_bundle = True
    elif k == "--include-this":
      include_this = True
    elif k == "-a":
      include_this = include_brunet = include_tests = True
    elif k == "-v":
      verbose = True
    else:
      assert False, "unhandled option"
  
  logging.basicConfig(stream=sys.stdout, 
    level=(logging.DEBUG if verbose else logging.INFO), 
    format="%(asctime)s %(name)s %(levelname)s %(message)s")
      
  package_gatorshare(include_tests, include_brunet, exclude_bundle, include_this)
  
if __name__ == "__main__":
  main()