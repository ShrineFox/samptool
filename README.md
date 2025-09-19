# samptool
A tool for repacking TTYD (GC)'s MusyX ".samp" sound archive format.  
Originally created by [tge-was-taken](https://github.com/tge-was-taken) in 2016.

# Usage
This is a commandline program, so you'd use the command prompt.  
```
Paper Mario Sample editor v0.1
Created by TGE, partially based on the script created by Nisto.
Please give credit where is due.

Usage:
samptool.exe <sounddir> -<mode> <dir> [<optarg>]

Where:
     <sounddir>  Path to the directory containing the samp and sdir files. (required)
     <mode>      Tool mode. See below. (required)
     <dir>       Path to directory to load from or write to depending on the mode. (required)
     [<optarg>]  Arguments whose usage differs depending on the mode. (optional)

Modes are:
     -x  Extracts the samples to <dir> in DSPADPCM format.
     -u  Updates the sdir and samp files using the dsp files in the folder specified in <dir>
         and saves the new files to the path specified by [<optargs>] if specified, otherwise
         the files are saved to the <sounddir> and the folder name is used as the base file name.

Example usage:
samptool.exe "D:\Sound" -x "D:\Sound\Out"
samptool.exe "D:\Sound" -u "D:\Sound\Dspfiles" "D:\Sound\pmario_new"
```
# Editing sound files
The sound files it extracts are in ``.dsp`` format.  
You can use [this fork of Audacity](https://github.com/jackoalan/audacity/releases/tag/v2.3.0) to export sounds as .dsp.  
More info in [this GbaTemp thread](https://gbatemp.net/threads/dspadpcm-dsp-audio-encoding-made-easy.390305/).  
You can probably also use [foobar2000](https://www.foobar2000.org/windows) with the [vgmstream plugin](https://www.foobar2000.org/components/view/foo_input_vgmstream) to listen to the .dsp files, maybe, I don't remember.  
If not, then that Audacity fork should do the trick.