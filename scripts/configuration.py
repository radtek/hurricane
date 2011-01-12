#!/usr/bin/env python

import os
from os.path import dirname, join

my_path = os.path.abspath(__file__)
this_dir = dirname(my_path)
sln_dir = dirname(this_dir)
sln_etc = join(sln_dir, "etc") 