.\.nuget\NuGet.exe install .nuget\packages.config -o packages
.\packages\psake.4.8.0\tools\psake\psake.cmd build.ps1 %*
pause