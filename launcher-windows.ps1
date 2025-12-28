# ================= НАСТРОЙКИ =================
$repoUrl     = "https://github.com/nshadx/pricewatcheruserbot"
$repoName    = "pricewatcheruserbot"
$projectName = "pricewatcheruserbot"
$buildCfg    = "Release"

Write-Host "!!!TURN OFF VPN!!!" -ForegroundColor Yellow

Start-Sleep -Seconds 2

# ================= ПРОВЕРКИ ==================

if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
        Write-Host "'dotnet' command not found. Download this from: https://dotnet.microsoft.com/en-us/download/dotnet/10.0" --ForegroundColor Yellow
        exit 1
}

if (-not (Get-Command "git" -ErrorAction SilentlyContinue)) {
        Write-Host "'git' command not found. Download this from: https://git-scm.com/install" --ForegroundColor Yellow
        exit 1
}

# ================= GIT =======================
if (-not (Test-Path $repoName)) {
    Write-Host "Cloning repository..." -ForegroundColor Cyan
    git clone $repoUrl
    if ($LASTEXITCODE -ne 0) { exit 1 }
    Set-Location $repoName
} else {
    Write-Host "Repository found, performing update..." -ForegroundColor Cyan
    Set-Location $repoName
    git pull --rebase
    if ($LASTEXITCODE -ne 0) { exit 1 }
    Set-Location ..
}

# ================= BUILD =====================
Write-Host "Publishing..." -ForegroundColor Cyan

$project = Join-Path $projectName "$projectName.csproj";

dotnet publish "$project" `
    --sc `
    -v q `
    -c $buildCfg `
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
TelegramSessionFilePath="telegram-session"
BrowserSessionFilePath="browser-session"
DbConnectionString="Data Source=app.db"
UserAgentFetchUrl="https://raw.githubusercontent.com/HyperBeats/User-Agent-List/main/useragents-desktop.txt"

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

# ================= PLAYWRIGHT ================

$playwright = Get-ChildItem -Recurse -Filter "playwright.ps1" | Select-Object -First 1
if (-not $playwright) {
    Write-Error "playwright.ps1 not found"
    exit 1
}

Write-Host "Playwright install..." -ForegroundColor Cyan
pwsh $playwright.FullName install
if ($LASTEXITCODE -ne 0) { exit 1 }

# ================= RUN =======================
$exe = Get-ChildItem -Recurse -Filter "$projectName.exe" | Select-Object -First 1

Write-Host "Starting (interactive)..." -ForegroundColor Green
Start-Process `
    -FilePath $exe.FullName`
    -WorkingDirectory (Get-Location) `
    -NoNewWindow `
    -Wait
