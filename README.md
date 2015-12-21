## Hyperbyte   
Welcome to the Hyperbyte Github Repository. <br/>
Since I used some open source codes to develop this project, I decided to make it open source too. <br/>
It's realiable since it download patches one by one. In case of failures, it tries to download again. </br>
This is an auto updater for general types of softwares.

## Requirements
- .Net Framework 4.5 or higher
- Network Connection
- Web Server

## How to
**1.** Set both Program.cs of projects to whatever you want

        > Here you can enable or disable the Notice Panel on your form after compilation.
        > (false = disable | true = enable)
        private static bool enableNotice = false;
        
        > Here you can name the title of your programm window after compilation.
        private static string windowTitle = "Hyperbyte Patcher";
        
        > Here you can set which programm the auto updater should open after the patch process finishes.
        private static string appExecutable = "MyGame.exe";
        private static string appArguments = "-l 3";
        
        > Here you MUST set the value of patcherExecutable variable to the assembly name (name of executable) of the patcher
        > It must be EXACTLY the same!
        private static string patcherExecutable = "hyperbytepatcher.exe";
        private static string patcherArguments = "";

**2.** Set the App.config file of Hyperbyte Patcher Project to whatever you want

        > Here you must set  the value to the url where it has the patches to download
        <add key="patchesWebPath" value="http://huenato-club.umbler.net/hyperbyte/patch/" />

**3.** Compile your solution

**4.** About the web files, in the url that you set in your App.config, you must have the files
- patchlist
- notice (loaded only if you set enableNotice to true)
- packages in .zip (you can compress using Windows's default zip compressor)
- Write the files that must be downloaded in patchlist file, ordered by numbers
- Separete the number from the package name by pressing tab

        Line 1. 100 pkg01.zip
        Line 2. 101 pkg01_fix.zip
        Line 3. 102 pkg02_new.zip
        Line 4. 103 patcher_update01.hyp

- .hyp files will be loaded by the selfupdater programm to update the patcher
- To make a .hyp file, just compress the new patcher executable with zip and rename the format to .hyp
- Once updated, the selfupdater will restart the patcher where it stopped.

- There are some examples in www folder of this repository

## Screenshots

Program.cs

<img src="http://i.imgur.com/Hhp87XI.png"/>

App.config

<img src="http://i.imgur.com/FBaxKZX.png"/>

Enabled Notices

<img src="http://i.imgur.com/EwJxEiU.png"/>

Disabled Notices

<img src="http://i.imgur.com/MIHh6l9.png"/>

## License
Hyperbyte Patcher and Hyperbyte Selfupdater is released under the [MIT License](http://www.opensource.org/licenses/MIT).
