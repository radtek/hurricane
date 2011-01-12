#!/usr/bin/env python

import getopt, sys, os, time, platform
from os import path
from os.path import dirname, join
from subprocess import Popen, PIPE
from subprocess import call
import setupconfig as config

usage = """Usage: %s [-scfv]
  -s  Run server.
  -c  Run client.
  -v  Verbose.
  -f  Run applications as foreground processes.
""" % sys.argv[0]

# Constants
my_path = path.abspath(__file__)
installer_dir = dirname(dirname(my_path))
sln_dir = dirname(installer_dir)
client_bin = join(join(installer_dir, "client"), "bin")
server_bin = join(join(installer_dir, "server"), "bin")
client_bundle = join(client_bin, "gsclient.bundle")
server_bundle = join(join(server_bin, "bin"), "gsserver.bundle")
client_exe = join(client_bin, "GSClientApp.exe")
shadow_dir = join(join(join(installer_dir, "client"), "var"), "shadow")

# Instance specific parameters
# The path will be customized by scripts.
mount_point = "/mnt/gatorshare"
gsserver_port = 8080

class GatorShareRunner:
  def run_gatorshare(self):
    try:
      optlist, args = getopt.getopt(sys.argv[1:], "scvb")
      self.verbose = False
      run_server = run_client = run_bundle = False
      if len(optlist) == 0:
        assert False
      for k,v in optlist:
        if k == "-s":
          run_server = True
        elif k == "-c":
          run_client = True
        elif k == "-v":
          self.verbose = True
        elif k == "-b":
          run_bundle = True
        else:
          assert False, "unhandled option"
    except:
      print usage
      sys.exit(2)
    
    if run_bundle:
      server_app = server_bundle
      client_app = client_bundle
    else:
      server_app = "xsp2"
      client_app = "mono " + client_exe
      
    if run_server:
      self.run_gsserver(server_app)
    
    if run_client:
      self.run_gsclient(client_app)
  
  def run_gsserver(self, server_app):
    """ Runs GSServer"""
    server_cmd = "%s --root %s --port %s --verbose --nonstop 2>&1 | tee %s" % (server_app, \
      server_bin, gsserver_port, \
      join(config.server_log, "output-server-$(date +%y%m%d%H%M%S).txt"))
    # Run as a background job
    lib_path = config.server_lib + os.pathsep + config.ld_library_path
    path_env = config.server_lib + os.pathsep + os.environ["PATH"]
    
    environment = os.environ
    environment["LD_LIBRARY_PATH"] = lib_path
    environment["MONO_CFG_DIR"] = config.installer_etc
    environment["PATH"] = path_env

    if self.verbose:
      environment["MONO_LOG_LEVEL"] = "debug"
    cwd = config.server_lib
    
    print "Going to run command %s in background. Env=%s, CWD=%s" % \
      (server_cmd, environment, cwd)
    Popen(server_cmd, shell=True, env=environment, cwd=cwd)
    
  def run_gsclient(self, client_app):
    """ Runs GSClient """
    mounts = Popen(["mount"], stdout=PIPE).communicate()[0]
    if mount_point in mounts.split():
      # unmount if already mounted.
      call(["umount", "-vl", mount_point])
      
    call(["mkdir", "-pv", shadow_dir])
    call(["mkdir", "-pv", mount_point])
    
    libdir = client_bin
    
    call("modprobe fuse".split())
    while not os.path.exists("/dev/fuse"):
      time.sleep(1)
    
    client_cmd = "%(client_app)s -o allow_other -m %(mount_point)s -S %(shadow)s 2>&1 | tee %(output)s" % \
      { "client_app": client_app, "mount_point": mount_point, \
        "shadow": shadow_dir, \
        "output": join(config.client_log, "output-client-$(date +%y%m%d%H%M%S).txt") }
    lib_path = config.client_bin + os.pathsep + config.ld_library_path
    # Run as a background job.
    
    environment = os.environ
    environment["LD_LIBRARY_PATH"] = lib_path
    environment["MONO_CFG_DIR"] = config.installer_etc

    if self.verbose:
       environment["MONO_LOG_LEVEL"] = "debug"
    cwd = config.client_bin
    print "Going to run command %s as a background job. Env=%s; CWD=%s" % \
      (client_cmd, environment, config.client_bin)
    
    Popen(client_cmd, shell=True, env=environment, cwd=cwd)
    
    while not mount_point in Popen(["mount"], stdout=PIPE).communicate()[0].split():
      print "FUSE is not up yet. Sleeping 1s..."
      time.sleep(1)
    
    namespace = platform.uname()[1]
    publish_dir = join(join(mount_point, "bittorrent"), namespace)
    
    if not os.path.exists(publish_dir):
      print "Creating %s ..." % publish_dir
      os.makedirs(publish_dir)

if __name__ == "__main__":
  GatorShareRunner().run_gatorshare()
  