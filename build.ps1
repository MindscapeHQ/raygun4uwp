# This causes psake to use the VS 2017 build tool:
Framework "4.6"

properties {
  $root =             $psake.build_script_dir

  $solution_file =    "$root/Raygun4UWP.sln"
  $nuget_dir =        "$root\.nuget"
  $build_dir_anycpu = "$root\Raygun4UWP\bin\Release"
  $build_dir_x64 =    "$root\Raygun4UWP\bin\x64\Release"
  $build_dir_x86 =    "$root\Raygun4UWP\bin\x86\Release"
  $build_dir_arm =    "$root\Raygun4UWP\bin\ARM\Release"
  $release_dir =      "$root\release"

  $configuration =    "Release"

  $env:Path +=        ";$nuget_dir"
}

task Install_VSSetup {
  exec { Install-Module VSSetup -Scope CurrentUser }
}

task Clean -depends Install_VSSetup {
  exec { msbuild "$solution_file" /t:clean /p:Configuration=$configuration /p:Platform="Any CPU" }
  exec { msbuild "$solution_file" /t:clean /p:Configuration=$configuration /p:Platform=x64 }
  exec { msbuild "$solution_file" /t:clean /p:Configuration=$configuration /p:Platform=x86 }
  exec { msbuild "$solution_file" /t:clean /p:Configuration=$configuration /p:Platform=ARM }

  remove-item -force -recurse $release_dir -ErrorAction SilentlyContinue | Out-Null
  new-item $release_dir -itemType directory | Out-Null
}

task Build -depends Clean {
  exec { msbuild "$solution_file" /p:Configuration=$configuration /p:Platform="Any CPU" }
  exec { msbuild "$solution_file" /p:Configuration=$configuration /p:Platform=x64 }
  exec { msbuild "$solution_file" /p:Configuration=$configuration /p:Platform=x86 }
  exec { msbuild "$solution_file" /p:Configuration=$configuration /p:Platform=ARM }
}

task Package -depends Build {
  exec { nuget pack Raygun4UWP.nuspec -OutputDirectory $release_dir }
}

task Zip -depends Package {
  $release = Get-ChildItem $release_dir | Select-Object -f 1
  $nupkg_name = $release.Name

  $outerfolder =      $release_dir + "\" + ($nupkg_name -replace ".nupkg", "")
  $raygun4uwpfolder = $outerfolder + "\Raygun4UWP"
  $anycpufolder =     $raygun4uwpfolder + "\AnyCPU"
  $x64folder =        $raygun4uwpfolder + "\x64"
  $x86folder =        $raygun4uwpfolder + "\x86"
  $armfolder =        $raygun4uwpfolder + "\ARM"

  new-item $raygun4uwpfolder -itemType directory | Out-Null
  new-item $anycpufolder -itemType directory | Out-Null
  new-item $x64folder -itemType directory | Out-Null
  new-item $x86folder -itemType directory | Out-Null
  new-item $armfolder -itemType directory | Out-Null

  # Any CPU
  copy-item $build_dir_anycpu/Raygun4UWP.dll $anycpufolder
  copy-item $build_dir_anycpu/Raygun4UWP.pdb $anycpufolder
  copy-item $build_dir_anycpu/Raygun4UWP.pri $anycpufolder
  # x64
  copy-item $build_dir_x64/Raygun4UWP.dll $x64folder
  copy-item $build_dir_x64/Raygun4UWP.pdb $x64folder
  copy-item $build_dir_x64/Raygun4UWP.pri $x64folder
  # x86
  copy-item $build_dir_x86/Raygun4UWP.dll $x86folder
  copy-item $build_dir_x86/Raygun4UWP.pdb $x86folder
  copy-item $build_dir_x86/Raygun4UWP.pri $x86folder
  # ARM
  copy-item $build_dir_arm/Raygun4UWP.dll $armfolder
  copy-item $build_dir_arm/Raygun4UWP.pdb $armfolder
  copy-item $build_dir_arm/Raygun4UWP.pri $armfolder

  $zipFullName = $outerfolder + ".zip"
  Get-ChildItem $outerfolder | Add-Zip $zipFullName
}

function Add-Zip # usage: Get-ChildItem $folder | Add-Zip $zipFullName
{
  param([string]$zipfilename)

  if (!(test-path($zipfilename)))
  {
    set-content $zipfilename ("PK" + [char]5 + [char]6 + ("$([char]0)" * 18))
    (dir $zipfilename).IsReadOnly = $false
  }

  $shellApplication = new-object -com shell.application
  $zipPackage = $shellApplication.NameSpace($zipfilename)

  foreach ($file in $input)
  {
    $zipPackage.CopyHere($file.FullName)
    do {
      Start-sleep 2
    } until ( $zipPackage.Items() | select {$_.Name -eq $file.Name} )
  }
}

task default -depends Zip