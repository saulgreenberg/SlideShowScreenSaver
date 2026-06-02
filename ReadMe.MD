**SlideShowScreenSaver Description**
=======

Benefits of using this Windows-based slide show screen saver:
- It plays a slide show of jpeg images and video files, randomly selected from a folder (and its sub-folders) of your choice. 
- It works even if there are an extremely large number of images or videos (tested with >100,000 files)
- It has a settings dialog that allows you to control essential features
- It is free
- It is a code-signed application (security)
- You can ask the developer fo  new features by submitting a Github issue
- As it is open source, other developers can examine, modify, and even fork the C#/WPF code
- It is easy to download and install (see Installation instructions below)

When  the slide show is running:
- images and videos are switched after a user-selected duration (in settings)
- videos are muted and played for only a user-selected duration , which means it works well with long videos files 
- users can skip forwards/backwards using the arrow keys
- user-selected metadata fields are displayed (e.g., folder and/or file name) discretely

When the slide show is paused (space bar):
- users can skip forwards/backwards using the arrow keys
- video controls appear and the video control is unmuted, allowing you full control over the video

- Settings options includes:
     - choose the folder containing your desired images/videos (default is your Pictures folder)
     - choose the length of time to display each image/video
     - choose whether the slide show should include videos
     - set a duration for limiting video playback length (optional)
     - whether or not to show the image file / folder name, and if so whether some or all of the file path should be included.

**Shortcuts**
=======
- Spacebar: pause / resume the slide show (a paused show will auto-resume after 1 minute)
- Left/Right arrow: pause the slide show and manualy navigate to the previous/next slide
- Any other key or mouse press: exit the screen saver
- When paused, video controls will appear atop a video allowing you to play and do other video operations.
    - video controls include play/pause, frame forward/backwards, mute/unmute, volume control, auto-repeat and others

**Installation instructions **
=======

1. Download the lastest release: 
    a. Click **SlideShowScreenSaver (latest)** in the **Releases** area on the right panel of the Github window, 
    b. Click the zip file **SlideShowScreenSaver-<version>.zip** 
    c. Unzip it.
2. Move the unzippped folder  to a location of your chosing. and open the  folder. You will see a large number of files - that is because we include Microsoft's .Net with the program to make it completely self-contained. 
3. Find the file **SlideShowScreenSaver.scr**
4. **Right click on SlideShowScreenSaver.scr** and choose **Install**.
6. The Windows screensaver settings panel should appear. Adjust the settings to point to the folder (and sub-folders) containing your jpeg mages and video files.

**Programming notes if rebuilding from source**
=======
- Uses C#,  WPF and built atop of .Net using Visual Studio 2026.
- To update RequiredDlls in future (e.g. when FFME or FFMEVideoPlayerControlLib is updated): rebuild FFMEVideoPlayerControlLib, then copy the new DLLs into RequiredDlls manually.
- Note that code-signing is only included in the pre-built version
- Thanks to various others who provided online examples of how to code a screensaver. I used and modified bits of their code to create this screensaver.
- Thanks to Claude, which was used in a later version to include videos in the SlideShowScreenSaver

**Creator**
=======
Saul Greenberg
saul@ucalgary.ca