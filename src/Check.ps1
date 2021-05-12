#!/usr/bin/env pwsh

<#
This script runs locally the checks from the continous integration.
#>

function Main
{
    Push-Location

    Set-Location $PSScriptRoot

    $nl = [System.Environment]::NewLine

    Write-Host "Check.ps1: Checking the format...$nl"
    dotnet format --check
    if ($LASTEXITCODE -ne 0)
    {
        throw "Format check failed."
    }

    Write-Host "${nl}Check.ps1: Checking the line length and number of lines...${nl}"
    dotnet bite-sized `
        --inputs '**/*.cs' `
        --excludes '**/obj/**' '**/bin/**' 'packages/**' `
        --max-lines-in-file 2000 `
        --max-line-length 90 `
        --ignore-lines-matching '[a-z]+://[^ \t]+$'

    if ($LASTEXITCODE -ne 0)
    {
        throw "The check of line width failed."
    }

    Write-Host "${nl}Check.ps1: Checking the dead code...${nl}"
    dotnet dead-csharp `
        --inputs '**/*.cs' `
        --excludes '**/obj/**' '**/bin/**' 'packages/**'
    if ($LASTEXITCODE -ne 0)
    {
        throw "The check of dead code failed."
    }

    Write-Host "${nl}Check.ps1: Checking the TODOs...${nl}"
    dotnet opinionated-csharp-todos `
        --inputs '**/*.cs' `
        --excludes '**/obj/**' '**/bin/**' 'packages/**'
    if ($LASTEXITCODE -ne 0)
    {
        throw "The check of dead code failed."
    }

    Write-Host "${nl}Check.ps1: Running the unit tests...${nl}"
    dotnet test /p:CollectCoverage=true
    if ($LASTEXITCODE -ne 0)
    {
        throw "The unit tests failed."
    }

    Write-Host "${nl}Check.ps1: Inspecting the code with JetBrains InspectCode...${nl}"
    dotnet jb inspectcode `
        '--exclude=*\obj\*;packages\*;*\bin\*;*\*.json' `
        opinionated-usings.sln

    $outDir = Join-Path (Split-Path -Parent $PSScriptRoot) "out"
    Write-Host "${nl}Check.ps1: Publishing to $outDir ... ${nl}"
    dotnet publish -c Release -o $outDir

    Write-Host "${nl}Check.ps1: Checking --help in Readme...${nl}"
    ./CheckHelpInReadme.ps1

    Pop-Location
}

Main