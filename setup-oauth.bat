@echo off

echo 🔐 Setting up OAuth credentials for EduShield SIS
echo.

set SECRETS_FILE=src\Api\EduShield.Api\appsettings.Secrets.json
set TEMPLATE_FILE=src\Api\EduShield.Api\appsettings.Secrets.template.json

REM Check if secrets file already exists
if exist "%SECRETS_FILE%" (
    echo ⚠️  Secrets file already exists at %SECRETS_FILE%
    set /p "overwrite=Do you want to overwrite it? (y/N): "
    if /i not "%overwrite%"=="y" (
        echo ❌ Setup cancelled
        exit /b 1
    )
)

REM Copy template to secrets file
copy "%TEMPLATE_FILE%" "%SECRETS_FILE%" >nul
echo ✅ Created secrets file from template

echo.
echo 📝 Next steps:
echo 1. Go to Google Cloud Console: https://console.cloud.google.com/
echo 2. Create/select a project and enable Google+ API
echo 3. Go to 'APIs ^& Services' ^> 'Credentials'
echo 4. Create OAuth 2.0 Client ID
echo 5. Add this redirect URI: http://localhost:5000/api/v1/auth/callback/google
echo 6. Edit %SECRETS_FILE% with your Client ID and Client Secret
echo.
echo 🚀 Then run: dotnet run --project src/Api/EduShield.Api
echo.

pause