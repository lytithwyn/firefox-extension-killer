# Firefox Extension Killer

This basically just gives the user a nice way to manually kill Firefox extensions.
The Mozilla documentation clearly lists all the places from which extensions can be
loaded but it's very tedious to check all the locations manually.  This tool simply
scans all those locations and gives a unified list of the extensions it finds.

The removal process at this point is "dumb", and works like this:

| Extension Load Type | Removal Process |
|----------------|-----------------|
|xpi| deletes the .xpi file|
|directory|the directory is recursively deleted|
|registry|the registry value is removed, files referenced remain intact|

The removal of registry-loaded extensions will probably change in the future to allow
the user to delete the file referenced (or likely its entire containing directory) so that the malware 
is more completely removed, but we have to be careful because I suppose an extension could be 
loaded from a "Program Files" directory and we don't want to risk deleting a functioning uninstall program.

## Prerequisites

* SharpDevelop (sort of) - I developed this in SharpDevelop, so a project file is included.  I didn't provide any other compilation
script or Makefile or anything since C# isn't my big thing and I don't really care to spend much time on learning about its workflow.
As long as you don't mind compiling this by hand, you don't need SharpDevelop.

* Ionic.Zip.Reduced - I didn't include the binary in the distribution because I didn't want to worry about licensing.  If you
use the SharpDevelop project, you can expand the "Resources" dictory of the project tree, then right click on "Ionic.Zip.Reduced"
and click "Refresh".  This will download the binary to the correct dirctory for you.