#!/usr/bin/env python
import sys, os, shutil
from os import path
from os.path import dirname, join

def run_second_instance(iface):
  """ Copies and modifies installer files to run a second instance of GatorShare on the same machine. """
  my_path = path.abspath(__file__)
  sln_dir = dirname(dirname(dirname(my_path)))
  installer_scripts_dir = join(join(sln_dir, "installer"), "scripts")

  install2_path = join(installer_scripts_dir, "install2.sh")
  run2_path = join(installer_scripts_dir, "run2.py")
  uninstall2_path = join(installer_scripts_dir, "uninstall2.sh")
  stop2_path = join(installer_scripts_dir, "stop2.sh")

  shutil.copy(join(installer_scripts_dir, "install.sh"), install2_path)
  shutil.copy(join(installer_scripts_dir, "run.py"), run2_path)
  shutil.copy(join(installer_scripts_dir, "uninstall.sh"), uninstall2_path)
  shutil.copy(join(installer_scripts_dir, "stop.sh"), stop2_path)

  #os.system("sed \"s/\/opt\/gatorshare/\/opt\/gatorshare2/\" -i " + join(installer_scripts_dir, "install2.sh"))

  with open(install2_path) as f:
    content = f.read()
    content = content.replace('/opt/gatorshare', '/opt/gatorshare2')
    content = content.replace('\/opt\/gatorshare', '\/opt\/gatorshare2')
    content = content.replace('8080', '8081')
    content = content.replace('23113', '23115')
    content = content.replace('23114', '23116')
  with open(install2_path, 'w') as f:
    f.write(content)
    
  with open(run2_path) as f:
    content = f.read()
    content = content.replace('/mnt/gatorshare', '/mnt/gatorshare2')
    content = content.replace('8080', '8081')
  with open(run2_path, 'w') as f:
    f.write(content)
    
  with open(uninstall2_path) as f:
    content = f.read()
    content = content.replace('/mnt/gatorshare', '/mnt/gatorshare2')
    content = content.replace('/opt/gatorshare', '/opt/gatorshare2')
  with open(uninstall2_path, 'w') as f:
    f.write(content)
    
  with open(stop2_path) as f:
    content = f.read()
    content = content.replace('gatorshare', 'gatorshare2')
  with open(stop2_path, 'w') as f:
    f.write(content)

  os.system("sudo " + uninstall2_path)
  os.system("sudo " + install2_path + " " + iface)
  os.system("sudo /opt/gatorshare2/scripts/run2.py -scb")

def main():
  if len(sys.argv) == 1:
    print "Usage: " + path.basename(__file__) + " iface"
    exit(2)
  
  iface = sys.argv[1]
  run_second_instance(iface)

if __name__ == "__main__":
  main()