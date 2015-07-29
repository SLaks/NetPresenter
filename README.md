#NetPresenter

NetPresenter is a synchronized network-based presentation app, intended for large events.  You can run NetPresenter on a number of computer on the same network, and they will all synchronize to display the same content at all times, controlled from any PC.

#Setup
NetPresenter is a standalone, stateless, serverless system, using reliable multicast instead of a central server to synchronize everything.  Therefore, there is no complicated setup step.  Instead, simply copy the EXE and DLL files, along with a directory of resources to present, to every computer, then run it.

The RDM protocol used for reliable multicast requires an optional MSMQ feature to be enabled in Windows, and requires that the program run as administrator.  On launch, NetPresenter will prompt you to enable this and restart as administrator.  If you say no, it will still work on all monitors of the local machine, but will not synchronize over a network.

As described below, NetPresenter presents media from subfolders of the current directory.  This directory structure must be identical on every computer in the network.  (running from a network share is fine, assuming there is sufficient bandwidth for every computer to load things at once)

#Usage
NetPresenter is built around the concept of a view, which is a single feature displayed on every monitor.  Right-clicking anywhere will open the menu (which can be used with the keyboard, mouse, or scroll wheel), which offers options to open a different view, as well as any commands offered by the current view.

NetPresenter has a number of views:

 - Intro: This view presents a background image stretched to fill the screen (eg, a wallpaper) and a foreground image centered within the screen (eg, text or a logo).  These images must be named `Background` and `Foreground` respectively, placed within the `Intro/` directory.  PNG and JPEG images are supported.
  - You can also create multiple pairs of background and foreground images by adding a common suffix to each pair (eg, `Background-Light.jpg` and `Foreground-Light.png`).  The view will then choose a pair at random each time it's opened (each monitor and computer may choose a different image).
 - Message: This view allows you to type a message and present it on every screen
 - Slideshow: This view presents JPEG images in a slideshow.  The arrow keys or scroll wheel will switch between images; every computer and monitor will always show the same image.  You can double-click on any point in the image to call attention to that point on every screen.
  - This view will offer a separate instance for every subdirectory containing images (except `Intro/`).
  - You can also select Start auto-play from the menu to automatically advance to the next image every 45 seconds.
 - Video: This view will present a sequence of videos.  Click or press space to pause or unpause; press left or right to switch videos.  Use the trackbar on the bottom for seeking.  The current video and position will be synchronized across every monitor and computer (subject to delays in loading videos on slower computers)
  - Like the slideshow, this view will offer a separate instance for every subdirectory containing videos.

You can also select Go offline from the menu to disconnect that computer from the network.  All monitors on that computer will still be synchronized, but it will not affect nor be affected by the rest of the network.