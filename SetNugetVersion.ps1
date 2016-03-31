# SeNugettVersion.ps1
#
# Set the nuget version in based on exe fileversion.
#
# usage:  
#  from cmd.exe: 
#     powershell.exe SetNugetVersion.ps1 filename.ext Nuspecfileandpath
# 
#  from powershell.exe prompt: 
#     .\SetNugetVersion.ps1 filename.ext Nuspecfileandpath
#
# last saved Time-stamp: <Wednesday, April 23, 2008  11:52:15  (by dinoch)>
#

 
function Usage
{
  echo "Usage: ";
  echo "  from cmd.exe: ";
  echo "     powershell.exe SetNugetVersion.ps1  filename.ext Nuspecfileandpath";
  echo " ";
  echo "  from powershell.exe prompt: ";
  echo "     .\SetNugetVersion.ps1  filename.ext Nuspecfileandpath";
  echo " ";
}


function Update-Nuget_Version_In_Nuspec_File([string]$File,[string]$NuspecFile)
{
  

  $FileVersion = get-fileversion $File

  echo $FileVersion

  $xml = [xml] (Get-Content $NuspecFile)	
  $versioNode = $xml.package.metadata.version
  $versioNode.innerxml = $fileVersion

}

  Update-Nuget_Version_In_Nuspec_File $args[0];
