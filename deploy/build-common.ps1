function GetContainerVer {
	param ([string]$vercmd)
	
	$rawout = (Invoke-Expression $vercmd | Out-String) -split '\r\n' | `
	ForEach-Object { if([string]::IsNullOrEmpty($_)) { '' } else { [Convert]::ToInt32($_.Substring($_.LastIndexOf('.') + 1)) } } | `
	Sort-Object -Descending

	$verOutput = $rawout | Select-Object -First 1

	if ([string]::IsNullOrEmpty($verOutput))
	{
		$version = 0
	} 
	else
	{
	   $version = $verOutput
	}
	
	$version
}

function BuildContainerApp {
    param ([string] $appName,
           [string] $containerRegName,
           [int] $verNo)
    
    $path = Get-Location
    $path = $path.ToString().Replace('\','\\')

    $buildcmd = ('docker build -t "{0}:0.{1}" -f ./Dockerfile ..' -f $appName, (++$verNo))
    Invoke-Expression $buildcmd
    $targetImg = ('{0}.azurecr.io/{1}:0.{2}' -f $containerRegName, $appName, $verNo)
    $tagcmd = ('docker tag {0}:0.{1} {2}' -f $appName, $verNo, $targetImg)
    Invoke-Expression $tagcmd
    $pushcmd = ('docker push {0}' -f $targetImg)
    Invoke-Expression $pushcmd

    $Env:API_IMGVER = ('0.{0}' -f $verNo)
}
