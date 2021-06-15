#!/usr/bin/env python3
'''genonline.py - Python 3 version of AnHowe's script to package a cloud-init.txt script into an
   Azure Resource Manage template format. 
   goal: "commandToExecute": "[variables('jumpboxWindowsCustomScript')]
"'''
import os
import re
import sys


def convertToOneArmTemplateLine(file):
    with open(file) as f:
        content = f.read()

    # convert to one line
    content = content.replace("\\", "\\\\")
    content = content.replace("\r\n", "\\n")
    content = content.replace("\n", "\\n")
    content = content.replace('"', '\\"')
    content = content.replace('\'', '\'\'')

    # replace {{{ }}} with variable names
    return re.sub(r"{{{([^}]*)}}}", r"',variables('\1'),'", content)


def usage():
    print('    usage: ', os.path.basename(sys.argv[0]), 'file1')
    print('    builds a one line string to send to commandToExecute')


def main():
    if len(sys.argv) != 2:
        usage()
        sys.exit(1)

    file = sys.argv[1]
    if not os.path.exists(file):
        sys.exit('Error: file: ' + file + ' does not exist')

    # build the yml file for cluster
    oneline = convertToOneArmTemplateLine(file)

    print('"customData": "[base64(concat(\'' + oneline + '\'))]\",')

if __name__ == "__main__":
    main()
