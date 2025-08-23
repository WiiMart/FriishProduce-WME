@echo off
:build
cls
echo ===============================
echo Cleaning FriishProduce...
echo ===============================
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" ^
 "FriishProduce.sln" ^
 /t:Clean /p:Configuration=Debug /p:Platform="Any CPU"

echo.
echo ===============================
echo Building FriishProduce...
echo ===============================
"C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" ^
 "FriishProduce.sln" ^
 /t:Build /p:Configuration=Debug /p:Platform="Any CPU"

echo.
echo ===============================
echo Build finished.
echo ===============================

echo.
choice /M "Rebuild?" /C YN
if errorlevel 2 exit /b
goto build