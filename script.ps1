param (
    [string]$RepoUrl = "https://github.com/nshadx/pricewatcheruserbot",

    [string]$ImageName = "pricewatcheruserbot",
    [string]$ContainerName = "pricewatcheruserbot",

    [string]$DataVolume = "pricewatcher_data"
)

# ===== Проверки =====

function Require-Command($name, $hint) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Write-Host "❌ $name не найден" -ForegroundColor Red
        Write-Host "👉 $hint"
        exit 1
    }
}

function Require-DockerRunning {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Docker daemon не запущен" -ForegroundColor Red
        Write-Host "👉 Запусти Docker Desktop"
        exit 1
    }
}

Require-Command git "https://git-scm.com/downloads"
Require-Command docker "https://docs.docker.com/get-docker/"
Require-DockerRunning

# ===== Repo path =====

$RepoName = [System.IO.Path]::GetFileNameWithoutExtension($RepoUrl)
$RepoPath = Join-Path $HOME $RepoName

function Read-Secret($prompt) {
    $secure = Read-Host $prompt -AsSecureString
    [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    )
}

# ===== Git =====

if (-not (Test-Path $RepoPath)) {
    git clone $RepoUrl $RepoPath
} else {
    Set-Location $RepoPath
    git pull --rebase
}

Set-Location $RepoPath

# ===== Docker build =====

$oldImageId = docker images -q $ImageName 2>$null
docker build -f pricewatcheruserbot/Dockerfile -t $ImageName .
$newImageId = docker images -q $ImageName
$imageChanged = $oldImageId -ne $newImageId

# ===== Container =====

$containerExists = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $ContainerName }

if ($containerExists -and $imageChanged) {
    docker stop $ContainerName | Out-Null
    docker rm $ContainerName | Out-Null
}

$sessionExists = docker run --rm `
    -v ${DataVolume}:/data `
    busybox `
    sh -c "test -f /data/session" 2>$null

if ($LASTEXITCODE -ne 0) {

    Write-Host ""
    Write-Host "==> Файл session в volume не найден. Введите данные для бота:"

    $apiId = Read-Host "Enter ApiId"
    $apiHash = Read-Host "Enter ApiHash"
    $phoneNumber = Read-Host "Enter phone number with country code (+7)"
    $password = Read-Secret "Enter 2FA password (hidden)"

    Write-Host ""
    Write-Host "==> Контейнер запускается В ЭТОМ ОКНЕ"
    Write-Host "==> После ввода кода Telegram нажми:"
    Write-Host "==> Ctrl+P, затем Ctrl+Q (НЕ Ctrl+C!)"
    Write-Host ""

    docker run -it `
        --name $ContainerName `
        --restart unless-stopped `
        -e BotCredentials__ApiId="$apiId" `
        -e BotCredentials__ApiHash="$apiHash" `
        -e BotCredentials__PhoneNumber="$phoneNumber" `
        -e BotCredentials__Password="$password" `
        -v ${DataVolume}:/data `
        $ImageName

} else {
    Write-Host "==> Файл session найден в volume. Подключаюсь..."
    docker run -it `
        --name $ContainerName `
        -v ${DataVolume}:/data `
        $ImageName
}

