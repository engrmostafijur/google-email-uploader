; Copyright 2007 Google Inc.
;
; Licensed under the Apache License, Version 2.0 (the "License");
; you may not use this file except in compliance with the License.
; You may obtain a copy of the License at
;
;      http:;www.apache.org/licenses/LICENSE-2.0
;
; Unless required by applicable law or agreed to in writing, software
; distributed under the License is distributed on an "AS IS" BASIS,
;
; WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
; See the License for the specific language governing permissions and
; limitations under the License.

!include LogicLib.nsh


; HM NIS Edit Wizard helper defines
!define PRODUCT_NAME "Google Email Uploader"
!define PRODUCT_VERSION "1.1"
!define PRODUCT_PUBLISHER "Google Inc."
!define PRODUCT_WEB_SITE "code.google.com/p/google-email-uploader"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\GoogleEmailUploader.exe"
!define PRODUCT_VERSION_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\GoogleEmailUploader.exe\Version"
!define PRODUCT_UPDATER_REGKEY "Software\Google\Update\Clients\{84F41014-78F2-4ebf-AF9B-8D7D12FCC37B}"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"
!define PRODUCT_STARTMENU_REGVAL "NSIS:StartMenuDir"
!define DOT_MAJOR "1"
!define DOT_MINOR "1"
; Macro for version compare
!define VersionCompare `!insertmacro VersionCompareCall`

; MUI 1.67 compatible ------
!include "MUI.nsh"

; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "GoogleEmailUploader\OpenImageResources\GMailIcon.ico"
!define MUI_UNICON "GoogleEmailUploader\OpenImageResources\GMailIcon.ico"
!define ICONS_GROUP "Google Email Uploader"
!define OLD_ICONS_GROUP "GoogleEmailUploader"

; Variables
var VERSION_RESULT

!define MUI_STARTMENUPAGE_NODISABLE
!define MUI_STARTMENUPAGE_DEFAULTFOLDER "Google Email Uploader"
!define MUI_STARTMENUPAGE_REGISTRY_ROOT "${PRODUCT_UNINST_ROOT_KEY}"
!define MUI_STARTMENUPAGE_REGISTRY_KEY "${PRODUCT_UNINST_KEY}"
!define MUI_STARTMENUPAGE_REGISTRY_VALUENAME "${PRODUCT_STARTMENU_REGVAL}"

; Instfiles page
!insertmacro MUI_PAGE_INSTFILES
AutoCloseWindow true

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language files
!insertmacro MUI_LANGUAGE "English"

; MUI end ------

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
!ifdef DEBUG
OutFile "bin.2003\Debug\${locale}\GoogleEmailUploader_${PRODUCT_VERSION}_Setup.exe"
!else
OutFile "bin.2003\Release\${locale}\GoogleEmailUploader_${PRODUCT_VERSION}_Setup.exe"
!endif
InstallDir "$PROGRAMFILES\Google\Google Email Uploader"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

;Function to compare 2 versions
; R 0 if equal
; 1 if Version1 is newer
; 2 if Version2 is newer
; See macro below for usage.
Function VersionCompare
    Exch $1
    Exch
    Exch $0 
    Exch
    Push $2
    Push $3
    Push $4
    Push $5
    Push $6
    Push $7
 
    begin:
    StrCpy $2 -1
    IntOp $2 $2 + 1
    StrCpy $3 $0 1 $2
    StrCmp $3 '' +2
    StrCmp $3 '.' 0 -3
    StrCpy $4 $0 $2
    IntOp $2 $2 + 1
    StrCpy $0 $0 '' $2
 
    StrCpy $2 -1
    IntOp $2 $2 + 1
    StrCpy $3 $1 1 $2
    StrCmp $3 '' +2
    StrCmp $3 '.' 0 -3
    StrCpy $5 $1 $2
    IntOp $2 $2 + 1
    StrCpy $1 $1 '' $2
 
    StrCmp $4$5 '' equal
 
    StrCpy $6 -1
    IntOp $6 $6 + 1
    StrCpy $3 $4 1 $6
    StrCmp $3 '0' -2
    StrCmp $3 '' 0 +2
    StrCpy $4 0
 
    StrCpy $7 -1
    IntOp $7 $7 + 1
    StrCpy $3 $5 1 $7
    StrCmp $3 '0' -2
    StrCmp $3 '' 0 +2
    StrCpy $5 0
 
    StrCmp $4 0 0 +2
    StrCmp $5 0 begin newer2
    StrCmp $5 0 newer1
    IntCmp $6 $7 0 newer1 newer2
 
    StrCpy $4 '1$4'
    StrCpy $5 '1$5'
    IntCmp $4 $5 begin newer2 newer1

    equal:
    StrCpy $0 0
    goto end
    newer1:
    StrCpy $0 1
    goto end
    newer2:
    StrCpy $0 2
 
    end:
    Pop $7
    Pop $6
    Pop $5
    Pop $4
    Pop $3
    Pop $2
    Pop $1
    Exch $0
