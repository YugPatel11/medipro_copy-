$ErrorActionPreference = 'Continue'
Write-Host "=== VMS MediPro - Scaffold Start ===" -ForegroundColor Cyan

# --- Solution ---
dotnet new sln -n MediPro --force

# --- Projects ---
dotnet new classlib   -n MediPro.Core              -o src/MediPro.Core              --framework net8.0
dotnet new classlib   -n MediPro.Dicom             -o src/MediPro.Dicom             --framework net8.0
dotnet new classlib   -n MediPro.Print             -o src/MediPro.Print             --framework net8.0
dotnet new classlib   -n MediPro.Quota             -o src/MediPro.Quota             --framework net8.0
dotnet new blazorwasm -n MediPro.Dashboard.Client  -o src/MediPro.Dashboard.Client  --no-https --framework net8.0
dotnet new web        -n MediPro.Dashboard         -o src/MediPro.Dashboard         --no-https --framework net8.0
dotnet new worker     -n MediPro.Worker            -o src/MediPro.Worker            --framework net8.0

# --- Add to Solution ---
dotnet sln add src/MediPro.Core/MediPro.Core.csproj
dotnet sln add src/MediPro.Dicom/MediPro.Dicom.csproj
dotnet sln add src/MediPro.Print/MediPro.Print.csproj
dotnet sln add src/MediPro.Quota/MediPro.Quota.csproj
dotnet sln add src/MediPro.Dashboard/MediPro.Dashboard.csproj
dotnet sln add src/MediPro.Dashboard.Client/MediPro.Dashboard.Client.csproj
dotnet sln add src/MediPro.Worker/MediPro.Worker.csproj

# --- Project References ---
dotnet add src/MediPro.Dicom/MediPro.Dicom.csproj             reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Print/MediPro.Print.csproj             reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Quota/MediPro.Quota.csproj             reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj     reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj     reference src/MediPro.Dashboard.Client/MediPro.Dashboard.Client.csproj
dotnet add src/MediPro.Dashboard.Client/MediPro.Dashboard.Client.csproj reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Worker/MediPro.Worker.csproj           reference src/MediPro.Core/MediPro.Core.csproj
dotnet add src/MediPro.Worker/MediPro.Worker.csproj           reference src/MediPro.Dicom/MediPro.Dicom.csproj
dotnet add src/MediPro.Worker/MediPro.Worker.csproj           reference src/MediPro.Print/MediPro.Print.csproj
dotnet add src/MediPro.Worker/MediPro.Worker.csproj           reference src/MediPro.Quota/MediPro.Quota.csproj
dotnet add src/MediPro.Worker/MediPro.Worker.csproj           reference src/MediPro.Dashboard/MediPro.Dashboard.csproj

# --- NuGet Packages (--no-restore for speed, single restore at end) ---
Write-Host "=== Adding NuGet Packages ===" -ForegroundColor Yellow

dotnet add src/MediPro.Core/MediPro.Core.csproj             package Serilog.AspNetCore --no-restore

dotnet add src/MediPro.Dicom/MediPro.Dicom.csproj           package fo-dicom --no-restore
dotnet add src/MediPro.Dicom/MediPro.Dicom.csproj           package Magick.NET-Q16-AnyCPU --no-restore
dotnet add src/MediPro.Dicom/MediPro.Dicom.csproj           package QuestPDF --no-restore

dotnet add src/MediPro.Print/MediPro.Print.csproj           package System.Drawing.Common --no-restore
dotnet add src/MediPro.Print/MediPro.Print.csproj           package System.Management --no-restore

dotnet add src/MediPro.Quota/MediPro.Quota.csproj           package FirebaseAdmin --no-restore
dotnet add src/MediPro.Quota/MediPro.Quota.csproj           package Google.Cloud.Firestore --no-restore

dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj   package Microsoft.EntityFrameworkCore.Sqlite --no-restore
dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj   package Microsoft.EntityFrameworkCore.Design --no-restore
dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj   package Microsoft.AspNetCore.Components.WebAssembly.Server --no-restore
dotnet add src/MediPro.Dashboard/MediPro.Dashboard.csproj   package Serilog.AspNetCore --no-restore

dotnet add src/MediPro.Worker/MediPro.Worker.csproj         package Microsoft.Extensions.Hosting.WindowsServices --no-restore
dotnet add src/MediPro.Worker/MediPro.Worker.csproj         package Serilog.AspNetCore --no-restore
dotnet add src/MediPro.Worker/MediPro.Worker.csproj         package Serilog.Sinks.File --no-restore
dotnet add src/MediPro.Worker/MediPro.Worker.csproj         package Serilog.Sinks.Console --no-restore

# --- Cleanup template files ---
Remove-Item src/MediPro.Core/Class1.cs  -ErrorAction SilentlyContinue
Remove-Item src/MediPro.Dicom/Class1.cs -ErrorAction SilentlyContinue
Remove-Item src/MediPro.Print/Class1.cs -ErrorAction SilentlyContinue
Remove-Item src/MediPro.Quota/Class1.cs -ErrorAction SilentlyContinue

# --- Restore all at once ---
Write-Host "=== Restoring All Packages ===" -ForegroundColor Yellow
dotnet restore MediPro.sln

Write-Host "=== Scaffold Complete ===" -ForegroundColor Green
