#!/usr/bin/env python

import os
from os import path
from os.path import dirname, join
from subprocess import call
import setupconfig as config
import shutil

mkbundle_cmd_tmpl = "MONO_OPTIONS=--runtime=v2.0.50215 mkbundle2 -o %(output_bundle)s --deps --config-dir " + \
    "%(config_dir)s --machine-config %(machine_config)s --config %(config)s --static -z %(exe)s "

def makebundle():
  bundle_server()
  bundle_client()
  shutil.copy(config.xsp_exe, config.server_bin)
  
def _modify_env():
  environment = os.environ
  try:
    ld_library_path = environment["LD_LIBRARY_PATH"]
  except KeyError:
    ld_library_path = ""
  
  ld_library_path += config.ld_library_path
  environment["LD_LIBRARY_PATH"] = ld_library_path
  
  return environment
  
  
def bundle_server():
  """ Bundle GatorShare.Web """
  dlls = "System.Xml.Linq.dll "
  for i in os.walk(config.server_lib):
    files = i[2]
    for file in files:
        if file.endswith(".dll"):
          dlls += join(config.server_lib, file) + " "
  
  mkbundle_cmd = (mkbundle_cmd_tmpl + \
    "%(dlls)s") % { "output_bundle": config.server_bundle, 
                   "config_dir": config.installer_etc,
                   "machine_config": config.machine_config, 
                   "config": config.sys_config,
                   "exe": config.xsp_exe, "dlls": dlls}
    
  mkbundle_gmcs_cmd = mkbundle_cmd_tmpl % {
    "output_bundle": join(config.server_lib, "gmcs"), 
    "config_dir": config.installer_etc,
    "machine_config": config.machine_config, 
    "config": config.sys_config,
    "exe": "/usr/lib/mono/2.0/gmcs.exe" }
    
  print "Running command:", mkbundle_cmd
  environment = _modify_env()
  call(mkbundle_cmd, shell=True, env=environment)
  
  # No need to bundle gmcs for precompiled gsserver.
  #print "Running command:", mkbundle_gmcs_cmd
  #call(mkbundle_gmcs_cmd, shell=True)
  
def bundle_client():
  """ Bundle GSClient """
  dlls = ""
  for i in os.walk(config.client_bin):
    files = i[2]
    for file in files:
        if file.endswith(".dll"):
          dlls += join(config.client_bin, file) + " "
  
  mkbundle_cmd = (mkbundle_cmd_tmpl + "%(dlls)s") % { 
    "output_bundle": config.client_bundle, \
    "config_dir": config.installer_etc, 
    "machine_config": config.machine_config, 
    "config": config.sys_config,
    "exe": config.client_exe, "dlls": dlls}
    
  print "Running command:", mkbundle_cmd
  environment = _modify_env()
  call(mkbundle_cmd, shell=True, env=environment)
  
if __name__ == "__main__":
  makebundle()