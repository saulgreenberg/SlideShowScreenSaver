**SlideShowScreenSaver Description**
=======
- This screensaver plays a slide show of jpeg images and video files, randomly selected from a folder (and its sub-folders) of your choice. That folder is searched for all images and videos on startup.
- Photo and video transitions are by a cross dissolve 
- Tested on a folder containing an extremely large number of images (> 100,000)
- Settings options includes:
     - choose the folder containing your desired images/videos (default is your Pictures folder)
     - choose the length of time to display each image/video
     - choose whether the slide show should include videos
     - set a duration for limiting video playback length (optional)
     - whether or not to show the image file / folder name, and if so whether some or all of the path should be included.

**Shortcuts**
=======
- Spacebar: pause / resume the slide show (a paused show will auto-resume after 1 minute)
- Left/Right arrow: pause the slide show and manualy navigate to the previous/next slide
- Any other key or mouse press: exit the screen saver
- When paused, video controls will appear atop a video allowing you to play and do other video operations.

**Installation instructions**
=======
If you just want the executable, download and unzip SlideShowScreenSaver.zip, and start at step 4.

1. Clone and build the program
2. Go to the bin folder 
3. Change the exe suffix: from SlideShowScreenSaver.exe to SlideShowScreenSaver.scr
4. Right click on SlideShowScreenSaver.scr and choose install.
5. If a dialog box appears that asks you to install .Net... (otherwise go to 6)
  *.Net is the runtime platform used by SlideShowScreenSaver. It is a standard Microsoft product that you should only have to install once. * 
     -  Download  the software from the Microsoft web site as instructed by clicking the button in the dialog box
     -  Open the softwar, and follow the installation  instructions (should be very straight forward to do). 
     - Repeat Step 4, which should now work. 
6. The Windows screensaver settings panel should appear. Adjust the settings to point to the folder (and sub-folders) containing your jpeg images.

**Programming notes**
=======
- Uses C#,  WPF and built atop of .Net using Visual Studio 2026.
- To update RequiredDlls in future (e.g. when FFME or FFMEVideoPlayerControlLib is updated): rebuild FFMEVideoPlayerControlLib, then copy the new DLLs into RequiredDlls manually.
- Thanks to various others who provided online examples of how to code a screensaver. I used and modified bits of their code to create this screensaver.
- Thanks to Claude, which was used in a later version to include videos in the SlideShowScreenSaver

**Creator**
=======
Saul Greenberg
saul@ucalgary.ca