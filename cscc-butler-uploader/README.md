# cscc-itch-butler-uploader

Helper utility application for CSCC Game Development Capstone course uploads to Itch.io (but could be easily tweaked for anyone else)

NOTE: This is legacy instructions, kept for extra context. For more direct useful info, see the `__README_FIRST` updated.

This project currently has no dependencies other than Node itself. Planned to stay that way.

I will try to keep the onboard copy of /butler up-to-date also.

## Uploaded Games

This tool provides easy local folders to prep and upload versioned game updates to an Itch.io game page. It must be configured against a game page and admin username that already exist.

The default behavior is to generate version numbers around the full weeks, days, and time information since the beginning of the project. For example, a version 1.2.50734 had its upload initiated 2 weeks, 5 days, 7 hours and 34 minutes since the 'official start time' of the project. On the "last week" and beyond, the version will look like 1.xx.yyyyy-final.

Minimum required uploaded versions are `linux`, `mac`, and `win`. `win32` is an optional folder with its own separate kickoff script.

These requirements are class-driven, feel free to use this code as a starting point for your own project and rework the logic to your own liking.

## Configuring

There are hard-coded values near the top of `butler-uploader.js` that you can modify based on when the project started and how long it will last.

Then, for each team, the username and gamename should be set accordingly.

These Itch.io resources should have already been made, and the username set to an Itch Page Admin with a verified email.

## Group Coordinator Usage

You can zip up everything (except maybe package-lock.json) into a self-contained deliverable zip once it has been configured, and deliver to each configured group.

## Team Usage

Please refer to `__README_FIRST.txt` for user-facing instructions. Everything is boiled into the Node.js script and the clickable batch files.

## Credits / License / Etc.

Originally written by Thomas Allenbaugh 2022-10-14 for use in the Columbus State Community College Game Development Capstone.

Use at your own risk! Node.js and Itch.io Butler / Wharf are covered by their respective licenses.
