# TIM2 the Tool Manager by Nekonosuke  

This tool (when finished) is meant to be a multi-tool for modding PAK archives in games  
developed by Level 5 (primarily DQ8 but partially DC 1 & 2, possibly RG)  
  
There are many extraction tools but few that repack files. Currently, it takes roughly 7 programs  
to make a single texture mod. In addition, most of the tools do not support batch processing.  
  
Throughout this tool's development I aim to decrease the complexity of the process and reduce the steps and tools required.  
  
As described [on the dq8p-modding channel](https://discord.gg/wxSfGqKmCJ "Yggdrasil Dragon Quest Modding Discord") in [this post](https://discord.com/channels/499582383067234305/1013548275447627836/1159629717763276900), this project will have several  
stages of progress based on amount of steps it reduces in producing a texture mod:  
  
**Stage 0:** *(right now)* Extract and rebuild PAK archives  
  
**Stage 1:** Extract TM2 from IM3 (.img) or PAK for processing then re-insert to PAK or IM3  
  
**Stage 2:** Provide command line interface with other necessary GUI programs to facilitate bulk editing  
  
**Stage 3:** Extract and replace any texture from PAK/IM3 as editable PNG (replacing all GUI programs)  
  
**Useage / Switch Order:**  
Add the executable to your Path or move game file to same directory as tool  
In the examples below, [these] are optional arguments and <these> are required.  
  
Unpack PAK archive (.chr, .pac, .pak, etc):  
    ./TM2toolmanager [-d || --debug] <MyFile.PAK>  
  
Rebuild PAK archive (.json):  
    ./TM2toolmanager [-d || --debug] <MyFile.json>  
  
**Options:**  
    -d, --debug       Enable debug messages  
    -h, --help        Display this help message  
  
**Progress:**  
This tool is currently under construction.  
Here are its current functionalities:  
    -Unpack PAK archive (.chr, .pac, .pak, etc)  
    -Rebuild PAK archive (.json)  
