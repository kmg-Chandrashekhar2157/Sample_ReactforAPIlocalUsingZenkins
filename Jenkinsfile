pipeline {

    agent {
        label 'windows-iis-POC-API'
    }

    options {
        skipDefaultCheckout(true)
    }

    environment {
        IIS_SITE = 'ApiJenkins'
        IIS_APPPOOL = 'ApiJenkins'
        IIS_PATH = 'C:\\inetpub\\wwwroot\\ApiJenkins'
        HEALTH_URL = 'http://localhost:8083/api/health'
    }

    stages {

        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Restore') {
            steps {
                bat 'dotnet restore Poc.Api.csproj'
            }
        }

        stage('Build') {
            steps {
                bat 'dotnet build Poc.Api.csproj --configuration Release --no-restore'
            }
        }

        stage('Test') {
            steps {
                bat 'dotnet test --configuration Release --no-build'
            }
        }

        stage('Publish') {
            steps {
                bat '''
                    if exist "%WORKSPACE%\\publish" (
                        rmdir /s /q "%WORKSPACE%\\publish"
                    )

                    dotnet publish Poc.Api.csproj ^
                        --configuration Release ^
                        --output "%WORKSPACE%\\publish" ^
                        --no-build
                '''
            }
        }

        stage('Stop IIS App Pool') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    Import-Module WebAdministration

                    Write-Host "========================================"
                    Write-Host "Stopping IIS App Pool"
                    Write-Host "App Pool: $env:IIS_APPPOOL"
                    Write-Host "========================================"

                    $state = (Get-WebAppPoolState -Name $env:IIS_APPPOOL).Value

                    Write-Host "Current state: $state"

                    if ($state -ne "Stopped") {
                        Stop-WebAppPool -Name $env:IIS_APPPOOL
                        Start-Sleep -Seconds 3
                    }

                    $state = (Get-WebAppPoolState -Name $env:IIS_APPPOOL).Value

                    Write-Host "Final state: $state"

                    if ($state -ne "Stopped") {
                        throw "Failed to stop IIS App Pool: $env:IIS_APPPOOL"
                    }

                    Write-Host "IIS App Pool stopped successfully."
                '''
            }
        }

        stage('Deploy to IIS') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    $source = Join-Path $env:WORKSPACE "publish"
                    $destination = $env:IIS_PATH

                    Write-Host "========================================"
                    Write-Host "Deploying Application"
                    Write-Host "Source      : $source"
                    Write-Host "Destination : $destination"
                    Write-Host "========================================"

                    if (-not (Test-Path $source)) {
                        throw "Publish folder does not exist: $source"
                    }

                    if (-not (Test-Path $destination)) {
                        New-Item `
                            -ItemType Directory `
                            -Path $destination `
                            -Force | Out-Null
                    }

                    Write-Host "Removing existing IIS files..."

                    Get-ChildItem `
                        -Path $destination `
                        -Force |
                        Remove-Item `
                        -Recurse `
                        -Force

                    Write-Host "Copying new application files..."

                    Copy-Item `
                        "$source\\*" `
                        $destination `
                        -Recurse `
                        -Force

                    Write-Host "Deployment completed successfully."
                '''
            }
        }

        stage('Start IIS App Pool') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    Import-Module WebAdministration

                    Write-Host "========================================"
                    Write-Host "Starting IIS App Pool"
                    Write-Host "App Pool: $env:IIS_APPPOOL"
                    Write-Host "========================================"

                    Start-WebAppPool -Name $env:IIS_APPPOOL

                    Start-Sleep -Seconds 5

                    $state = (Get-WebAppPoolState -Name $env:IIS_APPPOOL).Value

                    Write-Host "App Pool state: $state"

                    if ($state -ne "Started") {
                        throw "IIS App Pool failed to start."
                    }

                    Write-Host "IIS App Pool started successfully."
                '''
            }
        }

        stage('Health Check') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    $url = $env:HEALTH_URL
                    $success = $false

                    Write-Host "========================================"
                    Write-Host "Application Health Check"
                    Write-Host "URL: $url"
                    Write-Host "========================================"

                    for ($i = 1; $i -le 12; $i++) {

                        try {

                            $response = Invoke-WebRequest `
                                -Uri $url `
                                -UseBasicParsing `
                                -TimeoutSec 10

                            Write-Host "Attempt $i"
                            Write-Host "HTTP Status: $($response.StatusCode)"

                            if ($response.StatusCode -ge 200 -and
                                $response.StatusCode -lt 400) {

                                $success = $true
                                break
                            }

                        }
                        catch {

                            Write-Host "Attempt $i failed:"
                            Write-Host $_.Exception.Message
                        }

                        Write-Host "Waiting 5 seconds..."
                        Start-Sleep -Seconds 5
                    }

                    if (-not $success) {
                        throw "Application health check FAILED: $url"
                    }

                    Write-Host "========================================"
                    Write-Host "APPLICATION HEALTH CHECK PASSED"
                    Write-Host "========================================"
                '''
            }
        }
    }

    post {

        success {
            echo '========================================'
            echo 'CI/CD PIPELINE SUCCESSFUL'
            echo 'Application deployed to IIS successfully.'
            echo '========================================'
        }

        failure {
            echo '========================================'
            echo 'CI/CD PIPELINE FAILED'
            echo 'Check the Console Output for the failed stage.'
            echo '========================================'
        }
    }
}