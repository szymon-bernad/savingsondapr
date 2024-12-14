function GetContainerVer {
	param ([string]$vercmd)
	
	$verOutput = (Invoke-Expression $vercmd | Out-String) -split '\r\n' | `
	ForEach-Object { [string]::IsNullOrEmpty($_) ? '' : [Convert]::ToInt32($_.Substring($_.LastIndexOf('.') + 1)) } | `
	Sort-Object -Descending |`
 	Select-Object -First 1

	$version = [string]::IsNullOrEmpty($verOutput) ? 0 : $verOutput
	
	return $version
}

function BuildContainerApp {
    param ([string] $appName,
           [string] $containerRegName,
           [ref] $verNo)

	$verNoInt = [Convert]::ToInt32($verNo.value) + 1
    $buildcmd = ('docker build -t "{0}:0.{1}" -f ./Dockerfile ..' -f $appName, $verNoInt)
    Invoke-Expression $buildcmd
    $targetImg = ('{0}.azurecr.io/{1}:0.{2}' -f $containerRegName, $appName, $verNoInt)
    $tagcmd = ('docker tag {0}:0.{1} {2}' -f $appName, $verNoInt, $targetImg)
    Invoke-Expression $tagcmd
    $pushcmd = ('docker push {0}' -f $targetImg)
    Invoke-Expression $pushcmd

	$verNo.value = $verNoInt
}
