## Hyperbyte   
Welcome to the Hyperbyte Github Repository. <br/>
Since I used some open source codes to develop this, I decided to make it open source too. <br/>
It's realiable since it download patches one by one. In case of failures, it tries to download again. </br>
This is an auto updater for general types of softwares.

## Requirements
- .Net Framework 4.5 or higher
- Network Connection
- Web Server

## How to
**1.** Program.cs (Autoupdater and selfupdater)

        > Here you can enable or disable the Notice Panel of the auto updater
        > (false = disable | true = enable)
        private static bool enableNotice = false;
        
        > Here you can set your Autoupdater window's title
        private static string windowTitle = "Hyperbyte Patcher";
        
        > Here you can set the executable's name that will be started once the patch process is completed
        private static string appExecutable = "MyApp.exe";
        private static string appArguments = "-l 3";
        
        > Here you MUST set the value of patcherExecutable variable to the autoupdater's executable name
        > It must be EXACTLY the same!
        private static string patcherExecutable = "hyperbytepatcher.exe";
        private static string patcherArguments = "";

**2.** App.config

        > Here you must set to the url where it has the packages to download
        <add key="patchesWebPath" value="http://huenato-club.umbler.net/hyperbyte/patch/" />

**3.** Compile your project

**4.** About the web server, in the url that you set in your App.config, you must have these files
- patchlist, notice (basically its a txt without format - PS: notice will only be loaded if you set enableNotice to true -) and packages in .zip (you can compress using Windows's default zip compressor).
- Write the files that must be downloaded in patchlist file, ordered by numbers
- Separete the number from the package name with tab

        Line 1. 100 pkg01.zip
        Line 2. 101 pkg01_fix.zip
        Line 3. 102 pkg02_new.zip
        Line 4. 103 patcher_update01.hyp

- .hyp files will be loaded by the selfupdater programm to update the auto updater
- To make a .hyp file, just compress the new patcher executable with zip and rename the format to .hyp
- Once updated, the selfupdater will restart the patcher
- All files downloaded by the patcher will be extracted to the same folder where it is running
- Extracted files will overwrite duplicate files
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
