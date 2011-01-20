#!/usr/bin/env python

import sys, getopt, os, tarfile
from os.path import join, dirname
from subprocess import call
import configuration as config

tar_output = join(config.sln_dir, "gshare.tar.gz")

usage = """Usage: %s [-tbd]
-t  Include test scripts
-b  Include Brunet
-d  Exclude Bundles
""" % sys.argv[0]

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
      
  includes = "installer"
  if include_tests:
    includes += " tests/scripts"
  if include_brunet:
    includes += " misc/brunet"
  if include_this:
    includes += " scripts"
    
  excludes = "--exclude='*~' --exclude='*.pyc'"
  if exclude_bundle:
    excludes += "--exclude='*.bundle'"
    
  cmd = "tar -C %s -cz%sf %s %s %s" % (config.sln_dir, verbose and "v" or "", 
    tar_output, includes, excludes)
  print cmd
  call(cmd, shell=True)
  
if __name__ == "__main__":
  main()