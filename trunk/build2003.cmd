@echo off
echo *************************************************************
echo ********************Building*********************************
echo *************************************************************

REM ***********SETTING COMPILER OPTIONS***************
set CL_OPTIONS=^
    /nologo^
    /Zi^
    /clr^
    /TP^
    /Zl^
    /W3^
    /GS^
    /EHsc^
    /FD^
    /Gm-
set CL_DEBUG_OPTIONS=^
    /D "WIN32"^
    /D "_DEBUG"^
    /D "_WINDLL"^
    /D "_UNICODE"^
    /D "UNICODE"^
    /Od^
    /MTd^
    /Fo"Debug\\"^
    /Fd"Debug/vc70.pdb"
set CL_RELEASE_OPTIONS=^
    /D "WIN32"^
    /D "NDEBUG"^
    /D "_WINDLL"^
    /D "_UNICODE"^
    /D "UNICODE"^
    /O2^
    /MT^
    /Fo"Release\\"^
    /Fd"Release/vc70.pdb"

set LINKER_LIBRARIES=^
    nochkclr.obj^
    mscoree.lib^
    msvcrt.lib^
    mapi32.lib^
    kernel32.lib^
    user32.lib^
    gdi32.lib^
    winspool.lib^
    comdlg32.lib^
    advapi32.lib^
    shell32.lib^
    ole32.lib^
    oleaut32.lib^
    uuid.lib^
    odbc32.lib^
    odbccp32.lib
set LINKER_OPTIONS=^
    /DLL^
    /DEBUG^
    /FIXED:No^
    /noentry^
    /INCREMENTAL:NO
set LINKER_DEBUG_OPTIONS=/ASSEMBLYDEBUG 
set LINKER_RELEASE_OPTIONS=

set CSC_OPTIONS=^
    /nologo^
    /noconfig^
    /unsafe+^
    /warn:4
set CSC_DEBUG_OPTIONS=^
    /define:TRACE;DEBUG^
    /debug+^
    /debug:full^
    /optimize-
set CSC_RELEASE_OPTIONS=^
    /define:TRACE^
    /debug+^
    /debug:pdbonly^
    /optimize+

REM ***********CREATE OUTPUT DIRECTORIES***************
set DEBUG_BINDIR=%CD%\bin.2003\Debug
set RELEASE_BINDIR=%CD%\bin.2003\Release
mkdir %DEBUG_BINDIR%
mkdir %RELEASE_BINDIR%

REM **************BUILD GOOGLEEMAILUPLOADER******************
pushd GoogleEmailUploader
echo _____Building GoogleEmailUploader_____
set GOOGLEEMAILUPLOADER_REFERENCES=^
    /reference:System.dll^
    /reference:System.Drawing.dll^
    /reference:System.Windows.Forms.dll^
    /reference:System.XML.dll
set GOOGLEEMAILUPLOADER_FILES=^
    AssemblyInfo.cs^
    GoogleEmailUploaderModel.cs^
    HttpInterface.cs^
    MailClientInterfaces.cs^
    MailUploader.cs^
    Program.cs^
    Resources.cs^
    SigninLogic.cs^
    SigninView.cs^
    LKGStatePersistence.cs^
    SelectView.cs^
    UploadView.cs
mkdir obj
resgen Resources.resx obj\GoogleEmailUploader.Resources.resources
Csc.exe^
    %CSC_OPTIONS%^
    %CSC_DEBUG_OPTIONS%^
    %GOOGLEEMAILUPLOADER_REFERENCES%^
    /out:"%DEBUG_BINDIR%\GoogleEmailUploader.exe"^
    /resource:obj\GoogleEmailUploader.Resources.resources^
    /target:winexe^
    %GOOGLEEMAILUPLOADER_FILES%
Csc.exe^
    %CSC_OPTIONS%^
    %CSC_RELEASE_OPTIONS%^
    %GOOGLEEMAILUPLOADER_REFERENCES%^
    /out:"%RELEASE_BINDIR%\GoogleEmailUploader.exe"^
    /resource:obj\GoogleEmailUploader.Resources.resources^
    /target:winexe^
    %GOOGLEEMAILUPLOADER_FILES%
echo _____Done building GoogleEmailUploader_____
popd

REM *************BUILD OUTLOOK PLUGIN******************
pushd OutlookClient
echo _____Building OutlookClient_____
mkdir Debug
mkdir Release
set OUTLOOKCLIENT_REFERENCES=^
    /FU mscorlib.dll^
    /FU System.dll
set OUTLOOKCLIENT_FILES=^
    assembly_info.cc^
    outlook_client.cc
