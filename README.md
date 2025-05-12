# HerikaCommProxy
***Why send 1 request when you can send 10 and overload the server???***

# Setup Instructions

## Windows
 1. Download the win64 release.
 2. Extract anywhere.
 3. Run HerikaCommProxy.exe
 4. Edit `AIAgent.ini`

## Linux / DwemerDistro
 1. Download the linux64 release using wget or however you want.
 2. `tar zxvf filename_linux64.tar.gz`
 3. `cd HerikaCommProxy`
 4. `chmod +x HerikaCommProxy`
 5. `./HerikaCommProxy`
 6. Edit `AIAgent.ini`

## AIAgent.ini
Use this for your `AIAgent.ini` file.
```
SERVER=127.0.0.1
PORT=5154
PATH=HerikaServer/comm.php
POLINT=1
```

## Linux Start Script
If you want to run this in the background in DwemerDistro, you can use a script containing this to run it, then just use `tail -f log.txt` to view the output.
```
#!/bin/bash

cd /home/dwemer/HerikaCommProxy
./HerikaCommProxy &>log.txt&
echo Running...
```
