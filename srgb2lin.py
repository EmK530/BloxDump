# Made because PVRTexToolCLI sucks
# EmK530 2023-07-04

import math
from PIL import Image

def convert(path):
    def srgb2lin(s):
        if s <= 0.0404482362771082:
            lin = s / 12.92
        else:
            lin = pow(((s + 0.055) / 1.055), 2.4)
        return lin
    im=Image.open(path)
    new=[]
    for i in im.getdata():
        if len(i) == 3:
            new.append((
                math.floor(srgb2lin(i[0])/2058.61501702),
                math.floor(srgb2lin(i[1])/2058.61501702),
                math.floor(srgb2lin(i[2])/2058.61501702),
            ))
        else:
            new.append((
                math.floor(srgb2lin(i[0])/2058.61501702),
                math.floor(srgb2lin(i[1])/2058.61501702),
                math.floor(srgb2lin(i[2])/2058.61501702),
                i[3]
            ))
    im.close()
    newim=Image.new(im.mode,im.size)
    newim.putdata(new)
    newim.save(path)
