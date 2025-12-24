param (
    [string]$RepoUrl = "https://github.com/nshadx/pricewatcheruserbot",

    [string]$ImageName = "pricewatcheruserbot",
    [string]$ContainerName = "pricewatcheruserbot"
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

docker run -d `
    --name $ContainerName `
    -v ${RepoPath}:/data `
    $ImageName

