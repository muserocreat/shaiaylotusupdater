# Configuración de Entorno de Pruebas - Updater Windsurf

## Uso del Modo DEBUG

Este entorno está configurado para trabajar en modo DEBUG con las siguientes características:

### Servidor de Pruebas
- **URL Base**: `http://25.45.91.85/Shaiya`
- **Modo**: DEBUG (servidor de pruebas aislado)

### Flujo de Trabajo para Pruebas Locales

1. **Compilar en modo Debug:**
   ```batch
   compilar.bat
   ```
   Esto generará `Updater.exe` en la carpeta `Compilacion/`

2. **Para probar new_updater localmente:**
   - Coloca tu versión de prueba como `new_updater_local.exe` en la misma carpeta que `Updater.exe`
   - El sistema detectará automáticamente el archivo local y lo usará en lugar de descargar del servidor

3. **Archivos de Configuración:**
   - `Version.ini`: Configuración local del cliente
   - `UpdateVersion.ini`: Se descarga automáticamente del servidor de pruebas

### Estructura de Archivos para Pruebas

```
Compilacion/
├── Updater.exe              (Aplicación principal)
├── new_updater.exe          (Descargado del servidor de pruebas)
├── new_updater_local.exe    (Tu versión local - opcional)
├── Version.ini              (Configuración local)
└── UpdateVersion.ini        (Configuración del servidor)
```

### Diferencias DEBUG vs RELEASE

| Característica | DEBUG (Updater Windsurf) | RELEASE (Updater Principal) |
|---------------|-------------------------|----------------------------|
| Servidor      | 25.45.91.85 (pruebas)   | 158.69.213.250 (producción) |
| new_updater   | Soporta local override   | Solo desde servidor        |
| Compilación   | Debug con símbolos      | Release optimizada         |

### Validación de Pruebas

Antes de pasar a producción:
1. ✅ Verificar que funciona con servidor de pruebas
2. ✅ Probar new_updater local si aplica
3. ✅ Validar actualización de Version.ini
4. ✅ Comprobar integración completa

### Pasaje a Producción

Cuando las pruebas sean exitosas:
1. Cambiar a modo Release en el proyecto principal
2. Compilar con el script del updater principal
3. Desplegar al servidor de producción
