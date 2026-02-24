import os
def write(p,c):
    os.makedirs(os.path.dirname(p),exist_ok=True)
    open(p,chr(119)).write(c)
    print(chr(32)*2+chr(119)+chr(114)+chr(111)+chr(116)+chr(101)+chr(32)+p)
