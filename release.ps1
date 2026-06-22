[xml]$csproj = Get-Content "PulseDL.csproj"
$versionGroup = $csproj.Project.PropertyGroup | Where-Object { $_.Version }
$actVer = $versionGroup.Version
$version = Read-Host "Numéro de version à release (actuelle: $actVer)"

$ffmpegUpdate = $false
$ytdlpUpdate = $false

choice /c ON /m "Mettre à jour ffmpeg ?"
if ($LASTEXITCODE -eq 1) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Filter = "ffmpeg.exe|ffmpeg.exe"
    $dialog.Title = "Selectionnez ffmpeg.exe"

    if ($dialog.ShowDialog() -eq "OK") {
        $filePath = $dialog.FileName
        Remove-Item -Path "P:\ffmpeg.exe" -ErrorAction Ignore
        Copy-Item -Path $filePath -Destination "P:\ffmpeg.exe" -Force
        $ffmpegUpdate = $true
    }
}

choice /c ON /m "Mettre à jour ytdlp ?"
if ($LASTEXITCODE -eq 1) {
    Add-Type -AssemblyName System.Windows.Forms
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Filter = "yt-dlp.exe|yt-dlp.exe"
    $dialog.Title = "Selectionnez yt-dlp.exe"

    if ($dialog.ShowDialog() -eq "OK") {
        $filePath = $dialog.FileName
        Remove-Item -Path "P:\yt-dlp.exe" -ErrorAction Ignore
        Copy-Item -Path $filePath -Destination "P:\yt-dlp.exe" -Force
        $ytdlpUpdate = $true
    }
}

Remove-Item -Path "Release" -Recurse -Force -ErrorAction Ignore

$originalLocation = Get-Location

$versionGroup.Version = "$version"
$versionGroup.AssemblyVersion = "$version.0"
$versionGroup.FileVersion = "$version.0"
$versionGroup.InformationalVersion = "$version"
$csproj.Save("PulseDL.csproj")

dotnet.exe publish -c Release -r win-x64 -p:Platform=x64 -o ./Release/Unpackaged

$iss = Get-Content "package.iss" -Raw
$iss = $iss -replace `
    '#define MyAppVersion ".*"', `
    "#define MyAppVersion `"$version`""

Set-Content "package.iss" $iss

Set-Location "Release"

ISCC.exe "../package.iss"

Remove-Item -Path "P:\PulseDL-*-setup.exe"
Copy-Item -Path "PulseDL-$version-setup.exe" -Destination "P:\PulseDL-$version-setup.exe"

Set-Location "P:"

$latestJson = Get-Content "latest.json" -Raw | ConvertFrom-Json
$latestJson.core.version = $version
$latestJson.core.file = "https://cdn.pulsedl.fouinard.fr/PulseDL-$version-setup.exe"
$latestJson.core.checksum = (Get-FileHash "P:\PulseDL-$version-setup.exe" -Algorithm SHA256).Hash

if ($ffmpegUpdate) {
    $latestJson.ffmpeg.version = (& "P:\ffmpeg.exe" -version 2>&1)[0]
    $latestJson.ffmpeg.checksum = (Get-FileHash "P:\ffmpeg.exe" -Algorithm SHA256).Hash
}

if ($ytdlpUpdate) {
    $latestJson.ytdlp.version = (& "P:\yt-dlp.exe" -version 2>&1)[0]
    $latestJson.ytdlp.checksum = (Get-FileHash "P:\yt-dlp.exe" -Algorithm SHA256).Hash
}

$latestJson | ConvertTo-Json -Depth 10 | Set-Content "latest.json"

Set-Location $originalLocation