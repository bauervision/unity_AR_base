# unity_AR_base
Simple starting point for AR apps

This project uses AR Foundation, and is built off their samples for initial setup.  
So it is recommended to use Unity 2019.3.3f1.

## What it includes
* All the required AR Foundation objects in place, to include the updated AR Raycast Manager which is missing from almost all the current tutorials.  
* Simple click to place behavior script
* UI that counts all the objects you've spawned
* UI Button to clear them.
* In editor customizations: spawn single mesh, delete previous when spawning
* Select and deselect spawned objects, with color changing
* UI display of data stored on the selected objects
* WIP: fetching data from url to load on spawned objects rather than hard coding it

## WIP
Currently working on the following features:
* Spawn from a list of objects displayed on the UI
* Fetch data from url to set on models for display
* Add a ghost model so user can verify before spawning
* Set UI to block spawn events
* Spawn on surface of model instead of just on AR planes
* Move spawned model over the surface
* As AR planes are generated, save that data for reuse


## Setup
* Once Unity is installed, create a new 3D project, name it whatever you want, open the project and switch it over to an Android platform in Build Settings.
* Download this repo, and then copy it right overtop of your new project and replace the files.
* You will get a couple of errors in the console, you can simply clear them after the new files have been imported.

## Clean up
Now that you have the updated template copied into your project, there is a couple of small things to tidy up.

### Prep work
* Open Build Settings / Player Settings 
* Player ->  set your Company values, and icon
* Player / Icon / Adaptive -> set your icon
* Player / Other Settings / Identification -> set your own package name, just clear out `BauerVision.ARBase` and set your own. Be sure to leave the `com.` so for ex, `com.MyCompany.MyAppName`

### Deploy
Now you can Build your apk.

#### APK
Build Settings -> Build. If you just want to push out the apk.

#### Debugging
If you want to see your real time debugging inside of Unity's console:

* VSCode -> Set the debugger to the Unity Editor, this will create the launch.json that you need
* Unity -> Build Settings -> Enable Development Build, and Script Debugging for Android
* Build and Run.  This will then search for compatible devices ( make sure yours is connected at this point ), and will launch the file directly on your device.
* In order to see your logs inside of Unity, you need to switch the Console target from Editor to your connected Android device.

> Of course, place your Debug.Logs() where desired :)

