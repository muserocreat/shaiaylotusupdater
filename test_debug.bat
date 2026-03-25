@echo off
color 0B
title Probando Updater Windsurf (DEBUG)
echo ========================================================
echo INICIANDO PRUEBA DEL UPDATER WINDSURF (MODO DEBUG)
echo ========================================================
echo.
echo Servidor de pruebas: http://25.45.91.85/Shaiya
echo Modo: DEBUG
echo.

if not exist "Compilacion\Updater.exe" (
    echo [ERROR] No se encuentra Updater.exe en la carpeta Compilacion/
    echo Ejecuta compilar.bat primero
    pause
    exit /b 1
)

echo [OK] Updater.exe encontrado
echo.

if exist "Compilacion\new_updater_local.exe" (
    echo [INFO] Se detecto new_updater_local.exe - se usara version local
) else (
    echo [INFO] Se usara new_updater.exe desde el servidor de pruebas
)

echo.
echo Iniciando Updater en modo DEBUG...
echo ========================================================
cd Compilacion
start Updater.exe

echo.
echo Updater iniciado. Verifica la consola para ver el comportamiento.
echo Presiona cualquier tecla para continuar...
pause >nul
