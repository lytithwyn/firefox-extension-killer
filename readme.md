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