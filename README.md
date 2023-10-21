## TIM2 the Tool Manager by Nekonosuke  

This tool (when finished) is meant to be a multi-tool for modding PAK archives in games  
developed by Level 5 (primarily DQ8 but partially DC 1 & 2, possibly RG)  
  
There are many extraction tools but few that repack files. Currently, it takes roughly 7 programs  
to make a single texture mod. In addition, most of the tools do not support batch processing.  
  
Throughout this tool's development I aim to decrease the complexity of the process and reduce the steps and tools required.  
  
As described [on the dq8p-modding channel](https://discord.gg/wxSfGqKmCJ "Yggdrasil Dragon Quest Modding Discord") in [this post](https://discord.com/channels/499582383067234305/1013548275447627836/1159629717763276900), this project will have several  
stages of progress based on amount of steps it reduces in producing a texture mod:  
  
**Stage 0:** *(right now)*  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Extract and rebuild PAK archives. Text files are converted  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-UTF-8 automatically and re-converted to SJIS upon import.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-IM2 and IM3 (*.img) files can now be extracted.  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Batch modes: specify multiple files or all in current directory  
  
**Stage 1:** Extract TM2 from IM3 (.img) or PAK for processing then re-insert to PAK or IM3  
  
**Stage 2:** Provide command line interface with other necessary GUI programs to facilitate bulk editing  
  
**Stage 3:** Extract and replace any texture from PAK/IM3 as editable PNG (replacing all GUI programs)  
  
**Useage / Switch Order:**  
Add the executable to your Path or move game file to same directory as tool  
In the examples below, [these] are optional arguments and <these> are required.  
  
Unpack PAK archive, rebuild PAK archive, unpack .img archive (.chr, .pac, .pak, .json etc):  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;./TM2toolmanager [-d || --debug] <MyFile.PAK> [another0.IMG another1.json etc]  
  
Using batch mode (all valid files in directory):  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;./TM2toolmanager <-b || --batch> <--repack and/or --extract> [-d || --debug]  
  
**Options:**  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-d, --debug&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Enable debug messages  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-h, --help&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Display this help message  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-b, --batch&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Use batch processing  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-e, --extract&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Used with batch. Extract all files in directory  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-p, --repack&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;Used with batch. Repacks all extracted files in directory  
  
**Progress:**  
This tool is currently under construction.  
Here are its current functionalities:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Unpack PAK archive (.chr, .pac, .pak, etc)  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Rebuild PAK archive (.json)  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;- *.cfg files are converted to UTF-8")  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Unpack .img archive (IM2 or IM3)  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;-Batch processing  

