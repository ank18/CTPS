
clear

#$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent([Security.Principal.TokenAccessLevels]'Query,Duplicate'))
#echo $currentPrincipal
$creds
if (userName.Length > 0 && password.Length > 0)

            {
                System.Security.SecureString securePassword = new System.Security.SecureString();
 
                foreach (char c in password.ToCharArray())
                {
                    securePassword.AppendChar(c);
                }
 
                creds = new PSCredential(userName, securePassword);
            }
            else
            {
                // Use Windows Authentication
                creds = (PSCredential)null;
            }

			echo $creds
$Hosts = @("WD1WF11A")

ForEach ($Server in $Hosts){

Invoke-Command -ComputerName $Server -ScriptBlock {choco install -source http://wd1wf06b/Continuous/nuget TelephonyPlatformServicesChoco} -Credential $creds

}