#!/usr/bin/env python

from os import path
from os.path import join, dirname

my_path = path.abspath(__file__)
sln_dir = dirname(dirname(dirname(my_path)))
installer_dir = join(sln_dir, "installer")
installer_scripts = join(installer_dir, "scripts")
installer_archive = join(sln_dir, "gshare.tar")
 
