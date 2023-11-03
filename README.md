## TIM2 the Tool Manager by Nekonosuke  

This is a multi-tool for modding PAK archives in games developed by Level 5  
(primarily DQ8 but partially Dark Cloud 1 & 2, possibly Rogue Galaxy)  
  
As of now this tool can extract and rebuild both PAK and IMG archives. Most importantly,  
you do not need to keep to the same size files as the original! However, changing sizes has not  
been tested in-game yet.  
  
Some examples of supported file types:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;.pac  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;.pak  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;.chr  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;.img  
  
Throughout this tool's development I aim to decrease the complexity of the process and reduce  
the steps and tools required to make a texture mod. So far I have reduced the number of tools  
needed by two and elimated hex editing from the process.  
  
As described [on the dq8p-modding channel](https://discord.gg/wxSfGqKmCJ "Yggdrasil Dragon Quest Modding Discord") in [this post](https://discord.com/channels/499582383067234305/1013548275447627836/1159629717763276900), this project will have several  
stages of progress based on amount of steps it reduces in producing a texture mod:  
  
**Stage 1:** *(right now)*  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Extract TM2 from IM3 (.img) or PAK for processing then re-insert to PAK or IM3  
  
**Stage 2:** Provide command line interface with other necessary GUI programs to facilitate bulk editing  
  
**Stage 3:** Extract and replace any texture from PAK/IM3 as editable PNG (replacing all GUI programs)  
  
**Useage / Switch Order:**  
With CLI it's best to use full paths or cd to the file  
Drag 'n' Drop also supported  
In the examples below, [these] are optional arguments and <these> are required  
  
Unpack or repack PAK or IMG archive (.chr, .pac, .pak, .img, .json etc):  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;./TM2toolmanager [-d || --debug] <MyFile.PAK> [another0.IMG another1.json etc]  
  
Using batch mode (all valid files in directory):  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;./TM2toolmanager <-b || --batch> <--repack and/or --extract> [-d || --debug]  
  
**Options:**  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-d, --debug&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Enable debug messages  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-h, --help&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Display this help message  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-b, --batch&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Use batch processing  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-e, --extract&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Used with batch. Extract all files in directory  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-p, --repack&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Used with batch. Repacks all extracted files in directory  

**Planned** ~~bugs~~ **features and known bugs:**  
- Needs to auto-extract/repack PAKs contained inside of other PAKs  
- Needs support for DQ8M (mobile) PAKs  
- Needs parser for .clo, .lst, .str, and .mes text files  
- Multiple dummy headers at end of PAK causes error  
- Allow repacking of empty files  
- Auto unswizzle TM2 (Possibly not until v2.0)  
