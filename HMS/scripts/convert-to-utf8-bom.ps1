# Script PowerShell para converter arquivos C# para UTF-8 with BOM
# Salve como: convert-to-utf8-bom.ps1

param(
    [string]$Path = ".",
    [string[]]$Extensions = @("*.cs", "*.json", "*.xml", "*.csproj")
)

Write-Host "Convertendo arquivos para UTF-8 with BOM..." -ForegroundColor Green

$files = Get-ChildItem -Path $Path -Recurse -Include $Extensions | Where-Object { !$_.PSIsContainer }

$totalFiles = $files.Count
$convertedFiles = 0

foreach ($file in $files) {
    try {
        # Ler o conteúdo do arquivo
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        
        # Salvar com UTF-8 BOM
        $utf8WithBom = New-Object System.Text.UTF8Encoding $true
        [System.IO.File]::WriteAllText($file.FullName, $content, $utf8WithBom)
        
        $convertedFiles++
        Write-Host "Convertido: $($file.FullName)" -ForegroundColor Gray
    }
    catch {
        Write-Host "Erro ao converter: $($file.FullName) - $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`nConversão concluída!" -ForegroundColor Green
Write-Host "Total de arquivos: $totalFiles" -ForegroundColor Yellow
Write-Host "Arquivos convertidos: $convertedFiles" -ForegroundColor Yellow

# Pausa para ver o resultado
Read-Host "Pressione Enter para continuar..."