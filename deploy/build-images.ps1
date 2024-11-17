param(
 [string]$ContainerRegName,
 [switch]$DoAzLogin,
 [switch]$DeployToAzEnv)
 

function GetContainerVer {
	param ([string]$vercmd)
	
	$verOutput = (Invoke-Expression $vercmd | Out-String) -split '\r\n' | Sort-Object -Descending | Select-Object -First 1
	if ([string]::IsNullOrEmpty($verOutput))
	{
		$version = 0
	} 
	else
	{
	$version = [Convert]::ToInt32($verOutput.Substring($verOutput.LastIndexOf('.') + 1))
	}
	
	$version
}

if ($DoAzLogin)
{
	az login
	$acrLoginCmd = ('az acr login -n {0}' -f $ContainerRegName)
	Invoke-Expression $acrLoginCmd
}

### savingsondapr.api
$apiVerCmd = 'docker images savingsondapr.api --format "{{.Tag}}"'
$verNo = GetContainerVer($apiVerCmd)
if ($verNo -eq '')
{
	$verNo = 0
}

Push-Location -Path ..\SavingsOnDapr.Api

$path = Get-Location

$path = $path.ToString().Replace('\','\\')
$buildcmd = ('docker build -t "savingsondapr.api:0.{0}" -f ./Dockerfile ..' -f (++$verNo))

Invoke-Expression $buildcmd
$targetImg = ('{0}.azurecr.io/savingsondapr.api:0.{1}' -f $ContainerRegName, $verNo)
$tagcmd = ('docker tag savingsondapr.api:0.{0} {1}' -f $verNo, $targetImg)
Invoke-Expression $tagcmd
$pushcmd = ('docker push {0}' -f $targetImg)
Invoke-Expression $pushcmd

$Env:API_IMGVER = ('0.{0}' -f $verNo)
Pop-Location

### savingsondapr.eventstore
$apiVerCmd = 'docker images savingsondapr.eventstore --format "{{.Tag}}"'
$verNo = GetContainerVer($apiVerCmd)
if ($verNo -eq '')
{
	$verNo = 0
}

Push-Location -Path ..\SavingsOnDapr.EventStore

$path = Get-Location

$path = $path.ToString().Replace('\','\\')
$buildcmd = ('docker build -t "savingsondapr.eventstore:0.{0}" -f ./Dockerfile ..' -f (++$verNo))

Invoke-Expression $buildcmd
$targetImg = ('{0}.azurecr.io/savingsondapr.eventstore:0.{1}' -f $ContainerRegName, $verNo)
$tagcmd = ('docker tag savingsondapr.eventstore:0.{0} {1}' -f $verNo, $targetImg)
Invoke-Expression $tagcmd
$pushcmd = ('docker push {0}' -f $targetImg)
Invoke-Expression $pushcmd

$Env:EVT_IMGVER = ('0.{0}' -f $verNo)
Pop-Location

if ($DeployToAzEnv)
{
	$rgExists = (az group exists -n savings-platform-poc-rg)
	Write-Output $rgExists
	if ($rgExists -eq 'false')
	{
		Write-Output 'Creating resource-group...'
		az group create -n savings-platform-poc-rg --location westeurope
	}
	
	do {
		$rgExists = (az group exists -n savings-platform-poc-rg)
		Start-Sleep -Seconds 1
	}
	while ($rgExists -eq 'false')
	
	az deployment group create --name savings-platform-deploy2 --resource-group savings-platform-poc-rg --template-file main.bicep --parameters main.params.bicepparam
}
