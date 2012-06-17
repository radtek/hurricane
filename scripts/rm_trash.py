import os
import argparse
from os.path import join

trash_arr = ["obj", "bin"]

parser = argparse.ArgumentParser()
parser.add_argument('-d', '--dry_run', action='store_true')
parser.add_argument('top')
args = parser.parse_args()
top = args.top
dry_run = args.dry_run

for root, dirs, files in os.walk(top, topdown=False):
    for name in files:
        for n in trash_arr:
            if n in join(root, name):
                if dry_run:
                    print join(root, name)
                else:
                    os.remove(os.path.join(root, name))
    for name in dirs:
        for n in trash_arr:
            if n in join(root, name):
                if dry_run:
                    print join(root, name)
                else:
                    os.rmdir(os.path.join(root, name))