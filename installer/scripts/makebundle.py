#!/usr/bin/env python

import os, logging, sys, getopt
from os import path
from os.path import dirname, join
from subprocess import call, PIPE
import setupconfig as config
import shutil, logging.config

mkbundle_cmd_tmpl = "MONO_OPTIONS=--runtime=v2.0.50215 mkbundle -o %(output_bundle)s --deps --config-dir " + \
    "%(config_dir)s --machine-config %(machine_config)s --config %(config)s --static -z %(exe)s "

def makebundle():
  bundle_server()
  logging.info("Finished bundling gsserver.")
  bundle_client()
  logging.info("Finished bundling gsclient.")
  # For some reason, the xsp executable has to be in the server root
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
    
  
  mkbundle_gmcs_cmd = mkbundle_cmd_tmpl % { #@UnusedVariable
    "output_bundle": join(config.server_lib, "gmcs"), 
    "config_dir": config.installer_etc,
    "machine_config": config.machine_config, 
    "config": config.sys_config,
    "exe": "/usr/lib/mono/2.0/gmcs.exe" }
    
  logging.debug("Running command: " + mkbundle_cmd)
  environment = _modify_env()
  stdout = \
    None if logging.getLogger().getEffectiveLevel() == logging.DEBUG else PIPE
  call(mkbundle_cmd, shell=True, env=environment, stdout=stdout)
  
  
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
    
  logging.debug("Running command: " + mkbundle_cmd)
  environment = _modify_env()
  stdout = \
    None if logging.getLogger().getEffectiveLevel() == logging.DEBUG else PIPE
  call(mkbundle_cmd, shell=True, env=environment, stdout=stdout)

def main():
  try:
    optlist, args = getopt.getopt(sys.argv[1:], "v")
  except getopt.GetoptError, err:
    # print help information and exit:
    print >> sys.stderr, str(err)
    sys.exit(2)
    
  logging.config.fileConfig(join(config.installer_etc, "logging.conf"))
  for k, v in optlist:
    if k == "-v":
      logging.getLogger().setLevel(logging.DEBUG)
    else:
      assert False, "unhandled option"
        
  makebundle()
  
if __name__ == "__main__":
  main()