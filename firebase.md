# Firebase Setup Guide for VMS MediPro

This document explains exactly how to set up your Firebase project so that the MediPro .NET server can successfully synchronize and enforce print quotas.

## 1. Create the Firebase Project
1. Go to the [Firebase Console](https://console.firebase.google.com/).
2. Click **Add project** and name it `vms-medipro` (or anything you prefer).
3. Disable Google Analytics (you don't need it for this backend).
4. Click **Create project**.

## 2. Set Up Firestore Database
1. In the left sidebar, click on **Firestore Database** under the "Build" menu.
2. Click **Create database**.
3. Select **Start in production mode** (or test mode if you want open access for 30 days) and click **Next**.
4. Choose a database location closest to your hospital (e.g., `asia-south1` for India) and click **Enable**.

## 3. Create the Quota Document
The .NET server listens to a specific document to check if the clinic has remaining prints.
1. In Firestore, click **Start collection**.
2. Name the Collection ID: `clinics`. Click **Next**.
3. Set the Document ID to: `CLINIC-DEMO-001`. (This must match `ClinicLicenseKey` in your `appsettings.json`).
4. Add the following three fields to this document:
   - Field: `RemainingPrints` | Type: `number` | Value: `500`
   - Field: `TotalPrints` | Type: `number` | Value: `500`
   - Field: `ClinicName` | Type: `string` | Value: `VMS Demo Clinic`
5. Click **Save**.

## 4. Get the Service Account JSON File
The .NET backend needs permission to read and write to this database securely.
1. Click the **Gear icon** (Project settings) next to "Project Overview" in the top-left corner.
2. Go to the **Service accounts** tab.
3. Make sure **Node.js** or **.NET** is selected, and click the **Generate new private key** button.
4. Click **Generate key**. A JSON file will download to your computer.
5. Rename the downloaded file to `medipro-firebase-credentials.json` (or use the original name).
6. Ensure the path in your `appsettings.json` under `Firebase -> ServiceAccountPath` exactly matches where this JSON file is saved on your computer (e.g., `d:\\Projects\\medipro\\medipro-a4055...json`).

## 5. Verify Configuration
Ensure your `appsettings.json` (in both `MediPro.Worker` and `MediPro.Dashboard`) looks like this:

```json
"Firebase": {
  "ServiceAccountPath": "d:\\\\Projects\\\\medipro\\\\medipro-a4055-firebase-adminsdk-fbsvc-f34a68c15c.json",
  "ProjectId": "your-firebase-project-id",
  "ClinicLicenseKey": "CLINIC-DEMO-001",
  "Enabled": true
}
```

Once this is set up, when the `MediPro.Worker` starts, it will instantly fetch the `500` remaining prints from Firestore. Every time a page prints, it will deduct `1`. If it reaches `0`, printing stops until you manually change it back to a positive number in the Firebase Console (or via your FlutterFlow app).
