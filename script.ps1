param (
    [string]$RepoUrl = "https://github.com/nshadx/pricewatcheruserbot",

    [string]$ImageName = "pricewatcheruserbot",
    [string]$ContainerName = "pricewatcheruserbot",

    [string]$DataVolume = "pricewatcher_data"
)

# === Вычисляем имя репозитория и путь ===
$RepoName = [System.IO.Path]::GetFileNameWithoutExtension($RepoUrl)
$RepoPath = Join-Path "C:\Users\Public\Desktop\" $RepoName

function Read-Secret($prompt) {
    $secure = Read-Host $prompt -AsSecureString
    return [Runtime.InteropServices.Marshal]::PtrToStringAuto(
        [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    )
}

Write-Host "==> Репозиторий: $RepoName"
Write-Host "==> Путь: $RepoPath"
Write-Host ""

Write-Host "==> Проверка репозитория..."

if (-Not (Test-Path $RepoPath)) {
    git clone $RepoUrl $RepoPath
} else {
    Set-Location $RepoPath
    git pull --rebase
}

Set-Location $RepoPath

$oldImageId = docker images -q $ImageName 2>$null

Write-Host "==> Сборка Docker-образа..."
docker build -f pricewatcheruserbot/Dockerfile -t $ImageName .

$newImageId = docker images -q $ImageName
$imageChanged = $oldImageId -ne $newImageId

$containerExists = docker ps -a --format "{{.Names}}" | Where-Object { $_ -eq $ContainerName }

if ($containerExists -and $imageChanged) {
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
