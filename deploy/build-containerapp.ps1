param(
[Parameter(Mandatory=$true)][string]$ContainerRegName,
[Parameter(Mandatory=$true)][string]$ResGroupName,
[Parameter(Mandatory=$true)] [string]$BuildDir,
[Parameter(Mandatory=$true)][string]$AppName,
[Parameter(Mandatory=$true)][string]$ParamsFile,
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

### get latest version
$apiVerCmd = ('docker images {0} --format "{{{{.Tag}}}}"' -f $AppName)
Write-Host  ('dockerCmd: {0}' -f $ApiVerCmd)
$verNo = GetContainerVer $apiVerCmd
if ($verNo -eq '')
{
	$verNo = 0
}
Write-Host  ('verNo: {0}' -f $verNo)

### build the app
if ($DoTheBuild)
{
	Push-Location -Path $BuildDir
	$verRef = ([ref]$verNo)
	BuildContainerApp -appName $appName -containerRegName $ContainerRegName -verNo $verRef
	$Env:APP_IMGVER = ('0.{0}' -f $verRef.value)
	Pop-Location
}

### run deployment to ACA
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
		$runDeploymentExpr = ('az deployment group create --name {0}-d{1} `
		--resource-group {0} --template-file app-update.bicep --parameters {2}' `
		-f $ResGroupName, $currentDate, $ParamsFile)
	    Invoke-Expression $runDeploymentExpr
    }
}
