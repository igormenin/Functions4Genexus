<#
.SYNOPSIS
    Script interativo de empacotamento, versionamento e deploy da Release para o GitHub.

.DESCRIPTION
    1. Detecta a ultima tag criada no Git local.
    2. Pergunta ao usuario a proxima versao.
    3. Pergunta a descricao do commit e da tag.
    4. Atualiza AssemblyAttributes.cs com a nova versao na DLL.
    5. Compila ambos os projetos (GX18 U1-U13 e GX18 U14+).
    6. Compacta as DLLs e catalogs em arquivos .zip dentro de dist/.
    7. Faz git commit, cria a tag e envia para o GitHub (acionando o GitHub Actions).
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
    # Busca a tag mais recente ordenada por versão
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
        Write-Error "Em modo -NonInteractive, o parametro -NextVersion e obrigatorio."
        exit 1
    }

    # Sugestão básica de incremento de patch se possível
    $suggestedVer = ""
    if ($latestTag -match '^v?(\d+)\.(\d+)\.(\d+)$') {
        $major = [int]$matches[1]
        $minor = [int]$matches[2]
        $patch = [int]$matches[3] + 1
        $suggestedVer = "v$major.$minor.$patch"
    }

    $promptText = if ($suggestedVer) { "Informe a proxima versao (ex: $suggestedVer) [Enter para aceitar $suggestedVer]: " } else { "Informe a proxima versao (ex: v0.2.1): " }
    $userInputVer = Read-Host $promptText
    
    if ([string]::IsNullOrWhiteSpace($userInputVer)) {
        if ($suggestedVer) {
            $NextVersion = $suggestedVer
        } else {
            Write-Error "Versao nao informada. Operacao cancelada."
            exit 1
        }
    } else {
        $NextVersion = $userInputVer.Trim()
    }
}

# Padroniza Tag (com 'v') e Versão numérica (sem 'v')
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
# 4. Atualizar AssemblyAttributes.cs com a nova versão
# -----------------------------------------------------------------------------
Write-Host "`n[1/4] Atualizando versao no AssemblyAttributes.cs..." -ForegroundColor Yellow
$assemblyAttrPath = Join-Path $FuncDir "AssemblyAttributes.cs"
if (Test-Path $assemblyAttrPath) {
    $content = Get-Content $assemblyAttrPath -Raw
    $content = [System.Text.RegularExpressions.Regex]::Replace($content, '\[assembly: AssemblyVersion\("[^"]+"\)', "[assembly: AssemblyVersion(`"$quadVersion`")")
    $content = [System.Text.RegularExpressions.Regex]::Replace($content, '\[assembly: AssemblyFileVersion\("[^"]+"\)', "[assembly: AssemblyFileVersion(`"$quadVersion`")")
    $content = [System.Text.RegularExpressions.Regex]::Replace($content, '\[assembly: AssemblyInformationalVersion\("[^"]+"\)', "[assembly: AssemblyInformationalVersion(`"$numericVersion`")")
    Set-Content -Path $assemblyAttrPath -Value $content -Encoding UTF8
    Write-Host "  -> AssemblyAttributes.cs atualizado com sucesso!" -ForegroundColor Green
}

# -----------------------------------------------------------------------------
# 5. Compilar Projetos
# -----------------------------------------------------------------------------
Write-Host "`n[2/4] Compilando ambas as versoes em modo Release..." -ForegroundColor Yellow

# Legado
$legacyCsproj = Join-Path $FuncDir "Func4Genexus.Legacy.csproj"
dotnet build $legacyCsproj -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao da versao Legada!"
    exit $LASTEXITCODE
}
Write-Host "  -> GX18 U1 ao U13 compilado com sucesso!" -ForegroundColor Green

# U14+
$u14Csproj = Join-Path $FuncDir "Func4Genexus.U14.csproj"
dotnet build $u14Csproj -c Release --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao da versao U14+!"
    exit $LASTEXITCODE
}
Write-Host "  -> GX18 U14+ compilado com sucesso!" -ForegroundColor Green

# -----------------------------------------------------------------------------
# 6. Gerar Pacotes ZIP em dist/
# -----------------------------------------------------------------------------
Write-Host "`n[3/4] Empacotando arquivos de Release (.zip)..." -ForegroundColor Yellow

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

Write-Host "  -> Gerado: $zipLegacy" -ForegroundColor Green
Write-Host "  -> Gerado: $zipU14" -ForegroundColor Green

# -----------------------------------------------------------------------------
# 7. Git Commit, Tag e Deploy para o GitHub
# -----------------------------------------------------------------------------
Write-Host "`n[4/4] Preparando Deploy para o GitHub..." -ForegroundColor Yellow

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
