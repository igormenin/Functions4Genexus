<#
.SYNOPSIS
    Script interativo de empacotamento, versionamento e deploy da Release para o GitHub.

.DESCRIPTION
    1. Detecta a ultima tag criada no Git local.
    2. Pergunta ao usuario a proxima versao.
    3. Pergunta a descricao do commit e da tag.
    4. Compila ambas as versoes (GX18 U1-U13 e GX18 U14+).
       * Se houver qualquer erro de compilacao, o script interrompe IMEDIATAMENTE,
         exibe os erros e restaura os arquivos sem gerar ZIP nem fazer commit/tag.
    5. Se compilado com 100% de sucesso, atualiza AssemblyAttributes.cs e gera os .zip em dist/.
    6. Faz o git commit, cria a tag e envia para o GitHub.
#>

[CmdletBinding()]
param(
    [string]$NextVersion,
    [string]$Description,
    [switch]$NonInteractive
)

$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
if (-not $RootDir) { $RootDir = Get-Location }
$FuncDir = Join-Path $RootDir "Func4Genexus"
$DistDir = Join-Path $RootDir "dist"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       Func4Genexus - Publicador de Release / Deploy       " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 1. Detectar última Tag Git
# -----------------------------------------------------------------------------
$latestTag = ""
try {
    $allTags = git tag -l --sort=-v:refname
    if ($allTags) {
        $latestTag = ($allTags -split "`r?`n")[0].Trim()
    }
} catch { }

if ($latestTag) {
    Write-Host "`nUltima versao (Tag) encontrada no Git: " -NoNewline -ForegroundColor White
    Write-Host $latestTag -ForegroundColor Green
} else {
    Write-Host "`nNenhuma Tag encontrada no repositorio local." -ForegroundColor Yellow
}

# -----------------------------------------------------------------------------
# 2. Solicitar Próxima Versão
# -----------------------------------------------------------------------------
if (-not $NextVersion) {
    if ($NonInteractive) {
        Write-Host "`n[ERRO] Em modo -NonInteractive, o parametro -NextVersion e obrigatorio." -ForegroundColor Red
        exit 1
    }

    $suggestedVer = ""
    if ($latestTag -match '^v?(\d+)\.(\d+)\.(\d+)$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $patch = [int]$matches[3] + 1
        $suggestedVer = "v$major.$minor.$patch"
    }

    $promptText = if ($suggestedVer) { "Informe a proxima versao (ex: $suggestedVer) [Enter para $suggestedVer]: " } else { "Informe a proxima versao (ex: v0.2.2): " }
    $userInputVer = Read-Host $promptText
    
    if ([string]::IsNullOrWhiteSpace($userInputVer)) {
        if ($suggestedVer) {
            $NextVersion = $suggestedVer
        } else {
            Write-Host "`n[CANCELADO] Versao nao informada. O processo de release foi cancelado." -ForegroundColor Yellow
            exit 1
        }
    } else {
        $NextVersion = $userInputVer.Trim()
    }
}

$tagName = if ($NextVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) { $NextVersion } else { "v$NextVersion" }
$numericVersion = $tagName.TrimStart('v', 'V')
$quadVersion = if ($numericVersion -match '^\d+\.\d+\.\d+$') { "$numericVersion.0" } else { $numericVersion }

Write-Host "  -> Versao definida para a Release : $tagName (Assembly: $quadVersion)" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 3. Solicitar Descrição do Commit e da Tag
# -----------------------------------------------------------------------------
if (-not $Description) {
    if ($NonInteractive) {
        $Description = "Release $tagName"
    } else {
        $promptDesc = Read-Host "Informe a descricao do commit e da Tag [Enter para 'Release $tagName']"
        $Description = if ([string]::IsNullOrWhiteSpace($promptDesc)) { "Release $tagName" } else { $promptDesc.Trim() }
    }
}
Write-Host "  -> Descricao: $Description" -ForegroundColor Cyan

# -----------------------------------------------------------------------------
# 4. Atualizar AssemblyAttributes.cs temporariamente para compilação
# -----------------------------------------------------------------------------
$assemblyAttrPath = Join-Path $FuncDir "AssemblyAttributes.cs"
$originalAssemblyAttrContent = $null

