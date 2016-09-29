param ($dllPath)

# $version = (Get-Item $dllPath).VersionInfo.FileVersion

# $buildVer = $version.Build
# $minorVer = $version.Minor
# $majorVer = $version.Major
# $semVer = "$($version.Major).$($version.Minor).$($version.Build)"

# $env:BUILD_VER = $buildVer
# $env:MINOR_VER = $minorVer
# $env:MAJOR_VER = $majorVer
# $env:SEM_VER = $semVer

$env:MAJOR_VER = 0
$env:MINOR_VER = 1
$env:BUILD_VER = 0