#!/usr/bin/env python
# Configuration that python files in the same directory can import.

import os
from os import path
from os.path import join, dirname, split
from subprocess import Popen, PIPE


my_path = path.abspath(__file__)
# It can be the installer dir in the source code tree or the installation root.
installer_dir = dirname(dirname(my_path))

installer_etc = join(installer_dir, "etc")
machine_config = join(join(join(installer_etc, "mono"), "2.0"), "machine.config")

sys_config = join(join(installer_etc, "mono"), "config")

client_base = join(installer_dir, "client")
client_bin = join(client_base, "bin")
client_log = join(join(client_base, "var"), "log")

# Server bin is the root of "binaries" in the server module which include ASP.NET
# Pages
server_base = join(installer_dir, "server")
server_bin = join(server_base, "bin")

# Server lib is the "bin" subfolder under the server_bin root where dlls reside. 
server_lib = join(server_bin, "bin")
server_log = join(join(server_base, "var"), "log")

# Location of bundles.
client_bundle = join(client_bin, "gsclient.bundle")
server_bundle = join(join(server_bin, "bin"), "gsserver.bundle")

client_exe = join(client_bin, "GSClientApp.exe")

mono_prefix = split(split(Popen("which mono", shell=True, stdout=PIPE).communicate()[0])[0])[0]

xsp_exe = join(mono_prefix, "lib", "mono", "2.0", "xsp2.exe")

shadow_dir = join(join(join(installer_dir, "client"), "var"), "shadow")

ld_library_path = "/lib:/usr/lib:/usr/local/lib" 

install_dest_unix = "/opt/gatorshare"