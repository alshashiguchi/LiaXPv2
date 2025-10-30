# gerar-jwt-key.ps1
$bytes = New-Object byte[] 64
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$rng.GetBytes($bytes)
$key = [Convert]::ToBase64String($bytes)

Write-Host "🔑 JWT Signing Key Gerada (Base64, 64 bytes):" -ForegroundColor Green
Write-Host $key -ForegroundColor Cyan
Write-Host ""
Write-Host "Copie e cole no appsettings.Development.json em JWT:SigningKey" -ForegroundColor Yellow