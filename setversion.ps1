$version = Read-Host "Nouveau numéro de version (ex: 1.2.5)"

[xml]$csproj = Get-Content "PulseDL.csproj"
$versionGroup = $csproj.Project.PropertyGroup | Where-Object { $_.Version }
$versionGroup.Version = $version
$versionGroup.AssemblyVersion = "$version.0"
$versionGroup.FileVersion = "$version.0"
$versionGroup.InformationalVersion = $version
$csproj.Save("PulseDL.csproj")