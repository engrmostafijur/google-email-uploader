@echo off
rmdir /S /Q bin.2003
rmdir /S /Q bin.2005
rmdir /S /Q OutlookClient\Debug
rmdir /S /Q OutlookClient\Release
rmdir /S /Q OutlookExpressClient\Debug
rmdir /S /Q OutlookExpressClient\Release
rmdir /S /Q GoogleEmailUploader\obj
rmdir /S /Q ThunderbirdClient\obj
rmdir /S /Q GEMUTestScript\obj

del /S /Q /F *.user
del /Q /F /A:H *.suo
del /Q /F *.ncb



