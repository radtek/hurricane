#!/usr/bin/env python

from subprocess import call

def create_random_file(path, size_MB):
  cmd = "dd if=/dev/urandom of=%(file_path)s bs=%(bs)s count=%(size)s" % \
    { "file_path": path, "bs": 1024 * 1024, "size": size_MB }
  call(cmd, shell=True)
  