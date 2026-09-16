pipeline {
    agent {
        label 'windows-iis-POC-API'
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
                bat 'if exist "%WORKSPACE%\\publish" rmdir /s /q "%WORKSPACE%\\publish"'

                bat '''
                    dotnet publish Poc.Api.csproj ^
                    --configuration Release ^
                    --output "%WORKSPACE%\\publish" ^
                    --no-build
                '''
            }
        }
    }

    post {
        success {
            echo 'CI pipeline completed successfully.'
        }

        failure {
            echo 'CI pipeline failed. Check the console output.'
        }
    }
}