#!/usr/bin/env python
# Configuration that python files in the same directory can import.

from os import path
from os.path import join, dirname


my_path = path.abspath(__file__)
# It can be the installer dir in the source code tree or the installation root.
installer_dir = dirname(dirname(my_path))

installer_etc = join(installer_dir, "etc")
client_bin = join(join(installer_dir, "client"), "bin")

# Server bin is the root of "binaries" in the server module which include ASP.NET
# Pages
server_bin = join(join(installer_dir, "server"), "bin")

# Server lib is the "bin" subfolder under the server_bin root where dlls reside. 
server_lib = join(server_bin, "bin")

# Location of bundles.
client_bundle = join(client_bin, "gsclient.bundle")
server_bundle = join(join(server_bin, "bin"), "gsserver.bundle")

client_exe = join(client_bin, "GSClientApp.exe")
#xsp_exe = "/usr/lib/mono/2.0/xsp2.exe"
xsp_exe = join(server_lib, "xsp2.exe")

shadow_dir = join(join(join(installer_dir, "client"), "var"), "shadow")
ld_library_path = "/lib:/usr/lib:/usr/local/lib" 