cl.exe^
    %CL_OPTIONS%^
    %CL_DEBUG_OPTIONS%^
    /AI %DEBUG_BINDIR%^
    %OUTLOOKCLIENT_REFERENCES%^
    %OUTLOOKCLIENT_FILES%^
    /link^
        %LINKER_OPTIONS%^
        %LINKER_DEBUG_OPTIONS%^
        %LINKER_LIBRARIES%^
        /OUT:"%DEBUG_BINDIR%\OutlookClient.dll"^
        /PDB:"%DEBUG_BINDIR%\OutlookClient.pdb"
cl.exe^
    %CL_OPTIONS%^
    %CL_RELEASE_OPTIONS%^
    /AI %RELEASE_BINDIR%^
    %OUTLOOKCLIENT_REFERENCES%^
    %OUTLOOKCLIENT_FILES%^
    /link^
        %LINKER_OPTIONS%^
        %LINKER_RELEASE_OPTIONS%^
        %LINKER_LIBRARIES%^
        /OUT:"%RELEASE_BINDIR%\OutlookClient.dll"^
        /PDB:"%RELEASE_BINDIR%\OutlookClient.pdb"
echo _____Done building OutlookClient_____
popd

REM **********BUILD OUTLOOK EXPRESS PLUGIN*************
pushd OutlookExpressClient
echo _____Building OutlookExpressClient_____
mkdir Debug
mkdir Release
set OUTLOOKEXPRESSCLIENT_REFERENCES=^
    /FU mscorlib.dll^
    /FU System.dll
set OUTLOOKEXPRESSCLIENT_FILES=^
    assembly_info.cc^
    outlook_express_client.cc
cl.exe^
    %CL_OPTIONS%^
    %CL_DEBUG_OPTIONS%^
    /AI %DEBUG_BINDIR%^
    %OUTLOOKEXPRESSCLIENT_REFERENCES%^
    %OUTLOOKEXPRESSCLIENT_FILES%^
    /link^
        %LINKER_OPTIONS%^
        %LINKER_DEBUG_OPTIONS%^
        %LINKER_LIBRARIES%^
        /OUT:"%DEBUG_BINDIR%\OutlookExpressClient.dll"^
        /PDB:"%DEBUG_BINDIR%\OutlookExpressClient.pdb"
cl.exe^
    %CL_OPTIONS%^
    %CL_RELEASE_OPTIONS%^
    /AI %RELEASE_BINDIR%^
    %OUTLOOKEXPRESSCLIENT_REFERENCES%^
    %OUTLOOKEXPRESSCLIENT_FILES%^
    /link^
        %LINKER_OPTIONS%^
        %LINKER_RELEASE_OPTIONS%^
        %LINKER_LIBRARIES%^
        /OUT:"%RELEASE_BINDIR%\OutlookExpressClient.dll"^
        /PDB:"%RELEASE_BINDIR%\OutlookExpressClient.pdb"
echo _____Done building OutlookExpressClient_____
popd

REM ************BUILD THUNDERBIRD PLUGIN***************
pushd ThunderbirdClient
echo _____Building ThunderbirdClient_____
set THUNDERBIRDCLIENT_REFERENCES=^
    /reference:System.dll
set THUNDERBIRDCLIENT_FILES=^
    AssemblyInfo.cs^
    ThunderbirdConstants.cs^
    ThunderbirdEmailEnumerable.cs^
    ThunderbirdEmailEnumerator.cs^
    ThunderbirdEmailMessage.cs^
    ThunderbirdFolder.cs^
    ThunderbirdProfile.cs^
    ThunderbirdStore.cs^
    ThunderbirdClient.cs
mkdir obj
Csc.exe^
    %CSC_OPTIONS%^
    %CSC_DEBUG_OPTIONS%^
    %THUNDERBIRDCLIENT_REFERENCES%^
    /reference:"%DEBUG_BINDIR%\GoogleEmailUploader.exe"^
    /out:"%DEBUG_BINDIR%\ThunderbirdClient.dll"^
    /target:library^
    %THUNDERBIRDCLIENT_FILES%
Csc.exe^
    %CSC_OPTIONS%^
    %CSC_RELEASE_OPTIONS%^
    %THUNDERBIRDCLIENT_REFERENCES%^
    /reference:"%RELEASE_BINDIR%\GoogleEmailUploader.exe"^
    /out:"%RELEASE_BINDIR%\ThunderbirdClient.dll"^
    /target:library^
    %THUNDERBIRDCLIENT_FILES%
echo _____Done building ThunderbirdClient_____
popd

REM ************BUILD INSTALLER***********************
makensis /DDEBUG Installer.nsi 
makensis Installer.nsi 


echo *************************************************************
echo **************************Building done**********************
echo *************************************************************
