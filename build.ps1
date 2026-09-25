<#
.SYNOPSIS
    Script de build para testes locais do Func4Genexus (GeneXus 18 Legado e U14+).

.DESCRIPTION
    1. Compila a versao LEGADA (GX18 U1 ao U13) -> Func4Genexus_GX18_U1_to_U13.dll
    2. Compila a versao U14+ (GX18 U14 ou superior) -> Func4Genexus_GX18_U14plus.dll
    3. Copia as DLLs e catalogs diretamente para as pastas dentro de dist/ para teste local (sem compactar em zip).
#>

[CmdletBinding()]
param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$RootDir = $PSScriptRoot
if (-not $RootDir) { $RootDir = Get-Location }
$FuncDir = Join-Path $RootDir "Func4Genexus"
$DistDir = Join-Path $RootDir "dist"

Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "       Func4Genexus - Build Local para Testes             " -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan

# 1. Compilação Legada
Write-Host "`n[1/2] Compilando versao LEGADA (GX18 U1 ao U13) em modo $Configuration..." -ForegroundColor Yellow
$legacyCsproj = Join-Path $FuncDir "Func4Genexus.Legacy.csproj"
dotnet build $legacyCsproj -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao da versao Legada!"
    exit $LASTEXITCODE
}
Write-Host "  -> Compilacao Legada concluida com sucesso!" -ForegroundColor Green

# 2. Compilação U14+
Write-Host "`n[2/2] Compilando versao U14+ (GX18 U14 ou superior) em modo $Configuration..." -ForegroundColor Yellow
$u14Csproj = Join-Path $FuncDir "Func4Genexus.U14.csproj"
dotnet build $u14Csproj -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) {
    Write-Error "Falha na compilacao da versao U14+!"
    exit $LASTEXITCODE
}
Write-Host "  -> Compilacao U14+ concluida com sucesso!" -ForegroundColor Green

# 3. Organização na pasta dist (apenas arquivos descompactados para teste local)
Write-Host "`nOrganizando arquivos na pasta dist/ para testes locais..." -ForegroundColor Yellow

$pkgLegacyDir = Join-Path $DistDir "Func4Genexus_GX18_U1_to_U13"
$pkgU14Dir    = Join-Path $DistDir "Func4Genexus_GX18_U14plus"

New-Item -ItemType Directory -Path $pkgLegacyDir -Force | Out-Null
New-Item -ItemType Directory -Path $pkgU14Dir -Force | Out-Null

# Copia arquivos Legado
$legacyDll = Join-Path $FuncDir "bin\Legacy\net472\Func4Genexus_GX18_U1_to_U13.dll"
$legacyCatalog = Join-Path $FuncDir "Func4Genexus_Legacy.catalog"
Copy-Item $legacyDll -Destination (Join-Path $pkgLegacyDir "Func4Genexus_GX18_U1_to_U13.dll") -Force
Copy-Item $legacyCatalog -Destination (Join-Path $pkgLegacyDir "Func4Genexus_Legacy.catalog") -Force

# Copia arquivos U14+
$u14Dll = Join-Path $FuncDir "bin\U14\net472\Func4Genexus_GX18_U14plus.dll"
$u14Catalog = Join-Path $FuncDir "Func4Genexus_U14.catalog"
Copy-Item $u14Dll -Destination (Join-Path $pkgU14Dir "Func4Genexus_GX18_U14plus.dll") -Force
Copy-Item $u14Catalog -Destination (Join-Path $pkgU14Dir "Func4Genexus_U14.catalog") -Force

Write-Host "==========================================================" -ForegroundColor Green
Write-Host "         BUILD CONCLUIDO PARA TESTES LOCAIS!              " -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Green
Write-Host "`nArquivos prontos para copiar para a pasta Packages do seu GeneXus:" -ForegroundColor Cyan
Write-Host "  1. GX18 U1 ao U13 : $pkgLegacyDir" -ForegroundColor White
Write-Host "  2. GX18 U14+      : $pkgU14Dir" -ForegroundColor White

Write-Host "`nComandos sugeridos para copiar para a pasta do GeneXus:" -ForegroundColor Gray
Write-Host "  Copy-Item `"$pkgLegacyDir\*`" -Destination `"C:\GeneXus\Gx18\U13\Packages\`" -Force" -ForegroundColor DarkGray
Write-Host "  Copy-Item `"$pkgU14Dir\*`" -Destination `"C:\GeneXus\Gx18\U14\Packages\`" -Force" -ForegroundColor DarkGray