FunctionEnd
 
!macro VersionCompareCall _VER1 _VER2 _RESULT
     Push `${_VER1}`
     Push `${_VER2}`
     Call VersionCompare
     pop ${_RESULT}
!macroend

; Check to see if previous versions are installed
Function CheckAlreadyInstalled
   ClearErrors
   
   ReadRegStr $0 HKLM "${PRODUCT_VERSION_REGKEY}" ""
   IfErrors done
  
   ; Compare if this is a newer version
   ${VersionCompare} "${PRODUCT_VERSION}" $0 $VERSION_RESULT

   Strcmp $VERSION_RESULT "2" old checkmore
   old:
   MessageBox MB_OK "Another version of Google Email Uploader is already installed, If you wish to reinstall, please uninstall the existing version and run setup again."
   Quit

   checkmore:
   Strcmp $VERSION_RESULT "0" same done
   same:
   MessageBox MB_OK "Google Email Uploader is already installed, If you wish to reinstall, please uninstall the existing version and run setup again."
   Quit

   done:
   ;continue installation.

FunctionEnd

!define DOTNET_VERSION "2"
!define DOTNET_URL "http://www.microsoft.com/downloads/info.aspx?na=90&p=&SrcDisplayLang=en&SrcCategoryId=&SrcFamilyId=0856eacb-4362-4b0d-8edd-aab15c5e04f5&u=http%3a%2f%2fdownload.microsoft.com%2fdownload%2f5%2f6%2f7%2f567758a3-759e-473e-bf8f-52154438565a%2fdotnetfx.exe"

Function InstallDotNET
  SetOutPath "$TEMP"
  SetOverwrite on

  DetailPrint "Beginning download of latest .NET Framework version."
  NSISDL::download ${DOTNET_URL} "$TEMP\dotnetfx.exe"
  DetailPrint "Completed download."
  Pop $0
  ${If} $0 == "cancel"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download cancelled.  Continue Installation?" \
    IDYES NewDotNET IDNO GiveUpDotNET
  ${ElseIf} $0 != "success"
    MessageBox MB_YESNO|MB_ICONEXCLAMATION \
    "Download failed:$\n$0$\n$\nContinue Installation?" \
    IDYES NewDotNET IDNO GiveUpDotNET
  ${EndIf}
  DetailPrint "Pausing installation while downloaded .NET Framework installer runs."
  ExecWait '$TEMP\dotnetfx.exe /q /c:"install /q"'
  DetailPrint "Completed .NET Framework install/update. Removing .NET Framework installer."
  Delete "$TEMP\dotnetfx.exe"
  DetailPrint ".NET Framework installer removed."
  goto NewDotNet
 
GiveUpDotNET:
  Abort "Installation cancelled by user."
 
NewDotNET:
  DetailPrint "Proceeding with remainder of installation."
  Pop $0
  Pop $1
  Pop $2
  Pop $3
  Pop $4
  Pop $5
  Pop $6 ;backup of intsalled ver
  Pop $7 ;backup of DoNetReqVer

FunctionEnd


