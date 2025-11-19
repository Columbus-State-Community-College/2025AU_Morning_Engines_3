THIS IS - - - - - - - 
CSCC Butler Tool, specially formulated for your team's game!
There are `mac-` and `win-` variant scripts to use.
It can update your game on Itch.io and automatically sets the version number.
You should already be an Admin on your team's Itch page if you are reading this!
You are probably your team's build manager. Thanks for the help!

INSTALLATION - - - - - - -
- In order to use this tool, you will need to install the following:
- - Node.js LTS (Recommended, latest is 22), https://nodejs.org/en/
- - - If you're a programmer, or you have other reasons to expect to ever use NodeJS again in your life, it is recommended to install node via Node Version Manager (https://github.com/nvm-sh/nvm) or something similar, to avoid very annoying uninstall process that forces you to go deep in your computer's filesystem to fully get rid of directly-installed Node versions.

RECOMMENDATION - - - - - -
- Itch.io Desktop App allows your team to get automatic patch updates of your game whenever you push builds, 
- - https://itch.io/app

CREATE FOLDERS - - - - - -
- Git won't let empty folders be shared over repos. You will need to create the following folders, with exactly these names, inside this butler-uploader folder. (They should be alongside the butler folder)
- - linux
- - mac
- - win 
- - win32

RUNNING FILES - - - WINDOWS / MAC - - - 
### For all of the following steps, you should use the win- or mac- version of the script listed, based on what computer you have.
- If you are on a Windows, you can simply double-click on the corresponding win-.bat batch files
- If you are on Mac, open a terminal shell (command line) and point it at the folder this README is in. Then call `sh mac-1-check-dependencies` etc. for each step.

CONFIRM SETUP - - - - - - - 
- Just to make sure everything's set up right...
- - run 1-check-dependencies.bat
- - - This should print out your Itch and Node version numbers.
- - - If it does not work right, consider restarting your computer or reinstalling the tools.

AUTHENTICATE WITH ITCH - - - - - - -
- Gotta log in to your Admin account!
- - run 2-auth-login.bat
- - - It will open a link in your browser, make sure you're logged in to your itch account!
- - - If you need to log back out, use 2-auth-logout.bat and follow the prompts.

ALSO MAKE SURE YOUR ITCH ACCOUNT EMAIL IS VERIFIED - - - - - - -
- https://itch.io/user/settings/email-addresses and validate your email, or the script won't run.

PREPARE YOUR BUILDS - - - - - - -
- Place your Windows, Mac, and Linux Unity Builds in the win/, mac/, and linux/ folders, respectively
- - Make sure the folders contain ONLY the files you want the public to have access to.
- - You can include more files than the base files, for example a README or credits file.
- Following is the MINIMUM structure you should have in the butler-tool folders, here for reference.
- - - win32 (optional, Intel 32-bit Windows)
- - - - YOUR-GAME_Data
- - - - YOUR-GAME.exe
- - - - MonoBleedingEdge
- - - - UnityCrashHandler32.exe
- - - - UnityPlayer.dll
- - - win (REQUIRED, Intel 64-bit Windows)
- - - - YOUR-GAME_Data
- - - - YOUR-GAME.exe
- - - - MonoBleedingEdge
- - - - UnityCrashHandler64.exe
- - - - UnityPlayer.dll
- - - linux (REQUIRED, x86_64)
- - - - YOUR-GAME_Data
- - - - YOUR-GAME.exe
- - - - UnityPlayer.so 
- - - mac (REQUIRED, intel 64-bit + apple silicon Mac)
- - - - mac.app
- - - - - Contents
- - - - - - _CodeSignature
- - - - - - Frameworks
- - - - - - MacOS
- - - - - - MonoBleedingEdge
- - - - - - Resources
- - - - - - Info.plist
- - - web (Different class / setup)
- - - - No need to worry about this one.

FINALLY - - - - - - -
- Run `3-upload.bat` if you are providing win32, and `3-upload-no-win32.bat` otherwise.
- - - This process has already been configured for your team and project
- - - It will automatically put an appropriate version number on your project
- - - 1.x.y, where
- - - - "1" for the fact that it's "public" and the primary development effort.
- - - - - "x" for the number of full weeks since the project was started.
- - - - - - "y" is a long number representing the day, hour, and minute.
- - - See: https://semver.org/
- - - - - Not actually following these concepts to the letter, but these automatic version numbers contain a lot of information anyway!

Hope the tool runs without issues for you!
-Tom