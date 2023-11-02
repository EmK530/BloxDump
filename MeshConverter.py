import json
from os import path,system

db = False

oldprint = print
def debug(tx):
    if db:
        oldprint('\x1b[6;30;44m' + 'DEBUG' + '\x1b[0m '+str(tx))
def print(tx):
    oldprint('\x1b[6;30;47m' + 'INFO' + '\x1b[0m '+str(tx))
def warn(tx):
    oldprint('\x1b[6;30;43m' + 'WARN' + '\x1b[0m '+str(tx))
def error(tx):
    oldprint('\x1b[6;30;41m' + 'ERROR' + '\x1b[0m '+str(tx))

supported_mesh_versions = [
    "version 1.00", "version 1.01"
]

class BloxMesh:
    def version1(data, folderName, outhash):
        split=data.decode('iso-8859-15').splitlines()
        version=split[0]
        num_faces=int(split[1])
        content=json.loads("["+split[2].replace("][","],[")+"]")
        true_faces=int(len(content)/3)
        debug("[BloxMesh_v1] Mesh is version {} and has {} faces.".format(version,num_faces))
        if not path.isdir("assets/"+folderName):
            system('mkdir "assets/{}" >nul 2>&1'.format(folderName))
        out=open("assets/{}/{}.obj".format(folderName,outhash),"w")
        out.write("# Converted from Roblox Mesh {} to obj by BloxDump".format(version))
        vertData=""
        texData=""
        normData=""
        faceData=""
        loops=0
        for i in range(true_faces):
            loops+=1
            vert=content[i*3]
            norm=content[i*3+1]
            uv=content[i*3+2]
            if version=="version 1.00":
                vertData+=("\nv {} {} {}".format(vert[0]/2,vert[1]/2,vert[2]/2))
            else:
                vertData+=("\nv {} {} {}".format(vert[0],vert[1],vert[2]))
            normData+=("\nvn {} {} {}".format(norm[0],norm[1],norm[2]))
            texData+=("\nvt {} {} {}".format(uv[0],1.0 - uv[1],uv[2]))
        for i in range(int((loops-1)/3)):
            pos=i*3+1
            faceData+=("\nf {}/{}/{} {}/{}/{} {}/{}/{}".format(pos,pos,pos,pos+1,pos+1,pos+1,pos+2,pos+2,pos+2))
        out.write(vertData)
        out.write(normData)
        out.write(texData)
        out.write(faceData)
        out.close()

    def Convert(v1, v2, v3):
        meshVersion=v1[0:12].decode('iso-8859-15')
        numOnlyVer=meshVersion[8:12]
        if not meshVersion in supported_mesh_versions:
            return error("Attempt to convert unsupported mesh.")
        if numOnlyVer=="1.00" or numOnlyVer=="1.01":
            BloxMesh.version1(v1, v2, v3)