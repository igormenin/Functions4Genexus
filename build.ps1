<#
.SYNOPSIS
    Script de build, empacotamento e deploy do Func4Genexus para GeneXus 18 (Legado U1-U13 e U14+).

.DESCRIPTION
    1. Compila o projeto Legado (GX18 U1 a U13) -> Func4Genexus_GX18_U1_to_U13.dll
    2. Compila o projeto U14+ (GX18 U14 ou superior) -> Func4Genexus_GX18_U14plus.dll
    3. Organiza os arquivos na pasta dist/ e gera os arquivos .zip prontos para distribuição.
    4. Opcionalmente faz commit, cria Tag e envia para o GitHub.

.EXAMPLE
    .\build.ps1
    Apenas compila as duas versões e gera os zips na pasta dist/

.EXAMPLE
    .\build.ps1 -Tag "v1.0.0" -Push
    Compila, gera os zips, comita, cria a tag v1.0.0 e envia para o GitHub (disparando o Release automático).
#>

[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$CommitMessage,
    [string]$Tag,
    [switch]$Push,
    [string]$GitHubToken
)

$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
if (-not $RootDir) { $RootDir = Get-Location }
$FuncDir = Join-Path $RootDir "Func4Genexus"
$DistDir = Join-Path $RootDir "dist"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       Func4Genexus - Build & Package Automator           " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 1. Compilação
# -----------------------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host "`n[1/3] Compilando versao LEGADA (GX18 U1 ao U13)..." -ForegroundColor Yellow
    $legacyCsproj = Join-Path $FuncDir "Func4Genexus.Legacy.csproj"
    dotnet build $legacyCsproj -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha na compilacao da versao Legada! Verifique os erros acima."
        exit $LASTEXITCODE
    }
    Write-Host "  -> Compilacao Legada concluida com sucesso!" -ForegroundColor Green

    Write-Host "`n[2/3] Compilando versao U14+ (GX18 U14 ou superior)..." -ForegroundColor Yellow
    $u14Csproj = Join-Path $FuncDir "Func4Genexus.U14.csproj"
    dotnet build $u14Csproj -c Release --nologo -v quiet
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Falha na compilacao da versao U14+! Verifique os erros acima."
        exit $LASTEXITCODE
    }
    Write-Host "  -> Compilacao U14+ concluida com sucesso!" -ForegroundColor Green
} else {
    Write-Host "`n[PULANDO] Compilacao pulada por parametro (-SkipBuild)." -ForegroundColor Gray
}

# -----------------------------------------------------------------------------
# 2. Empacotamento na pasta dist/
# -----------------------------------------------------------------------------
Write-Host "`n[3/3] Empacotando arquivos de distribuicao em dist/..." -ForegroundColor Yellow

if (Test-Path $DistDir) {
    Remove-Item $DistDir -Recurse -Force
}
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

# Pacote 1: Legado (U1 ao U13)
$pkgLegacyDir = Join-Path $DistDir "Func4Genexus_GX18_U1_to_U13"
New-Item -ItemType Directory -Path $pkgLegacyDir -Force | Out-Null

$legacyDll = Join-Path $FuncDir "bin\Legacy\net472\Func4Genexus_GX18_U1_to_U13.dll"
$legacyCatalog = Join-Path $FuncDir "Func4Genexus_Legacy.catalog"
Copy-Item $legacyDll -Destination (Join-Path $pkgLegacyDir "Func4Genexus_GX18_U1_to_U13.dll") -Force
Copy-Item $legacyCatalog -Destination (Join-Path $pkgLegacyDir "Func4Genexus_Legacy.catalog") -Force

$zipLegacy = Join-Path $DistDir "Func4Genexus_GX18_U1_to_U13.zip"
Compress-Archive -Path "$pkgLegacyDir\*" -DestinationPath $zipLegacy -Force
Write-Host "  -> Pacote criado: $zipLegacy" -ForegroundColor Green

# Pacote 2: U14+ (U14 ou superior)
$pkgU14Dir = Join-Path $DistDir "Func4Genexus_GX18_U14plus"
New-Item -ItemType Directory -Path $pkgU14Dir -Force | Out-Null

$u14Dll = Join-Path $FuncDir "bin\U14\net472\Func4Genexus_GX18_U14plus.dll"
$u14Catalog = Join-Path $FuncDir "Func4Genexus_U14.catalog"
Copy-Item $u14Dll -Destination (Join-Path $pkgU14Dir "Func4Genexus_GX18_U14plus.dll") -Force
Copy-Item $u14Catalog -Destination (Join-Path $pkgU14Dir "Func4Genexus_U14.catalog") -Force

