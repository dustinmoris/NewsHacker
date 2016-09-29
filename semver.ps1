param ($dllPath)
$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dllPath).FileVersion
$majorVer = $version.Major
$minorVer = $version.Minor
$buildVer = $version.Build
$semVer = "$($version.Major).$($version.Minor).$($version.Build)"

[Environment]::SetEnvironmentVariable("SEM_VER", $semVer, "Machine")
[Environment]::SetEnvironmentVariable("MAJOR_VER", $majorVer, "Machine")
[Environment]::SetEnvironmentVariable("MINOR_VER", $minorVer, "Machine")
[Environment]::SetEnvironmentVariable("BUILD_VER", $buildVer, "Machine")