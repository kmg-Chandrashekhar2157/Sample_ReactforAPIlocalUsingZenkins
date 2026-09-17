pipeline {
    agent {
        label 'windows-iis-POC-API'
    }

    options {
        skipDefaultCheckout(true)
        timestamps()
    }

    environment {
        IIS_SITE = 'ApiJenkins'
        IIS_APPPOOL = 'ApiJenkins'
        IIS_PATH = 'C:\\inetpub\\wwwroot\\ApiJenkins'
        BACKUP_ROOT = 'C:\\JenkinsIISBackups\\ApiJenkins'
        HEALTH_URL = 'http://localhost:8083'
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
                    if exist "%WORKSPACE%\\publish" rmdir /s /q "%WORKSPACE%\\publish"

                    dotnet publish Poc.Api.csproj ^
                    --configuration Release ^
                    --output "%WORKSPACE%\\publish" ^
                    --no-build
                '''
            }
        }

        stage('Backup IIS') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    $backupRoot = $env:BACKUP_ROOT
                    $iisPath = $env:IIS_PATH

                    if (-not (Test-Path $iisPath)) {
                        Write-Host "IIS deployment folder does not exist. Skipping backup."
                        return
                    }

                    if (-not (Test-Path $backupRoot)) {
                        New-Item -ItemType Directory -Path $backupRoot -Force | Out-Null
                    }

                    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
                    $backupPath = Join-Path $backupRoot $timestamp

                    New-Item -ItemType Directory -Path $backupPath -Force | Out-Null

                    Write-Host "Creating IIS backup..."
                    Write-Host "Source : $iisPath"
                    Write-Host "Backup : $backupPath"

                    Copy-Item "$iisPath\\*" $backupPath -Recurse -Force

                    Write-Host "Backup completed successfully."

                    # Store backup path for rollback
                    Set-Content "$env:WORKSPACE\\iis-backup-path.txt" $backupPath
                '''
            }
        }

        stage('Stop IIS App Pool') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    Import-Module WebAdministration

                    Write-Host "Stopping IIS App Pool: $env:IIS_APPPOOL"

                    if ((Get-WebAppPoolState -Name $env:IIS_APPPOOL).Value -ne "Stopped") {
                        Stop-WebAppPool -Name $env:IIS_APPPOOL
                    }

                    Start-Sleep -Seconds 3

                    Write-Host "App Pool stopped."
                '''
            }
        }

        stage('Deploy to IIS') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    $source = Join-Path $env:WORKSPACE "publish"
                    $destination = $env:IIS_PATH

                    Write-Host "Deploying application..."
                    Write-Host "Source      : $source"
                    Write-Host "Destination : $destination"

                    if (-not (Test-Path $source)) {
                        throw "Publish folder not found: $source"
                    }

                    if (-not (Test-Path $destination)) {
                        New-Item -ItemType Directory -Path $destination -Force | Out-Null
                    }

                    # Remove old deployment files
                    Get-ChildItem -Path $destination -Force |
                        Remove-Item -Recurse -Force

                    # Copy new deployment
                    Copy-Item "$source\\*" $destination -Recurse -Force

                    Write-Host "Deployment files copied successfully."
                '''
            }
        }

        stage('Start IIS App Pool') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    Import-Module WebAdministration

                    Write-Host "Starting IIS App Pool: $env:IIS_APPPOOL"

                    Start-WebAppPool -Name $env:IIS_APPPOOL

                    Start-Sleep -Seconds 5

                    $state = (Get-WebAppPoolState -Name $env:IIS_APPPOOL).Value

                    Write-Host "App Pool state: $state"

                    if ($state -ne "Started") {
                        throw "IIS App Pool failed to start."
                    }
                '''
            }
        }

        stage('Health Check') {
            steps {
                powershell '''
                    $ErrorActionPreference = "Stop"

                    $url = $env:HEALTH_URL

                    Write-Host "Checking application: $url"

                    $success = $false

                    for ($i = 1; $i -le 12; $i++) {

                        try {
                            $response = Invoke-WebRequest `
                                -Uri $url `
                                -UseBasicParsing `
                                -TimeoutSec 10

                            Write-Host "HTTP Status: $($response.StatusCode)"

                            if ($response.StatusCode -ge 200 -and $response.StatusCode -lt 400) {
                                $success = $true
                                break
                            }
                        }
                        catch {
                            Write-Host "Health check attempt $i failed."
                            Write-Host $_.Exception.Message
                        }

                        Write-Host "Waiting 5 seconds..."
                        Start-Sleep -Seconds 5
                    }

                    if (-not $success) {
                        throw "Application health check failed."
                    }

                    Write-Host "Application is healthy."
                '''
            }
        }
    }

    post {

        success {
            echo 'CI/CD pipeline completed successfully.'
            echo 'Application deployed successfully to IIS.'
        }

        failure {
            echo 'Pipeline failed. Starting IIS rollback...'

            powershell '''
                $ErrorActionPreference = "Continue"

                Import-Module WebAdministration

                $backupFile = "$env:WORKSPACE\\iis-backup-path.txt"

                if (-not (Test-Path $backupFile)) {
                    Write-Host "No backup information found. Rollback cannot be performed."
                    exit 0
                }

                $backupPath = (Get-Content $backupFile -Raw).Trim()

                if (-not (Test-Path $backupPath)) {
                    Write-Host "Backup folder not found: $backupPath"
                    exit 0
                }

                Write-Host "======================================"
                Write-Host "ROLLBACK STARTED"
                Write-Host "Backup: $backupPath"
                Write-Host "======================================"

                try {
                    Stop-WebAppPool -Name $env:IIS_APPPOOL
                    Start-Sleep -Seconds 3
                }
                catch {
                    Write-Host "Could not stop App Pool."
                }

                try {
                    if (Test-Path $env:IIS_PATH) {
                        Get-ChildItem -Path $env:IIS_PATH -Force |
                            Remove-Item -Recurse -Force
                    }

                    Copy-Item "$backupPath\\*" `
                        $env:IIS_PATH `
                        -Recurse `
                        -Force

                    Start-WebAppPool -Name $env:IIS_APPPOOL

                    Start-Sleep -Seconds 5

                    Write-Host "======================================"
                    Write-Host "ROLLBACK COMPLETED"
                    Write-Host "======================================"
                }
                catch {
                    Write-Host "ROLLBACK FAILED:"
                    Write-Host $_.Exception.Message
                }
            '''
        }
    }
}