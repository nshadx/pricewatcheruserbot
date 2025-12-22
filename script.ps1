param (
    [string]$RepoUrl = "https://github.com/nshadx/pricewatcheruserbot",

    [string]$ImageName = "pricewatcheruserbot",
    [string]$ContainerName = "pricewatcheruserbot",

    [string]$DataVolume = "pricewatcher_data"
)

# === Проверки зависимостей ===

function Require-Command($name, $installHint) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Write-Host ""
        Write-Host "❌ $name не найден." -ForegroundColor Red
        Write-Host "👉 $installHint"
        exit 1
    }
}

function Require-DockerRunning {
    docker info *> $null
    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "❌ Docker установлен, но daemon не запущен." -ForegroundColor Red
        Write-Host "👉 Запусти Docker Desktop и попробуй снова."
        exit 1
    }
}

Require-Command git "Установи Git: https://git-scm.com/downloads"
Require-Command docker "Установи Docker: https://docs.docker.com/get-docker/"
Require-DockerRunning

# === Repo path из $HOME ===

$RepoName = [System.IO.Path]::GetFileNameWithoutExtension($RepoUrl)
$RepoPath = Join-Path $HOME $RepoName

function Read-Secret($prompt) {
    $secure = Read-Host $prompt -AsSecureString
    return [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    )
}

Write-Host "==> Репозиторий: $RepoName"
Write-Host "==> Путь: $RepoPath"
Write-Host ""

# === Git ===

if (-Not (Test-Path $RepoPath)) {
    Write-Host "==> Клонирую репозиторий..."
    git clone $RepoUrl $RepoPath
} else {
    Write-Host "==> Обновляю репозиторий (pull --rebase)..."
    Set-Location $RepoPath
    git pull --rebase
}

Set-Location $RepoPath

# === Docker build ===

$oldImageId = docker images -q $ImageName 2>$null

Write-Host "==> Сборка Docker-образа..."
docker build -f pricewatcheruserbot/Dockerfile -t $ImageName .

$newImageId = docker images -q $ImageName
$imageChanged = $oldImageId -ne $newImageId

# === Контейнер ===

$containerExists = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $ContainerName }

if ($containerExists -and $imageChanged) {
    Write-Host "==> Образ изменился, пересоздаю контейнер..."
    docker stop $ContainerName | Out-Null
    docker rm $ContainerName | Out-Null
}

if (-Not $containerExists -or $imageChanged) {

    Write-Host ""
    Write-Host "==> Введите данные для бота (не сохраняются):"

    $apiId = Read-Host "Enter ApiId"
    $apiHash = Read-Host "Enter ApiHash"
    $phoneNumber = Read-Host "Enter phone number"
    $password = Read-Secret "Enter 2FA password (hidden)"

    Write-Host ""
    Write-Host "==> Запуск интерактивного long-running контейнера..."

    docker run -it -d `
        --name $ContainerName `
        --restart unless-stopped `
        -e BotCredentials__ApiId="$apiId" `
        -e BotCredentials__ApiHash="$apiHash" `
        -e BotCredentials__PhoneNumber="$phoneNumber" `
        -e BotCredentials__Password="$password" `
        -v ${DataVolume}:/data `
        $ImageName

    Write-Host ""
    Write-Host "==> Подключиться к контейнеру:"
    Write-Host "   docker attach $ContainerName"
    Write-Host "   Detach: Ctrl+P, Ctrl+Q"

} else {
    Write-Host "==> Контейнер уже существует. Запускаю..."
    docker start $ContainerName
}

Write-Host ""
Write-Host "✅ Всё готово. Бот работает."
