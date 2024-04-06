**SlideShowScreenSaver Description**
- This screensaver plays a slide show of jpeg images, randomly selected from a folder (and its sub-folders) of your choice,
  where that folder is searched for all images on startup.
- Photo transitions are by a cross dissolve 
- Tested on a folder containing an extremely large number of images (> 10,000)
- Settings options includes:
     - choose the folder containing your desired images (default is your Pictures folder)
     - choose the length of time to display each image
     - whether or not to show the image file / folder name, and if so whether some or all of the path should be included.

<<<<<<< HEAD
**Shortcuts**
=======
Others:
 - A paused slide show will resume after 1 minute
 
Shortcuts:
>>>>>>> 62a90dd (Modified to auto-resume in 1 minute after pausing)
- Spacebar: pause / resume the slide show
- Left/Right arrow: pause the slide show and manualy navigate to the previous/next slide
- Any other key or mouse press: exit the screen saver

**Installation instructions**

If you just want the executable, download and unzip SlideShowScreenSaver.zip, and start at step 4.

1. Clone and build the program
2. Go to the bin folder 
3. Change the exe suffix: from SlideShowScreenSaver.exe to SlideShowScreenSaver.scr
4. Right click on SlideShowScreenSaver.scr and choose install.
5. If a dialog box appears that asks you to install .Net 6... (otherwise go to 6)
  *.Net 6 is the runtime platform used by SlideShowScreenSaver. It is a standard Microsoft product that you should only have to install once. * 
     -  Download  the software from the Microsoft web site as instructed by clicking the button in the dialog box
     -  Open the softwar, and follow the installation  instructions (should be very straight forward to do). 
     - Repeat Step 4, which should now work. 
6. The Windows screensaver settings panel should appear. Adjust the settings to point to the folder (and sub-folders) containing your jpeg images.

**Programming notes**
- Uses C#,  WPF and built atop of .Net 6.0 using Visual Studio 2022.
- Thanks to various others who provided online examples of how to code a screensaver. I used and modified bits of their code to create this screensaver.
