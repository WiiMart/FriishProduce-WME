@echo off
cls
echo ===============================
echo Running FriishProduce...
echo ===============================

REM Path to the executable in the same folder as this batch file
set EXE_PATH=%~dp0FriishProduce\bin\Debug\FriishProduce.exe

if not exist "%EXE_PATH%" (
    echo ERROR: Could not find "%EXE_PATH%"
    echo Make sure the executable is in the correct path.
    pause
    exit /b 1
)

REM Launch the program
start "" "%EXE_PATH%"

echo.
echo ===============================
echo Program launched.
echo ===============================
echo.
exit