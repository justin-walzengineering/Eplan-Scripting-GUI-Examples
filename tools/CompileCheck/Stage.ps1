<#
  Copies every example script into a staging folder and prepends the namespaces that
  Eplan is believed to add when it compiles a script, so name clashes (for example
  EventHandler) show up here instead of inside Eplan.

  The extra usings go on the SAME first line as the file's first line, so line numbers in
  error messages still match the real file. Duplicate usings only produce warning CS0105,
  which the project ignores, just as Eplan does.

  Called by CompileCheck.csproj. Not meant to be run by hand.
#>
param(
    [Parameter(Mandatory = $true)][string]$Source,
    [Parameter(Mandatory = $true)][string]$Destination
)

# Assumption, to verify in Eplan: these are the namespaces the script compiler adds.
$injected = 'using System; using System.Windows.Forms; using Eplan.EplApi.ApplicationFramework; using Eplan.EplApi.Base; using Eplan.EplApi.Gui; using Eplan.EplApi.Scripting; '

if (Test-Path $Destination) { Remove-Item $Destination -Recurse -Force }
New-Item -ItemType Directory -Force $Destination | Out-Null

$sourceRoot = (Resolve-Path $Source).Path.TrimEnd('\')
Get-ChildItem $sourceRoot -Recurse -Filter *.cs | ForEach-Object {
    $relative = $_.FullName.Substring($sourceRoot.Length).TrimStart('\')
    $target = Join-Path $Destination $relative
    New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null

    $text = [IO.File]::ReadAllText($_.FullName)
    [IO.File]::WriteAllText($target, $injected + $text, (New-Object Text.UTF8Encoding($false)))
}
