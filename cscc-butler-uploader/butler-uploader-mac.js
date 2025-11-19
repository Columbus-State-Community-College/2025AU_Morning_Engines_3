/* eslint-disable no-undef */
/* eslint-disable @typescript-eslint/no-var-requires */
// This uses child_process to run Butler
// https://nodejs.org/api/child_process.html#child_process_child_process_spawn_command_args_options
var path = require("path");
var execFile = require("child_process").execFile;

// NodeJS application that
// butler push mygame user/mygame:win32 --userversion 1.1.0(-final)
// See README for version number construction details

// EXAMPLE projectstart = new Date(2022, 8, 28, 13, 0, 0, 0).valueOf();
// This would be 2022 September 28, 1:00:00.000 PM

// DAYLIGHT SAVINGS TIME adjustment. Noon = 1:00 after the time change.
const projectstart = new Date(2025, 9, 29, 9, 0, 0, 0).valueOf();
// How many full weeks will the project last? Puts '-final' on the version number after that point.
const projectweeks = 6;

const username = "cscc-mini-projects";

// ATTENTION BUILD MANAGER: YOU MAY NEED TO MODIFY THESE LINES FOR YOUR GAME
// ESPECIALLY IF YOU CHANGE THE GAME URL ON itch.io
const gamename = "2025au-m3-you-cant-park-there";
// If you're on Linux, you may need to change this...
const butler_command_file = "./butler/versions/15.24.0/mac/butler";
// END OF ATTENTION BLOCK

function CurrentVersionString() {
  const now = Date.now();
  var msLeft = now - projectstart;
  const msPerMinute = 1000 * 60;
  const msPerHour = msPerMinute * 60;
  const msPerDay = msPerHour * 24;
  const msPerWeek = msPerDay * 7;
  const fullWeeks = Math.floor(msLeft / msPerWeek).toString(10);
  msLeft -= fullWeeks * msPerWeek;
  const fullDays = Math.floor(msLeft / msPerDay).toString(10);
  msLeft -= fullDays * msPerDay;
  const fullHours = Math.floor(msLeft / msPerHour)
    .toString(10)
    .padStart(2, "0");
  msLeft -= fullHours * msPerHour;
  const fullMinutes = Math.floor(msLeft / msPerMinute)
    .toString(10)
    .padStart(2, "0");
  const base_string = `1.${fullWeeks}.${fullDays}${fullHours}${fullMinutes}`;
  if (fullWeeks >= projectweeks - 1) {
    return base_string + "-final";
  }
  return base_string;
}

function UploadTeamGame(platform, version_string, cb) {
  var exePath = path.resolve(__dirname, butler_command_file);
  const butler_command_args = [
    "push",
    platform,
    `${username}/${gamename}:${platform}`,
    `--userversion`,
    version_string,
  ];
  //const butler_command = `/butler/versions/15.21.0/butler.exe push ${platform} ${username}/${gamename}:${platform} --userversion ${version_string}`;
  console.log(`RUNNING ${exePath} ${JSON.stringify(butler_command_args)}`);
  execFile(exePath, butler_command_args, (error, stdout, stderr) => {
    if (error) {
      console.error(`exec error: ${error}`);
      return;
    }

    console.log(`stdout: ${stdout}`);
    console.error(`stderr: ${stderr}`);
    console.log(`FINISHED ${JSON.stringify(butler_command_args)}\n\n`);
    cb(version_string);
  });
}
function AllDone(version_string) {
  console.log(`\nWoo! All done uploading ${version_string}!`);
}

function ButlerUploadLinux(version_string) {
  UploadTeamGame("linux", version_string, AllDone);
}
function ButlerUploadMac(version_string) {
  UploadTeamGame("mac", version_string, ButlerUploadLinux);
}
function ButlerUploadWin(version_string) {
  UploadTeamGame("win", version_string, ButlerUploadMac);
}
function ButlerUploadWin32(version_string) {
  UploadTeamGame("win32", version_string, ButlerUploadWin);
}
/* Web HTML game hack
function ButlerUploadHTMLGame(version_string) {
  UploadTeamGame("web", version_string, AllDone);
}
*/

function RunButlerUploader() {
  const now_date = new Date(Date.now());
  const proj_date = new Date(projectstart);
  const version_string = CurrentVersionString();
  console.log("\nCalculating version number... for " + projectweeks + " weeks");
  console.log(`now = ${now_date.toDateString()} @ ${now_date.toTimeString()}`);
  console.log(
    `project start = ${proj_date.toDateString()} @ ${proj_date.toTimeString()}`
  );
  console.log("So, version number is " + version_string);
  const args = process.argv;
  if (args.length > 2) {
    console.log(
      "No Windows-32 build being supplied. Poor forsaken gamers...\n\n"
    );
    ButlerUploadWin(version_string);
  } else {
    console.log(
      "You are awesome for providing a 32-bit Windows binary for deprived gamers!\n\n"
    );
    ButlerUploadWin32(version_string);
  }
  /* Web HTML game hack
  ButlerUploadHTMLGame(version_string);
    */
}
RunButlerUploader();
