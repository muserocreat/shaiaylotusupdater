@echo off
color 0A
title Compilando Shaiya Lotus Updater...
echo ========================================================
echo INICIANDO COMPILACION DEL UPDATER (x86 - Single File)
echo ========================================================
echo.

dotnet publish "c:\Users\Maxi\Desktop\Updater\Updater\Updater.csproj" -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -o "C:\Users\Maxi\Desktop\Updater\Compilacion"

echo.
echo ========================================================
echo COMPILACION TERMINADA.
echo Tu archivo esta en: C:\Users\Maxi\Desktop\Updater\Compilacion\Updater.exe
echo ========================================================
pause
