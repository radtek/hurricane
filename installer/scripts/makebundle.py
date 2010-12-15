#!/usr/bin/env python

import os
from os import path
from os.path import dirname, join
from subprocess import call
import setupconfig as config

def makebundle():
  bundle_client()
  bundle_server()
  
def bundle_server():
  # Bundle GatorShare.Web
  dlls = ""
  for i in os.walk(config.server_lib):
    files = i[2]
    for file in files:
        if file.endswith(".dll"):
          dlls += join(config.server_lib, file) + " "
  
  mkbundle_cmd = "mkbundle2 -o " + \
    config.server_bundle + " --deps --config-dir " + config.installer_etc + \
    " --static -z " + config.xsp_exe + " " + dlls
  print "Running command", mkbundle_cmd
  call(mkbundle_cmd, shell=True)
  
def bundle_client():
  # Bundle GSClient
  dlls = ""
  for i in os.walk(config.client_bin):
    files = i[2]
    for file in files:
        if file.endswith(".dll"):
          dlls += join(config.client_bin, file) + " "
  
  mkbundle_cmd = ("mkbundle2 -o " + \
    "%(client_bundle)s --deps --config-dir %(config_dir)s --static -z %(exe)s " + \
    "%(dlls)s") % { "client_bundle": config.client_bundle, \
                  "config_dir": config.installer_etc, "exe": config.client_exe, \
                  "dlls": dlls}
  print "Running command", mkbundle_cmd
  call(mkbundle_cmd, shell=True)
  
if __name__ == "__main__":
  makebundle()