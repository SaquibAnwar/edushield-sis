#!/bin/bash

# Setup script for OAuth credentials

echo "🔐 Setting up OAuth credentials for EduShield SIS"
echo ""

SECRETS_FILE="src/Api/EduShield.Api/appsettings.Secrets.json"
TEMPLATE_FILE="src/Api/EduShield.Api/appsettings.Secrets.template.json"

# Check if secrets file already exists
if [ -f "$SECRETS_FILE" ]; then
    echo "⚠️  Secrets file already exists at $SECRETS_FILE"
    read -p "Do you want to overwrite it? (y/N): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "❌ Setup cancelled"
        exit 1
    fi
fi

# Copy template to secrets file
cp "$TEMPLATE_FILE" "$SECRETS_FILE"
echo "✅ Created secrets file from template"

echo ""
echo "📝 Next steps:"
echo "1. Go to Google Cloud Console: https://console.cloud.google.com/"
echo "2. Create/select a project and enable Google+ API"
echo "3. Go to 'APIs & Services' > 'Credentials'"
echo "4. Create OAuth 2.0 Client ID"
echo "5. Add this redirect URI: http://localhost:5000/api/v1/auth/callback/google"
echo "6. Edit $SECRETS_FILE with your Client ID and Client Secret"
echo ""
echo "🚀 Then run: dotnet run --project src/Api/EduShield.Api"
echo ""