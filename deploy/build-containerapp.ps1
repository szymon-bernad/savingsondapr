param(
 [string]$ContainerRegName,
 [string]$ResGroupName,
 [switch]$DoTheBuild,
 [switch]$DoAzLogin,
 [switch]$DeployToAzEnv)

. .\build-common.ps1 

if($DoAzLogin)
{
	az login
}

$acrLoginCmd = ('az acr login -n {0}' -f $ContainerRegName)
Invoke-Expression $acrLoginCmd

### build app
$appName = 'savingsondapr.api'
$apiVerCmd = ('docker images {0} --format "{{{{.Tag}}}}"' -f $appName)
Write-Host  ('dockerCmd: {0}' -f $ApiVerCmd)
$verNo = GetContainerVer $apiVerCmd
if ($verNo -eq '')
{
	$verNo = 0
}
Write-Host  ('verNo: {0}' -f $verNo)

if ($DoTheBuild)
{
	Push-Location -Path ..\SavingsOnDapr.Api
	
	BuildContainerApp $appName $ContainerRegName $verNo
	
	Pop-Location
}
if ($DeployToAzEnv)
{
	$rgExistsExpr = ('az group exists -n {0}' -f $ResGroupName)
	$rgExists = (Invoke-Expression $rgExistsExpr | Out-String)

	if ($rgExists.Trim() -eq "false")
	{
		Write-Host 'Resource-Group not found... Terminating.'
    }
    else 
    {
        $currentDate = Get-Date -Format "yyyy-MM-ddHHmmss"
		$runDeploymentExpr = ('az deployment group create --name {0}-d{1} --resource-group {0} --template-file main-app-update.bicep --parameters main-app-update.params.bicepparam' -f $ResGroupName, $currentDate)
	    Invoke-Expression $runDeploymentExpr
    }

}
