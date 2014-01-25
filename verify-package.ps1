param($packageSource)

$ErrorActionPreference = "Stop"

trap
{
    Write-Error $_
    exit 1
}

function Verify-ExitCode
{
    if ($LASTEXITCODE -ne 0) 
    {
        exit 1
    }
}

$path = Split-Path $MyInvocation.MyCommand.Path

if(!$packageSource)
{
    $packageSource = "$path\artifacts\sdk"
}

$testPath = "$path\TestResults"

# Remove the test folder (if it exists)
if(Test-Path $testPath)
{
    rm -Recurse -Force $testPath
}

# Install the package into the artifacts\test folder
& $path\.nuget\nuget.exe install ProjectK -pre -Source $packageSource -output $testPath -NoCache
Verify-ExitCode

$projectKFolder = @(ls $testPath\ProjectK*)[0].FullName

Write-Host "Found ProjectK = $projectKFolder"

function Run-Tests($targetFramework)
{
    try
    {
        $folder = "net45"
        if($targetFramework)
        {
            $env:TARGET_FRAMEWORK=$targetFramework
            $folder = $targetFramework;
        }

        $testResults = "$testPath\$folder"

        mkdir $testResults -Force | Out-Null

        Write-Host "Running tests for $folder"

        Write-Host "Running Hello World"
        & $projectKFolder\tools\k run $path\samples\HelloWorld > $testResults\run.txt
        Verify-ExitCode
        
        Write-Host "Running Building World"
        & $projectKFolder\tools\k build $path\samples\HelloWorld > $testResults\build.txt
        Verify-ExitCode
        
        Write-Host "Running Cleaning World"
        & $projectKFolder\tools\k clean $path\samples\HelloWorld > $testResults\clean.txt
        Verify-ExitCode

        Write-Host "Checking output"
        @("run", "build", "clean") | %{
            $file = $_
            cat $testResults\$file.txt | %{
                if($_.Contains("Error") -or $_.Contains("Failed")) { throw "Error detected in $file.txt; $_"; }
            }
        }

        Write-Host "Tests passed for $folder"
    }
    finally
    {
        if($targetFramework)
        {
            rm env:\TARGET_FRAMEWORK
        }
    }
}

Run-Tests
Run-Tests "k10"