; Check to see if a minimum version of .Net is installed on the user's machine.
Function CheckDotNetInstalled
 
  StrCpy $0 "0"
  StrCpy $1 "SOFTWARE\Microsoft\.NETFramework" ;registry entry to look in.
  StrCpy $2 0
 
  StartEnum:
    ;Enumerate the versions installed.
    EnumRegKey $3 HKLM "$1\policy" $2
    
    ;If we don't find any versions installed, it's not here.
    StrCmp $3 "" noDotNetInstalled notEmpty
    
    ;We found something.
    notEmpty:
      ;Find out if the RegKey starts with 'v'.  
      ;If it doesn't, goto the next key.
      StrCpy $4 $3 1 0
      StrCmp $4 "v" +1 goNext
      StrCpy $4 $3 1 1
      
      ;It starts with 'v'. Now check to see how the installed major version
      ;relates to our required major version.
      ;If it's equal check the minor version, if it's greater, 
      ;we found a good RegKey.
      IntCmp $4 ${DOT_MAJOR} +1 goNext yesDotNetInstalled
      ;Check the minor version.  If it's equal or greater to our requested 
      ;version then we're good.
      StrCpy $4 $3 1 3
      IntCmp $4 ${DOT_MINOR} yesDotNetInstalled goNext yesDotNetInstalled
 
    goNext:
      ;Go to the next RegKey.
      IntOp $2 $2 + 1
      goto StartEnum
 
  yesDotNetInstalled:
    ;Now that we've found a good RegKey, let's make sure it's actually
    ;installed by getting the install path and checking to see if the 
    ;mscorlib.dll exists.
    EnumRegValue $2 HKLM "$1\policy\$3" 0
    ;$2 should equal whatever comes after the major and minor versions 
    ;(ie, v1.1.4322)
    StrCmp $2 "" noDotNetInstalled
    ReadRegStr $4 HKLM $1 "InstallRoot"
    ;Hopefully the install root isn't empty.
    StrCmp $4 "" noDotNetInstalled
    ;build the actuall directory path to mscorlib.dll.
    StrCpy $4 "$4$3.$2\mscorlib.dll"
    IfFileExists $4 yesDotNet noDotNetInstalled
 
  noDotNetInstalled:
    ;Looks like the proper .NETFramework isn't installed.  
    MessageBox MB_ICONQUESTION|MB_YESNO "You must have v${DOT_MAJOR}.${DOT_MINOR} or greater of the .NETFramework installed. Do you wish to install the latest .NET framework?" IDYES Install IDNO AbortInstall
    AbortInstall:
    Abort
    Install:
    call InstallDotNET
 
  yesDotNet:
    ;Everything checks out.  Go on with the rest of the installation.
    
FunctionEnd

Section "MainSection" SEC01
 
  call CheckAlreadyInstalled
  call CheckDotNetInstalled

  SetOutPath "$INSTDIR"
  SetOverwrite ifnewer
  File Redist\msvcr71.dll
!ifdef DEBUG
  File "bin.2003\Debug\${locale}\GoogleEmailUploader.exe"
  File "bin.2003\Debug\${locale}\GoogleEmailUploader.pdb"
  File "bin.2003\Debug\${locale}\ThunderbirdClient.dll"
  File "bin.2003\Debug\${locale}\ThunderbirdClient.pdb"
  File "bin.2003\Debug\${locale}\OutlookClient.dll"
  File "bin.2003\Debug\${locale}\OutlookClient.pdb"
  File "bin.2003\Debug\${locale}\OutlookExpressClient.dll"
  File "bin.2003\Debug\${locale}\OutlookExpressClient.pdb"
!else
  File "COPYING"
  File "EmailUploaderUserGuide.html"
  File "bin.2003\Release\${locale}\GoogleEmailUploader.exe"
  File "bin.2003\Release\${locale}\ThunderbirdClient.dll"
  File "bin.2003\Release\${locale}\OutlookClient.dll"
  File "bin.2003\Release\${locale}\OutlookExpressClient.dll"
!endif
  
