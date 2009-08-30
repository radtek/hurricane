#!/usr/bin/python
import xmlrpclib, getopt, sys, hashlib, base64

def readfile(path, offset, bytesToRead):
  with open(path) as f:
    f.seek(int(offset))
    ret = f.read(int(bytesToRead))
    print f.tell()
    return ret

def checksum(arg):
  m = hashlib.md5()
  m.update(arg)
  return m.hexdigest()

optlist, args = getopt.getopt(sys.argv[1:], "e:a:o:b:v")
optdict = {}
for k,v in optlist:
  optdict[k] = v

actual = readfile(optdict["-a"], optdict["-o"], optdict["-b"])
expected = readfile(optdict["-e"], optdict["-o"], optdict["-b"])

actual_sum = checksum(actual)
expected_sum = checksum(expected)
if "-v" in optdict:
  print "md5 hexdigest (actual):", actual_sum
  #print "base64 actual:", base64.b64encode(str(actual))
  print "md5 hexdigest (expected):", expected_sum
  #print "base64 expected:", base64.b64encode(str(expected))
assert actual_sum == checksum(expected)