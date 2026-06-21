# VMS MediPro DICOM Solution (.NET Architecture)
## Complete Feature Specification & .NET Engineering Guide

This document outlines the complete feature set of the VMS MediPro DICOM software and provides a detailed, step-by-step engineering guide on how to build these capabilities using the Microsoft .NET ecosystem (C#), which is the industry standard for this type of Windows-native application.

---

## PART 1: Complete Feature Specifications

### 1. Connectivity & DICOM Reception Server
* **Multi-Modality Integration:** Natively receives DICOM data from CR, DR (Digital X-Ray), CT, MRI, Mammography, Dental (OPG), and Sonography machines.
* **Concurrent Multi-Port Listening:** Capable of listening on multiple IP addresses and ports simultaneously (e.g., Port 104 for CT, Port 105 for Ultrasound) using highly parallel, non-blocking TCP listeners.
* **DICOM C-STORE SCP:** Acts as a robust Service Class Provider (SCP) to handle heavy, continuous incoming medical image transmissions without dropping packets.

### 2. Image Processing & Quality Engine
* **Linear DICOM Look-Up Tables (LUTs):** Mathematically maps raw 12-bit/16-bit DICOM pixel data into standard 8-bit RGB/Grayscale images. Applies Window Center/Window Width calculations to ensure printers output clinical-grade contrast.
* **Rule-Based LUT Assignment:** Automatically routes images through different contrast algorithms based on DICOM header tags (e.g., Modality type, Body Part Examined).
* **Simultaneous Color & Grayscale:** Processes both monochrome X-Rays and full-color 3D Doppler Ultrasounds concurrently via heavy multi-threading.

### 3. Native Print Routing & Load Balancing
* **Native Windows Spooler Integration:** Directly interfaces with the Windows Print Spooler API to bypass third-party driver bottlenecks.
* **Simultaneous 4-Printer Support:** Connects to and drives up to four standard Windows-based inkjet/laser printers simultaneously.
* **Intelligent Load Balancing (Rotation):** Monitors hardware printer states (Ready, Busy, Offline). If Printer A is actively printing, the engine automatically routes the next incoming scan to Printer B.
* **Dry Film Capability:** Optimizes print jobs specifically for PET medical dry film, replacing expensive chemical-based thermal films.
* **PNDT Compliance Logging:** Strictly tracks and formats obstetric ultrasound prints to comply with Pre-Natal Diagnostic Technique (PNDT) laws.

### 4. Digital Archiving & Licensing Management
* **Automated PDF Archiving:** Converts processed scans and patient reports into high-resolution PDFs, saving them directly to secured local/network directories.
* **Secure Digital Sharing:** Dispatches generated diagnostic PDFs to referring physicians or patients via secure Email or automated WhatsApp API integration.
* **Cloud-Synced Print Quotas (OTP Integration):** Integrates with a cloud database to track physical print counts. Connects to a mobile app used by clinic owners to remotely "recharge" their server's print license (pay-per-print model).

---

## PART 2: How to Build It in .NET (Technical Architecture)

Because this software must act as a bridge between heavy medical imaging data and local Windows hardware (printers), **C# and .NET 8** are the optimal choices. 

### 1. The Core DICOM Server (C# Worker Service)
You will build this as a **.NET Background Worker Service** so it runs silently on the Windows machine as a daemon.
* **The Library:** Install **`fo-dicom`** (Fellow Oak DICOM) via NuGet. It is the gold standard for DICOM in .NET.
* **The Logic:** Implement `fo-dicom`'s `IDicomCStoreProvider`. When the X-Ray machine sends a file, your server accepts the `DicomCStoreRequest`. 
* **Concurrency:** Use `Task.Run` and C#'s asynchronous features (`async/await`) to ensure that receiving a massive 500MB MRI dataset doesn't block the server from receiving a small X-ray on another port at the same time.

### 2. Image Processing Engine (Magick.NET)
Extracting the image from the `.dcm` file and making it printable requires pixel-level manipulation.
* **Extraction:** Use `fo-dicom`'s `DicomImage` class to read the raw pixel data and apply the Window/Level (LUT) mathematically based on the DICOM tags (`0028,1050` and `0028,1051`).
* **Conversion & Enhancing:** Pass the raw bitmap array to **`Magick.NET`** (the .NET wrapper for ImageMagick). Use Magick.NET to scale the image to the correct DPI (e.g., 300 DPI for high-quality printing), adjust the gamma, and render it as an 8-bit format (JPEG or BMP) that the Windows printer understands.

### 3. Print Routing & Spooler Logic (System.Drawing)
This is where .NET completely outperforms Python. 
* **The Spooler Interface:** Use the native **`System.Drawing.Printing.PrintDocument`** class. You do not need hacky wrappers. 
* **Load Balancing:** Use **WMI (Windows Management Instrumentation)** via `System.Management.ManagementObjectSearcher` to query `Win32_Printer`. You can check `PrinterStatus` in real-time. 
* **The Queue:** Implement a `System.Threading.Channels.Channel` (or RabbitMQ if scaling up) as an in-memory queue. As DICOM files are processed by Magick.NET, drop them into the channel. A dedicated consumer thread reads the channel, finds the first printer whose WMI status is "Idle", and assigns the `PrintDocument` to that specific printer queue.

### 4. Local Web Dashboard & Archiving (ASP.NET Core)
Hospital staff need a UI to view logs, configure ports, and set up PNDT compliance rules.
* **The Backend:** Build an **ASP.NET Core Web API** that runs alongside the Worker Service.
* **The Database:** Use **Entity Framework Core (EF Core)** paired with **SQLite** or **SQL Server Express** (since this runs locally on a hospital PC, SQLite is incredibly fast and requires no setup). This will store patient logs, printer configurations, and port rules.
* **PDF Generation:** Use **QuestPDF** (a modern, fast .NET PDF library) to take the processed Magick.NET images, lay them out on a virtual page (like a 2x2 grid), add the clinic's logo/header, and save it as a `.pdf` to the local drive.
* **The Frontend:** Serve a simple **Blazor WebAssembly** or React frontend from the ASP.NET Core app so staff can access the dashboard via `localhost:5000` in their browser.

### 5. Quota Sync & Mobile App Integration
To implement the "pay-per-print" licensing model:
* **Cloud Sync:** Integrate the **Firebase Admin .NET SDK** into your C# Worker Service. 
* **The Workflow:** Every time `PrintDocument` successfully fires, the Worker Service makes an async call to Firebase Firestore to decrement the `RemainingPrints` integer for that specific clinic's license key. 
* **The Mobile App:** Build the clinic owner's app in **Flutter** or **FlutterFlow**, connected to the same Firebase project. When `RemainingPrints` hits zero, the C# server rejects new print jobs. The clinic owner buys a new pack in the Flutter app, Firebase updates to 1000 prints, and the C# server instantly resumes printing via a Firebase real-time listener.
