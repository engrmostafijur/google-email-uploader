## Development ##
For development we use VS 2005. However one have to be sure not to be using any of the .NET 2.0 specifics so that GoogleEmailUploader works on targeted .NET 1.1.

Open GoogleEmailUploader.2005.sln in VS 2005.

## Command line build ##
VS 2003 environment must be set properly. This is done by running "C:\Program Files\Microsoft Visual Studio .NET 2003\Common7\Tools\vsvars32.bat" in command line.

Also you need to make sure that msis is installed on the machine. This is used to build the installer. Download from - [NSIS](http://nsis.sourceforge.net/Main_Page)

Use build2003.bat to build and clean.bat to cleanup.