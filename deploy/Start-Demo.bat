@echo off
title LOS-LMS Demo
cd /d "%~dp0"
echo ================================================
echo    LOS-LMS Demo is starting...
echo.
echo    A browser window will open automatically.
echo    If it does not, open your browser and go to:
echo        http://localhost:5050/applications
echo.
echo    Keep THIS window open while using the app.
echo    Close this window to stop the app.
echo ================================================
echo.
rem Open the browser a few seconds after the server has had time to start,
rem without blocking the server (which runs in this window).
rem The dashboard is at /applications; the site root is only a placeholder shell.
start "" cmd /c "timeout /t 4 /nobreak >nul & start "" http://localhost:5050/applications"
LosLms.exe --urls http://localhost:5050
