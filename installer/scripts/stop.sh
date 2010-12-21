#!/bin/bash

# Trailing / prevents pkill from killing processes with gatorshare prefix.
sudo pkill -KILL -f gatorshare/
sudo umount /mnt/gatorshare