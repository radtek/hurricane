#!/usr/bin/env python

import sys, os, shutil
from subprocess import Popen, PIPE
from os import path
from os.path import dirname, join
import random
import platform

my_path = path.abspath(__file__)
sln_dir = dirname(dirname(dirname(my_path)))
etc_dir = join(sln_dir, "etc")

def write_read_compare():
  """ Copies the data file to publisher's folder, and reads it in the downloader's folder. """
  with open(join(etc_dir, "test_data_path.txt")) as f:
    test_data_path = f.readline().strip()
  hostname = platform.uname()[1]
  publish_dir = join("/mnt/gatorshare/bittorrent", hostname)
  read_dir = join("/mnt/gatorshare2/bittorrent", hostname)
  dest_file_name = str(random.randrange(100, 10000)) + ".dat"
  write_file_path = join(publish_dir, dest_file_name)
  print "Copying data to", write_file_path
  if not path.exists(publish_dir):
    os.makedirs(publish_dir)
  shutil.copy(test_data_path, write_file_path)
  raw_input("Press any key to continue.")
  read_file_path = join(read_dir, dest_file_name)
  if not path.exists(read_dir):
    os.makedirs(read_dir)
  print "Reading data from", read_file_path
  md5sum_transferred = Popen(["md5sum", read_file_path], stdout=PIPE).communicate()[0].split()[0]
  md5sum_original = Popen(["md5sum", write_file_path], stdout=PIPE).communicate()[0].split()[0]
  print "md5sum (expect, actual)", md5sum_original, md5sum_transferred
  assert md5sum_original == md5sum_transferred

if __name__ == "__main__":
  write_read_compare()