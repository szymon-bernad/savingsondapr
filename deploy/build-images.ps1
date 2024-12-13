param(
 [string]$ContainerRegName,
 [string]$ResGroupName,
 [switch]$DoTheBuild,
 [switch]$DeployToAzEnv)

. .\build-common.ps1 

az login
$acrLoginCmd = ('az acr login -n {0}' -f $ContainerRegName)
Invoke-Expression $acrLoginCmd

### savingsondapr.api
$apiVerCmd = 'docker images savingsondapr.api --format "{{.Tag}}"'
$verNo = GetContainerVer $apiVerCmd
if ($verNo -eq '')
{
	$verNo = 0
}
Write-Host  ('verNo: {0}' -f $verNo)

if ($DoTheBuild)
{
	Push-Location -Path ..\SavingsOnDapr.Api
	
	$verNo = BuildContainerApp 'savingsondapr.api' $ContainerRegName $verNo
	
	Pop-Location
}
$Env:API_IMGVER = ('0.{0}' -f $verNo)

### savingsondapr.eventstore
$apiVerCmd = 'docker images savingsondapr.eventstore --format "{{.Tag}}"'
$verNo = 0
$verNo = GetContainerVer $apiVerCmd
if ($verNo -eq '')
{
	$verNo = 0
}

if ($DoTheBuild)
{
	Push-Location -Path ..\SavingsOnDapr.EventStore

	$verNo = BuildContainerApp 'savingsondapr.eventstore' $ContainerRegName $verNo

	Pop-Location
}
$Env:EVT_IMGVER = ('0.{0}' -f $verNo)

### currencyexchange.api
$apiVerCmd = 'docker images savingsondapr.currencyexchange --format "{{.Tag}}"'
$verNo = 0
$verNo = GetContainerVer $apiVerCmd
if ($verNo -eq '')
{
	$verNo = 0
}

if ($DoTheBuild)
{
	Push-Location -Path ..\CurrencyExchange.Api

	$verNo = BuildContainerApp 'savingsondapr.currencyexchange' $ContainerRegName $verNo

	Pop-Location
}
$Env:EXCH_IMGVER = ('0.{0}' -f $verNo)

if ($DeployToAzEnv)
{
	$rgExistsExpr = ('az group exists -n {0}' -f $ResGroupName)
	$rgExists = (Invoke-Expression $rgExistsExpr | Out-String)

	if ($rgExists.Trim() -eq "false")
	{
		Write-Output 'Creating resource-group...'
		$createRgExpr = ('az group create -n {0} --location "Poland Central"' -f $ResGroupName)
		Invoke-Expression $createRgExpr
	}
	
	do 
	{
		$rgExists = (Invoke-Expression $rgExistsExpr | Out-String)
		Start-Sleep -Seconds 1
	}
	while ($rgExists.Trim() -eq "false")
	$currentDate = Get-Date -Format "yyyy-MM-ddHHmmss"
	$runDeploymentExpr = ('az deployment group create --name {0}-d{1} --resource-group {0} --template-file main.bicep --parameters main.params.bicepparam' -f $ResGroupName, $currentDate)
	Invoke-Expression $runDeploymentExpr
}
