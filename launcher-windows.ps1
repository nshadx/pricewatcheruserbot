# ================= НАСТРОЙКИ =================
$repoUrl    = "https://github.com/nshadx/pricewatcheruserbot"
$repoName   = "pricewatcheruserbot"
$runnable   = "pricewatcheruserbot"
$data       = "data"
$buildCfg   = "Release"

Write-Host "!!!TURN OFF VPN!!!"

# ================= ПРОВЕРКИ ==================
foreach ($cmd in @("git", "dotnet")) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Error "$cmd not found in PATH"
        exit 1
    }
}

# ================= GIT =======================
if (-not (Test-Path $repoName)) {
    Write-Host "Cloning repository..." -ForegroundColor Cyan
    git clone $repoUrl
    if ($LASTEXITCODE -ne 0) { exit 1 }
} else {
    Write-Host "Repository found, performing update..." -ForegroundColor Cyan
    Set-Location $repoName
    git pull --rebase
    if ($LASTEXITCODE -ne 0) { exit 1 }
    Set-Location ..
}

# ================= BUILD =====================
Write-Host "Publishing..." -ForegroundColor Cyan

dotnet publish `
    -c $buildCfg `
    --self-contained `
    -o "publish"

if ($LASTEXITCODE -ne 0) {
    Write-Error "Publishing failed"
    exit 1
}

Set-Location "publish"

# ================= .ENV ======================
if (-not (Test-Path ".env")) {

    Write-Host ".env not found" -ForegroundColor Yellow

    $TelegramPhoneNumber = Read-Host "Enter telegram phone number (with contry code e.g. +7)"
    $TelegramApiId       = Read-Host "Enter telegram api_id"
    $TelegramApiHash     = Read-Host "Enter telegram api_hash"
    $TelegramPassword    = Read-Host "Enter telegram account password"
    
    $OzonPhoneNumber     = Read-Host "Enter Ozon phone number (without contry code)"

    @"
StorageDirectoryPath="$data/"
TelegramSessionFilePath="$data/telegram-session"
BrowserSessionFilePath="$data/browser-session"
DbConnectionString="Data Source=app.db"

TelegramPhoneNumber=$TelegramPhoneNumber
TelegramApiId=$TelegramApiId
TelegramApiHash=$TelegramApiHash
TelegramPassword=$TelegramPassword
OzonPhoneNumber=$OzonPhoneNumber
"@ | Out-File ".env" -Encoding UTF8 -Force

    Write-Host ".env created" -ForegroundColor Green
}
else {
    Write-Host ".env already exists, skipping..." -ForegroundColor Green
}

if (-not (Test-Path "./$data")) {
    New-Item -ItemType Directory -Path "./$data" -Force | Out-Null
}

# ================= PLAYWRIGHT ================

$playwright = Get-ChildItem -Recurse -Filter "playwright.ps1" | Select-Object -First 1
if (-not $playwright) {
    Write-Error "playwright.ps1 not found"
    exit 1
}

Write-Host "Playwright install..." -ForegroundColor Cyan
& $playwright.FullName install
if ($LASTEXITCODE -ne 0) { exit 1 }

# ================= RUN =======================
$exe = Get-ChildItem -Recurse -Filter "$runnable.exe" | Select-Object -First 1

Write-Host "Starting (interactive)..." -ForegroundColor Green
Start-Process `
    -FilePath $exe.FullName`
    -WorkingDirectory (Get-Location) `
    -NoNewWindow `
    -Wait
