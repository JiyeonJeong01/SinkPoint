@echo off
setlocal

set "BLENDER=C:\Program Files\Blender Foundation\Blender 4.2\blender.exe"
set "SCRIPT=%~dp0BatchDecimate.py"
set "DEFAULT_RATIO=0.20"

echo.
echo ========================================
echo SinkPoint FBX Batch Decimate
echo ========================================
echo This overwrites the original FBX file(s).
echo Default: keeps about 20%%, reduces about 80%%.
echo.

set /p "TARGET=FBX file or folder path: "
if "%TARGET%"=="" (
    echo.
    echo No target entered.
    pause
    exit /b 1
)

set /p "RATIO=Decimate keep ratio [default %DEFAULT_RATIO%]: "
if "%RATIO%"=="" set "RATIO=%DEFAULT_RATIO%"

"%BLENDER%" --background --python "%SCRIPT%" -- --target "%TARGET%" --ratio "%RATIO%"

echo.
echo ========================================
echo Finished
echo ========================================

pause
