# unity_AR_base
Simple starting point for AR apps

This project uses AR Foundation, and is built off their samples for initial setup.  
So it is recommended to use Unity 2019.3.3f1.

## What it includes
* All the required AR Foundation objects in place, to include the updated AR Raycast Manager which is missing from almost all the current tutorials.  
* Simple click to place behavior script
* UI that counts all the objects you've spawned
* Button to clear them.

## Setup
* Once Unity is installed, create a new 3D project, name it whatever you want, open the project and switch it over to an Android platform in Build Settings.
* Download this repo, and then copy it right overtop of your new project and replace the files.
* You will get a couple of errors in the console, you can simply clear them after the new files have been imported.

## Build the app
Now that you have the updated template copied into your project, there is a couple of small things to tidy up.

### Prep
* Open Build Settings / Player Settings 
* Player ->  set your Company values, and icon
* Player / Icon / Adaptive -> set your icon

### Deploy
Now you can Build your apk.

Build Settings -> Build.