$zipU14 = Join-Path $DistDir "Func4Genexus_GX18_U14plus.zip"
Compress-Archive -Path "$pkgU14Dir\*" -DestinationPath $zipU14 -Force
Write-Host "  -> Pacote criado: $zipU14" -ForegroundColor Green

# -----------------------------------------------------------------------------
# 3. Git Commit e Tag (Opcional)
# -----------------------------------------------------------------------------
if ($CommitMessage -or $Tag) {
    Write-Host "`nPreparando Git Commit..." -ForegroundColor Yellow
    git add .
    
    $msg = if ($CommitMessage) { $CommitMessage } else { "Release ${Tag}: binarios atualizados para GX18 U1-U13 e U14+" }
    git commit -m $msg
    Write-Host "  -> Commit realizado: $msg" -ForegroundColor Green
}

if ($Tag) {
    Write-Host "`nCriando Tag Git: $Tag..." -ForegroundColor Yellow
    # Se a tag local já existir, avisa
    $existingTag = git tag -l $Tag
    if ($existingTag) {
        Write-Warning "A tag '$Tag' ja existe localmente."
    } else {
        git tag -a $Tag -m "Release $Tag"
        Write-Host "  -> Tag '$Tag' criada!" -ForegroundColor Green
    }

    if ($Push) {
        Write-Host "`nEnviando commit e tags para origin..." -ForegroundColor Yellow
        git push origin main
        git push origin $Tag
        Write-Host "  -> Push concluido com sucesso! O GitHub Actions iniciara a Release." -ForegroundColor Green
    }
} elseif ($Push) {
    Write-Host "`nEnviando alteracoes para origin main..." -ForegroundColor Yellow
    git push origin main
    Write-Host "  -> Push concluido com sucesso!" -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# 4. Upload Direto na API do GitHub (se token fornecido)
# -----------------------------------------------------------------------------
if ($GitHubToken -and $Tag) {
    Write-Host "`nPublicando Release diretamente na API do GitHub..." -ForegroundColor Yellow
    
    $headers = @{
        "Authorization" = "Bearer $GitHubToken"
        "Accept"        = "application/vnd.github+json"
        "X-GitHub-Api-Version" = "2022-11-28"
    }

    $repoUrl = git remote get-url origin
    # Extrai owner e repo de urls https ou ssh
    if ($repoUrl -match "github\.com[/:]([^/]+)/([^/\.]+)") {
        $owner = $matches[1]
        $repo  = $matches[2]
        
        $body = @{
            tag_name         = $Tag
            name             = "Release $Tag"
            body             = "Extensao Func4Genexus para GeneXus 18.`n`n- **GX18 U1 ao U13**: Func4Genexus_GX18_U1_to_U13.zip`n- **GX18 U14 ou superior**: Func4Genexus_GX18_U14plus.zip"
            draft            = $false
            prerelease       = $false
            generate_release_notes = $true
        } | ConvertTo-Json

        try {
            $releaseResponse = Invoke-RestMethod -Uri "https://api.github.com/repos/$owner/$repo/releases" -Method Post -Headers $headers -Body $body -ContentType "application/json"
            $uploadUrlTemplate = $releaseResponse.upload_url
            $cleanUploadUrl = $uploadUrlTemplate -replace '\{.*\}', ''

            foreach ($zipPath in @($zipLegacy, $zipU14)) {
                $fileName = [System.IO.Path]::GetFileName($zipPath)
                $fileBytes = [System.IO.File]::ReadAllBytes($zipPath)
                $assetUrl = "$cleanUploadUrl?name=$fileName"
                
                $assetHeaders = @{
                    "Authorization" = "Bearer $GitHubToken"
                    "Content-Type"  = "application/zip"
                    "X-GitHub-Api-Version" = "2022-11-28"
                }

                Write-Host "  -> Enviando $fileName para a Release..." -ForegroundColor Cyan
                Invoke-RestMethod -Uri $assetUrl -Method Post -Headers $assetHeaders -Body $fileBytes | Out-Null
            }
            Write-Host "  -> Release publicada com sucesso no GitHub!" -ForegroundColor Green
        } catch {
            Write-Warning "Falha ao publicar via API do GitHub: $_"
        }
    }
}

Write-Host "`n==========================================================" -ForegroundColor Green
Write-Host "                PROCESSO CONCLUIDO!                       " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "Arquivos gerados em: $DistDir" -ForegroundColor Cyan
Get-ChildItem $DistDir -Filter "*.zip" | Select-Object Name, @{N="Tamanho(KB)";E={[Math]::Round($_.Length/1KB,1)}} | Format-Table -AutoSize
