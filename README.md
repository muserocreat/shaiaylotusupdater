# 🌸 Shaiya Lotus Updater - Ultimate Edition

![Shaiya Lotus Banner](https://shaiyalotus.com/resources/images/logo.png)

A modern, high-performance, and visually stunning game launcher for **Shaiya Lotus**. Built with .NET 8 and WPF, this updater focuses on providing users with a premium experience from the moment they launch the game.

## ✨ Key Features

- **🌀 Dynamic Backgrounds**: High-definition rotating artwork featuring the Goddesses of Light and Darkness, with smooth cross-fade transitions every 10 seconds.
- **💓 Breathing Play Button**: An interactive "JUGAR" button that pulses with life once the game is fully updated, providing clear visual feedback.
- **🟢 Real-time Server Status**: An integrated live connectivity indicator that pings the login server every 20 seconds to show if the server is Online or Offline.
- **🚀 Advanced Auto-Update**: Seamlessly self-updates to the latest launcher version (currently v1000) using an automated replacement engine.
- **📱 External Navigation**: Discord, News, and Download buttons launch directly in the user's default browser for a better reading experience.
- **⚡ Performance Optimized**: Compiled as an x86 standalone executable with native library compression for fast startup and low resource usage.

## 🛠️ Build & Development

This project is optimized for **Visual Studio 2022** and **.NET 8.0 Windows**.

### Automatic Compilation
Use the included `compilar.bat` in the root directory to generate a production-ready build:
- **Build Target**: Release / x86
- **Output Directory**: `C:\Users\Maxi\Desktop\Updater\Compilacion\`
- **Packaging**: Single-file, Self-contained.

### Core Technologies
- **WPF (Windows Presentation Foundation)**: For the cinematic UI.
- **Microsoft WebBrowser WebView2**: For future web integration.
- **Parsec.Shaiya.Data**: Professional game data extraction and patching.

## 📁 Project Structure

- `/Updater`: Main source code.
- `/Resources`: UI assets (Loto Goddesses, Icons, Strings).
- `/Compilacion`: Final distribution builds.

---
**Shaiya Lotus** - *The clash of Goddesses has never looked so beautiful.* ⚔️💮
