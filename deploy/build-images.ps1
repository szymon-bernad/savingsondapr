param(
 [string]$ContainerRegName,
 [string]$ResGroupName,
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

### currencyexchange.api
$apiVerCmd = 'docker images savingsondapr.currencyexchange --format "{{.Tag}}"'
$verNo = GetContainerVer($apiVerCmd)
if ($verNo -eq '')
{
	$verNo = 0
}

Push-Location -Path ..\CurrencyExchange.Api

$path = Get-Location

$path = $path.ToString().Replace('\','\\')
$buildcmd = ('docker build -t "savingsondapr.currencyexchange:0.{0}" -f ./Dockerfile ..' -f (++$verNo))

Invoke-Expression $buildcmd
$targetImg = ('{0}.azurecr.io/savingsondapr.currencyexchange:0.{1}' -f $ContainerRegName, $verNo)
$tagcmd = ('docker tag savingsondapr.currencyexchange:0.{0} {1}' -f $verNo, $targetImg)
Invoke-Expression $tagcmd
$pushcmd = ('docker push {0}' -f $targetImg)
Invoke-Expression $pushcmd

$Env:EXCH_IMGVER = ('0.{0}' -f $verNo)
Pop-Location

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
	
	do {
		$rgExists = (Invoke-Expression $rgExistsExpr | Out-String)
		Start-Sleep -Seconds 1
	}
	while ($rgExists.Trim() -eq "false")
	
	$runDeploymentExpr = ('az deployment group create --name {0}-dplmnt --resource-group {0} --template-file main.bicep --parameters main.params.bicepparam' -f $ResGroupName)
	Invoke-Expression $runDeploymentExpr
}