if (Test-Path $assemblyAttrPath) {
    $originalAssemblyAttrContent = Get-Content $assemblyAttrPath -Raw
    $newContent = [System.Text.RegularExpressions.Regex]::Replace($originalAssemblyAttrContent, '\[assembly: AssemblyVersion\("[^"]+"\)', "[assembly: AssemblyVersion(`"$quadVersion`")")
    $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, '\[assembly: AssemblyFileVersion\("[^"]+"\)', "[assembly: AssemblyFileVersion(`"$quadVersion`")")
    $newContent = [System.Text.RegularExpressions.Regex]::Replace($newContent, '\[assembly: AssemblyInformationalVersion\("[^"]+"\)', "[assembly: AssemblyInformationalVersion(`"$numericVersion`")")
    Set-Content -Path $assemblyAttrPath -Value $newContent -Encoding UTF8
}

# -----------------------------------------------------------------------------
# 5. Compilar Projetos com Verificação Rigorosa de Erros
# -----------------------------------------------------------------------------
Write-Host "`n[1/3] Compilando ambas as versoes em modo Release..." -ForegroundColor Yellow

function Abort-Release([string]$errorMessage, [string]$buildOutput) {
    Write-Host "`n==========================================================" -ForegroundColor Red
    Write-Host "                 ERRO NA COMPILACAO!                      " -ForegroundColor Red
    Write-Host "==========================================================" -ForegroundColor Red
    Write-Host $errorMessage -ForegroundColor Red
    
    if ($buildOutput) {
        Write-Host "`nDetalhes do erro do compilador:" -ForegroundColor Yellow
        $errorsOnly = $buildOutput -split "`r?`n" | Where-Object { $_ -match "(error|erro|falha|MSB)" }
        if ($errorsOnly) {
            $errorsOnly | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        } else {
            Write-Host $buildOutput -ForegroundColor DarkGray
        }
    }

    # Restaura arquivo original de versão para não sujar o Git
    if ($originalAssemblyAttrContent -and (Test-Path $assemblyAttrPath)) {
        Set-Content -Path $assemblyAttrPath -Value $originalAssemblyAttrContent -Encoding UTF8
        Write-Host "`n[REVERTIDO] AssemblyAttributes.cs foi restaurado ao estado anterior." -ForegroundColor Gray
    }

    Write-Host "`n[INTERROMPIDO] O processo de release foi cancelado com seguranca." -ForegroundColor Yellow
    Write-Host "Nenhum arquivo zip foi gerado e nenhum commit/tag foi criado." -ForegroundColor Yellow
    exit 1
}

# Compilação 1: Legado (GX18 U1 ao U13)
Write-Host "  -> Compilando GX18 U1 ao U13..." -NoNewline -ForegroundColor White
$legacyCsproj = Join-Path $FuncDir "Func4Genexus.Legacy.csproj"
$buildOutputLegacy = & dotnet build $legacyCsproj -c Release --nologo 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    Write-Host " [FALHOU]" -ForegroundColor Red
    Abort-Release "A compilacao da versao LEGADA (GX18 U1 ao U13) falhou!" $buildOutputLegacy
}
Write-Host " [OK]" -ForegroundColor Green

# Compilação 2: U14+ (GX18 U14 ou superior)
Write-Host "  -> Compilando GX18 U14+..." -NoNewline -ForegroundColor White
$u14Csproj = Join-Path $FuncDir "Func4Genexus.U14.csproj"
$buildOutputU14 = & dotnet build $u14Csproj -c Release --nologo 2>&1 | Out-String
if ($LASTEXITCODE -ne 0) {
    Write-Host " [FALHOU]" -ForegroundColor Red
    Abort-Release "A compilacao da versao U14+ falhou!" $buildOutputU14
}
Write-Host " [OK]" -ForegroundColor Green

Write-Host "  -> Ambas as versoes foram compiladas com 100% de sucesso!" -ForegroundColor Green

# -----------------------------------------------------------------------------
# 6. Gerar Pacotes ZIP em dist/ (Apenas após sucesso absoluto na compilação)
# -----------------------------------------------------------------------------
Write-Host "`n[2/3] Empacotando arquivos de Release (.zip)..." -ForegroundColor Yellow

$pkgLegacyDir = Join-Path $DistDir "Func4Genexus_GX18_U1_to_U13"
$pkgU14Dir    = Join-Path $DistDir "Func4Genexus_GX18_U14plus"

New-Item -ItemType Directory -Path $pkgLegacyDir -Force | Out-Null
New-Item -ItemType Directory -Path $pkgU14Dir -Force | Out-Null

# Copiar arquivos
$legacyDll = Join-Path $FuncDir "bin\Legacy\net472\Func4Genexus_GX18_U1_to_U13.dll"
$legacyCatalog = Join-Path $FuncDir "Func4Genexus_Legacy.catalog"
Copy-Item $legacyDll -Destination (Join-Path $pkgLegacyDir "Func4Genexus_GX18_U1_to_U13.dll") -Force
Copy-Item $legacyCatalog -Destination (Join-Path $pkgLegacyDir "Func4Genexus_Legacy.catalog") -Force

$u14Dll = Join-Path $FuncDir "bin\U14\net472\Func4Genexus_GX18_U14plus.dll"
$u14Catalog = Join-Path $FuncDir "Func4Genexus_U14.catalog"
Copy-Item $u14Dll -Destination (Join-Path $pkgU14Dir "Func4Genexus_GX18_U14plus.dll") -Force
Copy-Item $u14Catalog -Destination (Join-Path $pkgU14Dir "Func4Genexus_U14.catalog") -Force

# Gerar ZIPs
$zipLegacy = Join-Path $DistDir "Func4Genexus_GX18_U1_to_U13.zip"
$zipU14    = Join-Path $DistDir "Func4Genexus_GX18_U14plus.zip"

if (Test-Path $zipLegacy) { Remove-Item $zipLegacy -Force }
if (Test-Path $zipU14)    { Remove-Item $zipU14 -Force }

Compress-Archive -Path "$pkgLegacyDir\*" -DestinationPath $zipLegacy -Force
Compress-Archive -Path "$pkgU14Dir\*" -DestinationPath $zipU14 -Force

Write-Host "  -> Pacote criado: $zipLegacy" -ForegroundColor Green
Write-Host "  -> Pacote criado: $zipU14" -ForegroundColor Green

# -----------------------------------------------------------------------------
# 7. Git Commit, Tag e Deploy para o GitHub
# -----------------------------------------------------------------------------
Write-Host "`n[3/3] Preparando Deploy para o GitHub..." -ForegroundColor Yellow

$confirmPush = $true
if (-not $NonInteractive) {
    $confirm = Read-Host "Deseja realizar o commit, criar a tag $tagName e enviar (push) para o GitHub agora? (S/N) [S]"
    if (-not [string]::IsNullOrWhiteSpace($confirm) -and $confirm.Trim().ToUpper() -ne "S") {
        $confirmPush = $false
    }
}

if ($confirmPush) {
    Write-Host "`nExecutando git add e commit..." -ForegroundColor Yellow
    git add .
    git commit -m "$Description"
    Write-Host "  -> Commit realizado com sucesso!" -ForegroundColor Green

    Write-Host "`nCriando Tag Git $tagName..." -ForegroundColor Yellow
    git tag -a $tagName -m "$Description"
    Write-Host "  -> Tag $tagName criada localmente!" -ForegroundColor Green

    Write-Host "`nEnviando alteracoes e Tag para o GitHub (origin main e $tagName)..." -ForegroundColor Yellow
    git push origin main
    git push origin $tagName

    Write-Host "`n==========================================================" -ForegroundColor Green
    Write-Host "           DEPLOY ENVIADO COM SUCESSO!                    " -ForegroundColor Green
    Write-Host "==========================================================" -ForegroundColor Green
    Write-Host "O GitHub Actions foi iniciado e ira criar a Release oficial" -ForegroundColor Cyan
    Write-Host "com os dois pacotes .zip para download direto em 1 a 2 minutos." -ForegroundColor Cyan
    Write-Host "Acompanhe em: https://github.com/igormenin/Functions4Genexus/actions" -ForegroundColor White
    Write-Host "Pagina da Release: https://github.com/igormenin/Functions4Genexus/releases" -ForegroundColor White
} else {
    Write-Host "`nOperacao de push cancelada pelo usuario." -ForegroundColor Yellow
    Write-Host "Os arquivos de release .zip foram gerados em dist/, mas nenhum commit/push foi feito." -ForegroundColor Gray
}
