@echo off

::============================================================
::first check if containing path is a unity project:
::============================================================
SET pathAssets=%~dp0%Assets\
if not exist "%pathAssets%" (
  echo Could not create symlinks - Assets folder missing
  pause
  exit
)
SET pathPackages=%~dp0%Packages\
if not exist "%pathPackages%" (
  echo Could not create symlinks - Packages folder missing
  pause
  exit
)
SET pathProjectSettings=%~dp0%ProjectSettings\
if not exist "%pathProjectSettings%" (
  echo Could not create symlinks - ProjectSettings folder missing
  pause
  exit
)
SET pathUserSettings=%~dp0%UserSettings\
if not exist "%pathUserSettings%" (
  echo Could not create symlinks - UserSettings folder missing
  pause
  exit
)
echo path is a valid unity project

::============================================================
::create directory for project copy:
::============================================================
SET copypath=%~dp0%
IF %copypath:~-1%==\ SET copypath=%copypath:~0,-1%
SET copypath=%copypath% - symlinked\
if exist "%copypath%" (
  echo copy directory already exists
) else (
  echo copy directory does not exist - create directory %copypath%
  mkdir "%copypath%"
)

::============================================================
::create symlinks if they do not exist:
::============================================================
SET copypathAssets=%copypath%Assets\
if not exist "%copypathAssets%" (
  echo create symlink for Assets folder
  mklink /d "%copypathAssets%" "%pathAssets%"
) else (
  echo symlink for Assets folder already exists
)
SET copypathPackages=%copypath%Packages\
if not exist "%copypathPackages%" (
  echo create symlink for Packages folder
  mklink /d "%copypathPackages%" "%pathPackages%"
) else (
  echo symlink for Packages folder already exists
)
SET copypathProjectSettings=%copypath%ProjectSettings\
if not exist "%copypathProjectSettings%" (
  echo create symlink for ProjectSettings folder
  mklink /d "%copypathProjectSettings%" "%pathProjectSettings%"
) else (
  echo symlink for ProjectSettings folder already exists
)
SET copypathUserSettings=%copypath%UserSettings\
if not exist "%copypathUserSettings%" (
  echo create symlink for UserSettings folder
  mklink /d "%copypathUserSettings%" "%pathUserSettings%"
) else (
  echo symlink for UserSettings folder already exists
)

SET pathConfig=%~dp0%_config\
SET copypathConfig=%copypath%_config\
if not exist "%copypathConfig%" (
  echo create symlink for _config folder
  mklink /d "%copypathConfig%" "%pathConfig%"
) else (
  echo symlink for _config folder already exists
)

pause
