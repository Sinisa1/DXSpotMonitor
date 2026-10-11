

# Fix: ADB Not Found - Navigate to Correct Directory

## **Problem: You're in the wrong directory**

The `.\adb.exe` command only works when you're **in the same folder** as adb.exe.

---

## **Solution: Navigate First, Then Run**

```powershell
# Change to the directory where adb.exe is located
cd "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools"

# Now run adb
.\adb.exe devices
```

---

## **Alternative: Run with Full Path (No CD needed)**

From anywhere in PowerShell:

```powershell
& "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools\adb.exe" devices
```

The `&` tells PowerShell to execute the command at that path.

---

## **Quick Test - Run This Now:**

```powershell
# Check if ADB path exists
Test-Path "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools\adb.exe"

# If TRUE, run this:
& "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools\adb.exe" devices
```

---

## **Expected Outputs:**

### **✅ Phone Connected & Authorized:**
```
List of devices attached
R58N123ABC      device
```

### **⚠️ Phone Connected But Not Authorized:**
```
List of devices attached
R58N123ABC      unauthorized
```
**Fix:** Look at phone, tap "Allow USB debugging"

### **❌ No Phone Detected:**
```
List of devices attached
[empty]
```
**Fix:** 
1. Check USB cable is connected
2. Enable Developer Options → USB Debugging on phone
3. Try different USB port/cable

---

## **If ADB Path Doesn't Exist**

The path might be different. Find it:

```powershell
# Search for adb.exe
Get-ChildItem "C:\Program Files\dotnet\packs" -Recurse -Filter "adb.exe" -ErrorAction SilentlyContinue | Select-Object FullName
```

This will show the actual path to adb.exe on your system.

---

## **Add ADB to PATH (Optional - Makes Life Easier)**

This lets you run `adb` from anywhere without the full path:

```powershell
# Run PowerShell as Administrator
# Add to PATH permanently
$adbPath = "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools"
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";$adbPath", [EnvironmentVariableTarget]::Machine)

# Restart PowerShell, then you can use:
adb devices
```

---

## **Fastest Way - Deploy Without Finding ADB**

Just deploy directly using .NET CLI:

```powershell
# From your workspace root
cd C:\_REPOSITORIES\VA3TNM_LOG\DXSpotViewer

# This automatically finds and uses ADB
dotnet build -f net9.0-android -t:Run
```

This command:
- ✅ Finds phone automatically
- ✅ Installs the app
- ✅ Launches it
- ✅ No need to manually run adb commands

---

## **Run This Now (Copy/Paste):**

```powershell
# Option 1: Try with full path
& "C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\35.0.105\tools\adb.exe" devices

# If that fails, just deploy directly:
cd C:\_REPOSITORIES\VA3TNM_LOG\DXSpotViewer
dotnet build -f net9.0-android -t:Run
```

**Tell me:**
1. What does the first command output? (device list)
2. Or does the second command (dotnet build) work and install the app?

The dotnet build command is actually the easiest way - it handles all the ADB stuff automatically! 📱


# From your workspace root
cd C:\_REPOSITORIES\VA3TNM_LOG\DXSpotViewer

# This automatically finds and uses ADB
dotnet build -f net9.0-android -t:Run




# DX Spot Viewer - Complete Android App Documentation

**Project:** VA3TNM Ham Radio Logger - Android Companion App  
**Version:** 1.0 (Prototype)  
**Technology:** .NET 9 MAUI for Android  
**Communication:** MQTT (broker.hivemq.com)  
**Last Updated:** February 2026

---

## Table of Contents

1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Prerequisites & Setup](#prerequisites--setup)
4. [Complete Source Code](#complete-source-code)
5. [Phone Preparation](#phone-preparation)
6. [Building & Deployment](#building--deployment)
7. [Testing Guide](#testing-guide)
8. [Troubleshooting](#troubleshooting)
9. [Integration with WPF Logger](#integration-with-wpf-logger)
10. [Future Enhancements](#future-enhancements)

---

## Project Overview

### Purpose

A companion Android app for the VA3TNM Ham Radio Logger that receives real-time DX spot notifications via MQTT. The app displays incoming spots with visual indicators for needed DXCC entities, bands, and modes.

### Key Features

- ✅ Real-time MQTT connectivity (works on WiFi and cellular data)
- ✅ Visual color coding for needed DX (DXCC/Band/Mode)
- ✅ Vibration alerts for important spots
- ✅ Spot history display (up to 100 spots)
- ✅ Connection status monitoring with auto-reconnect
- ✅ No server setup required (uses public MQTT broker)
- ✅ Works anywhere with internet connection

### Technology Stack

- **.NET 9** (or .NET 8)
- **.NET MAUI** (Multi-platform App UI)
- **MQTTnet 4.3.7** (MQTT client library)
- **CommunityToolkit.Maui** (UI helpers)
- **CommunityToolkit.Mvvm** (MVVM framework)

---

## Architecture

### System Overview



---

## Prerequisites & Setup

### Software Requirements

1. **Visual Studio 2026** (18.4.0-insiders) or **Visual Studio 2022**
2. **.NET 9 SDK** (or .NET 8)
3. **.NET MAUI Workload**
4. **Android SDK** (installed with MAUI)

### Verify Installation

---

## Phone Preparation

### Step 1: Enable Developer Mode

1. **Settings** → **About Phone** → **Software Information**
2. Tap **Build Number** 7 times
3. See: "Developer mode enabled"

### Step 2: Enable USB Debugging

1. **Settings** → **Developer Options**
2. Turn ON: **USB Debugging**
3. Turn ON: **Install via USB**

### Step 3: Connect & Authorize

1. Connect phone via USB to PC
2. Phone shows: "Allow USB debugging?"
3. Check "Always allow"
4. Tap **Allow**

### Step 4: Verify Connection
