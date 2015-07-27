# TextToSpeechInterlacer

Will create a multi-language text-to-speech audiobook in .wav

### Input text file

 en: To be or not to be?
 fr: Etre ou non etre, merde!
 
 en: Die, Macbeth!
 fr: Meurs, Macbeth, le connard!
 
 en: Thou shalt not kill me, imbecile
 fr: baise mon fesse, imbecile!

### Config

Config this with TtsMultilang.exe.config (App.config)

 - `file_in` - input txt file
 - `file_out` - output .wav file (numbers will be appended when outputting multiple audio files for long texts)
 - `file_encoding` - specify "utf-8", especially in case your input file contains non-Ascii characters (e.g. russian)
 - `lang1` - each line in prefixed with `lang1` + `COLON` => will be read in language 1
 - `lang2` - each line in prefixed with `lang2` + `COLON` => will be read in language 2
 - `lang1_name` - part of a name of language 1, enough to find it (e.g. "Hortense", will be enough to find and select "Microsoft Hortense Desktop - French")
 - `lang2_name` - part of a name of language 2, enough to find it
 - `lang1_rate` - how fast read leanguage 1 (-4 to 4)
 - `lang2_rate` - how fast read leanguage 2 (-4 to 4)

#### Optional options

 - `tmpWav` - template filename for saving each line. These are then concatenated using NAudio and deleted. Sorry, disk!
 - `maxBytesPerFile` - split output wav (`file_out`) when the size of each one exceeds this many bytes
