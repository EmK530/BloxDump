import ctypes,tempfile,time,requests,json
from threading import Thread
from multiprocessing import Process,cpu_count
from os import path,system,listdir
from srgb2lin import convert

threads = 0
db = False

thrs = []
alive_threads = 0
known = []
knownlinks = []
tempPath = tempfile.gettempdir().replace("\\","/")+"/Roblox/http/"

bans=[
    "noFilter",
    "Png",
    "isCircular"
]

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

def make_thread(func,args):
    global alive_threads,threads
    while alive_threads >= threads:
        for i in thrs:
            if not i.is_alive():
                thrs.remove(i)
                alive_threads-=1
        if alive_threads >= threads:
            time.sleep(0.1)
    alive_threads+=1
    p = Thread(target=func, args=args)
    p.start()
    thrs.append(p)

def main(name):
    file=open(tempPath+name,"rb")
    databytes=file.read()
    data=databytes.decode('iso-8859-15')
    file.close()
    if data[0:4] != "RBXH":
        return debug("Ignoring non-RBXH file.")
    link=data[data.find("https://"):len(data)].split("\x00")[0]
    if link in knownlinks:
        return debug("Ignoring duplicate cdn link.")
    knownlinks.append(link)
    s=link.split("/")
    outhash=s[len(s)-1]
    if outhash in bans:
        return debug("Ignoring blocked hash.")
    #debug("Ripping cached file: "+i)
    dl=requests.get(link)
    while True:
        if not dl.status_code == 200:
            warn("Download failed, retrying...")
        else:
            break
    cont=dl.content
    begin=cont[0:32]
    output=None
    folder=None
    if begin.find(b"<roblox!")!=-1:
        return debug("Ignoring unsupported XML file.")
    elif begin.find(b"<roblox xml")!=-1:
        return debug("Ignoring unsupported XML file.")
    elif begin.find(b"version")!=-1:
        return debug("Ignoring unsupported mesh file.")
    elif begin.find(b'{"locale":"')!=-1:
        print("Data identified as JSON translation")
        output="translation"
        folder="Translations"
        #return debug("Ignoring unsupported translation file.")
    elif begin.find(b"PNG\r\n")!=-1:
        print("Data identified as PNG")
        output="png"
        folder="Textures"
    elif begin.find(b"OggS")!=-1:
        print("Data identified as OGG")
        output="ogg"
        folder="Sounds"
    elif begin.find(b"KTX ")!=-1:
        print("Data identified as Khronos Texture")
        output="ktx"
        folder="KTX Textures"
    elif begin.find(b'"name": "')!=-1:
        print("Data identified as JSON font list")
        output="ttf"
        folder="Fonts"
    elif begin.find(b'{"')!=-1:
        return debug("Ignoring general JSON file.")
    else:
        return warn("File unrecognized: "+begin.decode('iso-8859-15'))
    if output=="ktx":
        if not path.isdir("temp"):
            system('mkdir temp >nul 2>&1')
        if not path.isdir("assets/"+folder):
            system('mkdir "assets/'+folder+'" >nul 2>&1')
        out=open("temp/"+outhash+".ktx","wb")
        out.write(cont)
        out.close()
        stopped=False
        system('pvrtextoolcli -i temp/'+outhash+'.ktx -noout -shh -d "assets/'+folder+'/'+outhash+'.png"')
        p = Process(target=convert,args=('assets/'+folder+'/'+outhash+'.png',))
        p.start()
        p.join()
        system('del temp\\'+outhash+'.ktx')
    elif output=="ttf":
        js=json.loads(cont)
        outname=js["name"]
        if not path.isdir("assets/"+folder):
            system('mkdir "assets/'+folder+'" >nul 2>&1')
        out=open("assets/"+folder+"/"+outname+".json","wb")
        out.write(cont)
        out.close()
        print("Found {} fonts".format(len(js["faces"])))
        for i in js["faces"]:
            print("Downloading {}-{}.ttf...".format(outname,i["name"]))
            assetid=i["assetId"].split("rbxassetid://")[1]
            dl2=requests.get("https://assetdelivery.roblox.com/v1/asset?id="+assetid)
            if not dl2.status_code == 200:
                return warn("Download failed.")
            out=open("assets/"+folder+"/"+outname+"-"+i["name"]+".ttf","wb")
            out.write(dl2.content)
            out.close()
    elif output=="translation":
        js=json.loads(cont)
        locale=js["locale"]
        if not path.isdir("assets/"+folder):
            system('mkdir "assets/'+folder+'" >nul 2>&1')
        out=open("assets/"+folder+"/locale-"+locale+".json","wb")
        out.write(cont)
        out.close()
    elif output!=None:
        if not path.isdir("assets/"+folder):
            system('mkdir "assets/'+folder+'" >nul 2>&1')
        out=open("assets/"+folder+"/"+outhash+"."+output,"wb")
        out.write(cont)
        out.close()

if __name__ == '__main__':
    threads = cpu_count()
    system("cls")
    ctypes.windll.kernel32.SetConsoleTitleW('BloxDump | Prompt')
    print("Thread limit: {} threads.".format(threads))
    print("This thread limit was automatically picked to utilize 100% CPU for KTX processing.")
    oldprint()
    oldprint("Do you want to clear Roblox's cache?")
    oldprint("Clearing cache will prevent ripping of anything from previous game sessions.")
    oldprint("Do this if you want to let BloxRip work in real-time while you're playing.")
    ans=input("Type Y to clear or anything else to proceed: ")
    if ans.lower()=="y":
        print("Deleting Roblox cache...")
        system('del '+tempPath.replace("/","\\")+"* /q")
    system("cls")
    ctypes.windll.kernel32.SetConsoleTitleW('BloxDump | Idle')
    print("BloxDump started.")
    while True:
        counts = 0
        files = listdir(tempPath)
        total = len(files)
        for i in files:
            counts+=1
            ctypes.windll.kernel32.SetConsoleTitleW('BloxDump | Processing file {}/{} ({})'.format(counts,total,i))
            if not i in known:
                known.append(i)
                make_thread(main,(i,))
        ctypes.windll.kernel32.SetConsoleTitleW('BloxDump | Idle')
        print("Ripping loop completed.")
        time.sleep(10)