; Shortcuts
  CreateDirectory "$SMPROGRAMS\${ICONS_GROUP}"
  CreateShortCut "$SMPROGRAMS\${ICONS_GROUP}\GoogleEmailUploader.lnk" "$INSTDIR\GoogleEmailUploader.exe"
  CreateShortCut "$DESKTOP\GoogleEmailUploader.lnk" "$INSTDIR\GoogleEmailUploader.exe"

  ;start menu
  WriteIniStr "$INSTDIR\${PRODUCT_NAME}.url" "InternetShortcut" "URL" "${PRODUCT_WEB_SITE}"
  CreateShortCut "$SMPROGRAMS\${ICONS_GROUP}\Website.lnk" "$INSTDIR\${PRODUCT_NAME}.url"
  CreateShortCut "$SMPROGRAMS\${ICONS_GROUP}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"

; Execute the App.
  Exec "$INSTDIR\GoogleEmailUploader.exe"
SectionEnd

Section -Post
  ; Deleting registry keys for the original 1.0 Setup file which had product name "GoogleEmailUploader"
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY}  "Software\Microsoft\Windows\CurrentVersion\Uninstall\GoogleEmailUploader"
  DeleteIniStr "$INSTDIR\GoogleEmailUploader.url" "InternetShortcut" "URL"
  Delete "$SMPROGRAMS\${OLD_ICONS_GROUP}\*.*" 
  RMDir "$SMPROGRAMS\${OLD_ICONS_GROUP}"

   
  WriteUninstaller "$INSTDIR\Uninstall.exe"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$INSTDIR\GoogleEmailUploader.exe" 
  WriteRegStr HKLM "${PRODUCT_VERSION_REGKEY}" "" ${PRODUCT_VERSION}
  
  ; Updater key
  WriteRegStr HKLM "${PRODUCT_UPDATER_REGKEY}" "pv" "${PRODUCT_VERSION}"
  WriteRegStr HKLM "${PRODUCT_UPDATER_REGKEY}" "name" "${PRODUCT_NAME}"
  WriteRegStr HKLM "${PRODUCT_UPDATER_REGKEY}" "lang" "${locale}"

  ; Uninstall keys
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\Uninstall.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$INSTDIR\GoogleEmailUploader.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"

SectionEnd

; Section descriptions
!insertmacro MUI_FUNCTION_DESCRIPTION_BEGIN
  !insertmacro MUI_DESCRIPTION_TEXT ${SEC01} ""
!insertmacro MUI_FUNCTION_DESCRIPTION_END

Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) was successfully removed from your computer."
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove $(^Name) and all of its components?" IDYES +2
  Abort
FunctionEnd

Section Uninstall
  StrCpy $1 "GoogleEmailUploader.exe" 
  FindProcDLL::FindProc "$1" 
  ;0   = Process was not found 
  ;1   = Process was found 
  ;607 = Unsupported OS 
  StrCmp $R0 0  end error 
    error: 
      MessageBox MB_OK|MB_ICONSTOP "The application $1 is currently running, cannot be removed." 
      Quit
   end:
  
  Delete "$INSTDIR\${PRODUCT_NAME}.url"
  Delete "$INSTDIR\Uninstall.exe"
  Delete "$INSTDIR\GoogleEmailUploader.exe"
  Delete "$INSTDIR\*"

  Delete "$SMPROGRAMS\${ICONS_GROUP}\Uninstall.lnk"
  Delete "$SMPROGRAMS\${ICONS_GROUP}\Website.lnk"
  Delete "$DESKTOP\GoogleEmailUploader.lnk"
  Delete "$SMPROGRAMS\${ICONS_GROUP}\GoogleEmailUploader.lnk"

  RMDir "$SMPROGRAMS\${ICONS_GROUP}"
  RMDir "$INSTDIR"

  DeleteRegKey HKLM "Software\Google\GoogleEmailUploader"
  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"

  ; Delete the main key so that keys for all previous versions also get removed at the time of uninstall.
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"

  ; Delete the Updater key
  DeleteRegKey HKLM "${PRODUCT_UPDATER_REGKEY}"

  SetAutoClose true
SectionEnd
