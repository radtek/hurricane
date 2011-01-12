#!/usr/bin/env python

import os
from os.path import dirname, join

my_path = os.path.abspath(__file__)
brunet_basedir = dirname(dirname(dirname(my_path))) 
brunet_etc = join(brunet_basedir, "etc")
remote_tas_file = join(brunet_etc, "RemoteTAs.xml")
brunet_ns_file = join(brunet_etc, "BrunetNamespace.txt")
node_config_file = join(brunet_etc, "node.config")