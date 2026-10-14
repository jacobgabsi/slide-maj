@echo off
rem ===========================================================================
rem  Lance Slide, en installant .NET 10 si le PC ne l'a pas.
rem
rem  Sur un PC neuf : double-clic sur ce fichier. L'installation fonctionne
rem  SANS INTERNET grace a l'installateur range dans le sous-dossier Runtime.
rem  Internet n'est utilise qu'en secours, si ce fichier a ete supprime.
rem ===========================================================================
chcp 65001 >nul
setlocal enabledelayedexpansion
title Slide

set "DOSSIER=%~dp0"
set "EXE=%DOSSIER%bin\Debug\net10.0-windows\slide.exe"
if not exist "%EXE%" set "EXE=%DOSSIER%bin\Release\net10.0-windows\slide.exe"
if not exist "%EXE%" set "EXE=%DOSSIER%slide.exe"
if not exist "%EXE%" (
    echo Programme introuvable : slide.exe n'est ni dans ce dossier, ni dans bin\Debug\net10.0-windows.
    pause
    exit /b 1
)

call :RUNTIME_PRESENT
if defined PRESENT goto lancer

echo.
echo   .NET 10 n'est pas installe sur ce PC : installation en cours.
echo   Windows va demander l'autorisation (environ 1 minute).
echo.

rem --- 1) l'installateur fourni a cote : fonctionne sans Internet ------------
set "LOCAL=%DOSSIER%Runtime\windowsdesktop-runtime-10-win-x64.exe"
if exist "%LOCAL%" (
    echo   Installation depuis le dossier Runtime...
    powershell -NoProfile -Command "Start-Process -FilePath '%LOCAL%' -ArgumentList '/install','/quiet','/norestart' -Verb RunAs -Wait"
    call :RUNTIME_PRESENT
    if defined PRESENT goto lancer
)

rem --- 2) winget (Windows 10 recent et Windows 11), si Internet -------------
where winget >nul 2>&1
if %errorlevel%==0 (
    echo   Installation par winget...
    winget install --id Microsoft.DotNet.DesktopRuntime.10 -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity
    call :RUNTIME_PRESENT
    if defined PRESENT goto lancer
)

rem --- 3) telechargement direct chez Microsoft ------------------------------
set "LIEN=https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64.exe"
if /i "%PROCESSOR_ARCHITECTURE%"=="ARM64" set "LIEN=https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-arm64.exe"
if /i "%PROCESSOR_ARCHITECTURE%"=="x86" set "LIEN=https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x86.exe"
set "INSTALLEUR=%TEMP%\windowsdesktop-runtime-10.exe"

echo   Telechargement depuis Microsoft...
powershell -NoProfile -Command "$ProgressPreference='SilentlyContinue'; try { Invoke-WebRequest -Uri '%LIEN%' -OutFile '%INSTALLEUR%' -UseBasicParsing -TimeoutSec 900 } catch { exit 1 }"
if exist "%INSTALLEUR%" (
    echo   Installation...
    powershell -NoProfile -Command "Start-Process -FilePath '%INSTALLEUR%' -ArgumentList '/install','/quiet','/norestart' -Verb RunAs -Wait"
    del "%INSTALLEUR%" >nul 2>&1
    call :RUNTIME_PRESENT
    if defined PRESENT goto lancer
)

echo.
echo   .NET 10 n'a pas pu etre installe.
echo   Verifiez que le fichier Runtime\windowsdesktop-runtime-10-win-x64.exe est bien la,
echo   ou installez .NET 10 Desktop Runtime depuis :
echo   https://dotnet.microsoft.com/download/dotnet/10.0
echo.
pause
exit /b 1

:lancer
call :ACE_PRESENT
if defined ACE goto demarrer

rem ===========================================================================
rem  Acces a la base slide.accdb : il faut le fournisseur Microsoft ACE, livre
rem  avec Office ou par le moteur de base Access. Meme principe que .NET 10 :
rem  l'installateur fourni d'abord (sans Internet), le reseau en secours.
rem
rem  Seul cas ou l'installation silencieuse peut echouer : un Office "clic-et-a-jour"
rem  deja present refuse le redistribuable. Mais alors ACE est en general deja la, donc
rem  on n'arrive meme pas ici ; sinon le message de la fin explique quoi faire.
rem ===========================================================================
echo.
echo   Le fournisseur de base Access (ACE) n'est pas installe : installation en cours.
echo.

set "LOCALACE=%DOSSIER%Runtime\accessdatabaseengine_X64.exe"
if exist "%LOCALACE%" (
    echo   Installation depuis le dossier Runtime...
    powershell -NoProfile -Command "Start-Process -FilePath '%LOCALACE%' -ArgumentList '/quiet','/norestart' -Verb RunAs -Wait"
    call :ACE_PRESENT
    if defined ACE goto demarrer
)

where winget >nul 2>&1
if %errorlevel%==0 (
    echo   Installation par winget...
    winget install --id Microsoft.AccessDatabaseEngine2016 -e --source winget --accept-package-agreements --accept-source-agreements --disable-interactivity
    call :ACE_PRESENT
    if defined ACE goto demarrer
)

set "INSTALLACE=%TEMP%\accessdatabaseengine_X64.exe"
echo   Telechargement depuis Microsoft...
powershell -NoProfile -Command "$ProgressPreference='SilentlyContinue'; try { Invoke-WebRequest -Uri 'https://download.microsoft.com/download/3/5/C/35C84C36-661A-44E6-9324-8786B8DBE231/accessdatabaseengine_X64.exe' -OutFile '%INSTALLACE%' -UseBasicParsing -TimeoutSec 900 } catch { exit 1 }"
if exist "%INSTALLACE%" (
    echo   Installation...
    powershell -NoProfile -Command "Start-Process -FilePath '%INSTALLACE%' -ArgumentList '/quiet','/norestart' -Verb RunAs -Wait"
    del "%INSTALLACE%" >nul 2>&1
    call :ACE_PRESENT
    if defined ACE goto demarrer
)

echo.
echo   Le fournisseur Access n'a pas pu etre installe. Slide va demarrer, mais la
echo   minuterie et la localite des photos ne fonctionneront pas.
echo   Installez Runtime\accessdatabaseengine_X64.exe a la main, ou le
echo   Microsoft 365 Access Runtime (64 bits).
echo.
timeout /t 8 >nul

:demarrer
start "" "%EXE%"
exit /b 0

rem --- Fournisseur Access (ACE) installe ? ----------------------------------
:ACE_PRESENT
set "ACE="
reg query "HKLM\SOFTWARE\Classes\Microsoft.ACE.OLEDB.12.0" >nul 2>&1
if %errorlevel%==0 set "ACE=1"
if not defined ACE (
    reg query "HKLM\SOFTWARE\Classes\Microsoft.ACE.OLEDB.16.0" >nul 2>&1
    if !errorlevel!==0 set "ACE=1"
)
exit /b 0

rem --- .NET 10 Desktop installe ? (la presence du dossier du runtime suffit) -
:RUNTIME_PRESENT
set "PRESENT="
for /d %%D in ("%ProgramFiles%\dotnet\shared\Microsoft.WindowsDesktop.App\10.*") do set "PRESENT=1"
if not defined PRESENT for /d %%D in ("%ProgramFiles(x86)%\dotnet\shared\Microsoft.WindowsDesktop.App\10.*") do set "PRESENT=1"
exit /b 0
