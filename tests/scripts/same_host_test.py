#!/usr/bin/env python
# This script interacts with running GatorShare file systems.

from __future__ import with_statement
import sys, os, shutil
from subprocess import Popen, PIPE
from os import path
from os.path import dirname, join
import random
import platform
import utils
import tempfile

my_path = path.abspath(__file__)
sln_dir = dirname(dirname(dirname(my_path)))
etc_dir = join(sln_dir, "etc")
test_data_in_temp = join(tempfile.gettempdir(), "gatorshare_test_data.dat")

def write_read_compare():
  """ Copies the data file to publisher's folder, and reads it in the downloader's folder. """
  
  test_data_path_config = join(etc_dir, "test_data_path.txt")
  if os.path.exists(test_data_path_config):
    with open(test_data_path_config) as f:
      test_data_path = f.readline().strip()
  else:
    test_data_path = test_data_in_temp
  
  if not os.path.exists(test_data_path):
    print "Test data does not exist, creating it..."
    utils.create_random_file(test_data_path, 100)
    
  hostname = platform.uname()[1]
  publish_dir = join("/mnt/gatorshare/bittorrent", hostname)
  read_dir = join("/mnt/gatorshare2/bittorrent", hostname)
  dest_file_name = str(random.randrange(100, 10000)) + ".dat"
  write_file_path = join(publish_dir, dest_file_name)
  print "Copying %s to %s" % (test_data_path, write_file_path)
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