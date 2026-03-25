# 🧪 Updater Windsurf - Entorno de Pruebas

Este es el entorno de pruebas dedicado para el **Shaiya Lotus Updater**. Aquí puedes probar actualizaciones locales del `new_updater` de forma segura antes de desplegar al servidor principal.

## 🚀 Configuración Rápida

### 1. Compilar en Modo DEBUG
```batch
compilar.bat
```
- Genera: `Compilacion/Updater.exe` (70MB con símbolos de depuración)
- Servidor: `http://25.45.91.85/Shaiya` (servidor de pruebas)

### 2. Probar el Updater
```batch
test_debug.bat
```
- Inicia el Updater compilado
- Muestra información de configuración

### 3. Para Probar new_updater Local
1. Copia tu archivo de prueba como: `Compilacion/new_updater_local.exe`
2. El sistema lo detectará automáticamente y lo usará en lugar del del servidor

## 📋 Flujo de Trabajo

```
Desarrollo → Pruebas Locales → Validación → Producción
    ↓              ↓              ↓           ↓
Updater Windsurf → new_updater_local → Servidor 25.45.91.85 → Servidor 158.69.213.250
```

## 🔧 Características del Modo DEBUG

- ✅ **Servidor de pruebas aislado**: No afecta el servidor principal
- ✅ **Override local**: Puedes probar new_updater sin subirlo al servidor
- ✅ **Símbolos de depuración**: Facilita la identificación de problemas
- ✅ **Compilación rápida**: Optimizada para desarrollo

## 📁 Estructura Importante

```
Updater Windsurf/
├── compilar.bat           # Compila en modo DEBUG
├── test_debug.bat         # Script de prueba rápido
├── DEBUG_CONFIG.md        # Configuración detallada
├── Compilacion/           # Salida de compilación
│   ├── Updater.exe        # Aplicación principal (DEBUG)
│   ├── Updater.pdb        # Símbolos de depuración
│   └── new_updater_local.exe # Tu versión de prueba (opcional)
└── Updater/
    ├── Core/NewUpdater.cs # Soporta override local
    └── Common/Constants.cs # Configuración DEBUG/RELEASE
```

## ⚠️ Importante

- **Este entorno NO debe usarse para producción**
- **Siempre prueba aquí antes de desplegar**
- **El servidor de pruebas (25.45.91.85) está aislado del principal**

## 🐛 Solución de Problemas

### Si no compila:
- Verifica que tienes .NET 8.0 Windows instalado
- Asegúrate de estar en el directorio `Updater Windsurf`

### Si no detecta new_updater_local:
- Verifica que el archivo esté en `Compilacion/new_updater_local.exe`
- Asegúrate de estar compilando en modo DEBUG

### Si hay errores de conexión:
- Verifica que el servidor 25.45.91.85 esté accesible
- Revisa la configuración en `Common/Constants.cs`

---

**Listo para probar!** 🎮
