$ErrorActionPreference = "Stop"

$root = Resolve-Path (Join-Path $PSScriptRoot "..")
$publishDir = Join-Path $root "artifacts\desktop\publish"
$payloadDir = Join-Path $root "artifacts\desktop\payload"
$installerProjectDir = Join-Path $root "artifacts\desktop\installer-project"
$installerPublishDir = Join-Path $root "artifacts\desktop\installer-publish"
$installerPath = Join-Path $root "artifacts\desktop\OnlineCourses.Desktop.Setup.exe"
$desktopProjectPath = Join-Path $root "OnlineCourses.Desktop\OnlineCourses.Desktop.csproj"

Remove-Item -LiteralPath $publishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $payloadDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installerProjectDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installerPublishDir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $installerPath -Force -ErrorAction SilentlyContinue

New-Item -ItemType Directory -Path $publishDir | Out-Null
New-Item -ItemType Directory -Path $payloadDir | Out-Null
New-Item -ItemType Directory -Path $installerProjectDir | Out-Null

dotnet publish $desktopProjectPath `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    --output $publishDir

Copy-Item -LiteralPath (Join-Path $publishDir "OnlineCourses.Desktop.exe") -Destination (Join-Path $payloadDir "OnlineCourses.Desktop.exe")
Copy-Item -LiteralPath (Join-Path $publishDir "desktopsettings.json") -Destination (Join-Path $payloadDir "desktopsettings.json")

$installerCsproj = @'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <AssemblyName>OnlineCourses.Desktop.Setup</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <EmbeddedResource Include="..\payload\OnlineCourses.Desktop.exe" LogicalName="OnlineCourses.Desktop.exe" />
    <EmbeddedResource Include="..\payload\desktopsettings.json" LogicalName="desktopsettings.json" />
  </ItemGroup>
</Project>
'@

Set-Content -LiteralPath (Join-Path $installerProjectDir "OnlineCourses.Desktop.Setup.csproj") -Value $installerCsproj -Encoding UTF8

$programCs = @'
using System.Diagnostics;
using System.Reflection;

var appDir = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "OnlineCourses.Desktop");

Directory.CreateDirectory(appDir);

ExtractResource("OnlineCourses.Desktop.exe", Path.Combine(appDir, "OnlineCourses.Desktop.exe"));
ExtractResource("desktopsettings.json", Path.Combine(appDir, "desktopsettings.json"));
CreateDesktopShortcut(appDir);

Process.Start(new ProcessStartInfo
{
    FileName = Path.Combine(appDir, "OnlineCourses.Desktop.exe"),
    WorkingDirectory = appDir,
    UseShellExecute = true
});

void ExtractResource(string resourceName, string destinationPath)
{
    using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
        ?? throw new InvalidOperationException($"Resource not found: {resourceName}");
    using var file = File.Create(destinationPath);
    stream.CopyTo(file);
}

void CreateDesktopShortcut(string appDirectory)
{
    var shortcutPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
        "OnlineCourses Desktop.lnk");

    var shellType = Type.GetTypeFromProgID("WScript.Shell")
        ?? throw new InvalidOperationException("WScript.Shell COM object is not available.");

    dynamic shell = Activator.CreateInstance(shellType)
        ?? throw new InvalidOperationException("Failed to create WScript.Shell COM object.");

    dynamic shortcut = shell.CreateShortcut(shortcutPath);
    shortcut.TargetPath = Path.Combine(appDirectory, "OnlineCourses.Desktop.exe");
    shortcut.WorkingDirectory = appDirectory;
    shortcut.Description = "OnlineCourses Desktop";
    shortcut.Save();
}
'@

Set-Content -LiteralPath (Join-Path $installerProjectDir "Program.cs") -Value $programCs -Encoding UTF8

dotnet publish (Join-Path $installerProjectDir "OnlineCourses.Desktop.Setup.csproj") `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $installerPublishDir

Copy-Item -LiteralPath (Join-Path $installerPublishDir "OnlineCourses.Desktop.Setup.exe") -Destination $installerPath

Write-Host "Desktop EXE:" (Join-Path $publishDir "OnlineCourses.Desktop.exe")
Write-Host "Installer:" $installerPath
