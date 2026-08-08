@echo off
rem Builds the portable, zero-install LOS-LMS demo the client can double-click to run.
rem Output: publish\LOS-LMS-Demo.zip  (this is the file you share)
setlocal
cd /d "%~dp0"

set OUT=publish\LOS-LMS-Demo

echo === Cleaning previous build ===
if exist "%OUT%" rmdir /s /q "%OUT%"
if exist "publish\LOS-LMS-Demo.zip" del /q "publish\LOS-LMS-Demo.zip"

echo === Publishing self-contained win-x64 build (bundles .NET runtime) ===
dotnet publish LosLms\LosLms.csproj -c Release -r win-x64 --self-contained true -o "%OUT%"
if errorlevel 1 (
  echo.
  echo PUBLISH FAILED. Fix the errors above and re-run.
  exit /b 1
)

echo === Applying portable configuration (SQLite, http://localhost:5050) ===
copy /y deploy\appsettings.portable.json "%OUT%\appsettings.json" >nul
copy /y deploy\Start-Demo.bat "%OUT%\Start-Demo.bat" >nul

echo === Ensuring uploads folder exists ===
if not exist "%OUT%\App_Data\uploads" mkdir "%OUT%\App_Data\uploads"

echo === Zipping ===
powershell -NoProfile -Command "Compress-Archive -Path 'publish\LOS-LMS-Demo\*' -DestinationPath 'publish\LOS-LMS-Demo.zip' -Force"
if errorlevel 1 (
  echo ZIP FAILED.
  exit /b 1
)

echo.
echo ================================================
echo  DONE.
echo  Share this file with the client:
echo      publish\LOS-LMS-Demo.zip
echo.
echo  Client steps: unzip, then double-click Start-Demo.bat
echo ================================================
endlocal
