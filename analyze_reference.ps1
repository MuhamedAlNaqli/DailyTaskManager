# PowerShell script to analyze reference Excel file format
param(
    [string]$ReferencePath = "D:\Obrt\Klijenti\Logosoft\Utroseni sati\2025\Pregled utrošenih sati - MA - 0725.xlsx"
)

Write-Host "🔍 Analyzing Reference Excel File Format" -ForegroundColor Green

if (-not (Test-Path $ReferencePath)) {
    Write-Host "❌ Reference file not found: $ReferencePath" -ForegroundColor Red
    exit 1
}

try {
    # Create Excel COM object
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $excel.DisplayAlerts = $false

    # Open the reference workbook
    $workbook = $excel.Workbooks.Open($ReferencePath)
    $worksheet = $workbook.Worksheets.Item(1)

    Write-Host "`n📊 REFERENCE FILE ANALYSIS:" -ForegroundColor Yellow
    Write-Host "================================" -ForegroundColor Yellow

    # Get basic info
    $usedRange = $worksheet.UsedRange
    Write-Host "📏 Used Range: $($usedRange.Address)" -ForegroundColor Cyan
    Write-Host "📐 Rows: $($usedRange.Rows.Count), Columns: $($usedRange.Columns.Count)" -ForegroundColor Cyan

    # Analyze headers (assuming they're in row 1)
    Write-Host "`n📋 COLUMN HEADERS:" -ForegroundColor Yellow
    for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
        $header = $worksheet.Cells.Item(1, $col).Text
        $width = $worksheet.Columns.Item($col).ColumnWidth
        Write-Host "  Column $col : '$header' (Width: $width)" -ForegroundColor White
    }

    # Sample data rows (first 5 data rows)
    Write-Host "`n📈 SAMPLE DATA ROWS:" -ForegroundColor Yellow
    $maxRows = [Math]::Min(6, $usedRange.Rows.Count) # Header + 5 data rows
    for ($row = 2; $row -le $maxRows; $row++) {
        Write-Host "  Row $row :" -ForegroundColor White
        for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
            $value = $worksheet.Cells.Item($row, $col).Text
            $border = $worksheet.Cells.Item($row, $col).Borders.LineStyle
            Write-Host "    Col $col : '$value' (Border: $border)" -ForegroundColor Gray
        }
    }

    # Check for totals row (usually at the bottom)
    Write-Host "`n🧮 TOTALS/SUMMARY ROW:" -ForegroundColor Yellow
    $lastRow = $usedRange.Rows.Count
    if ($lastRow -gt 2) {
        Write-Host "  Last Row ($lastRow):" -ForegroundColor White
        for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
            $value = $worksheet.Cells.Item($lastRow, $col).Text
            $isBold = $worksheet.Cells.Item($lastRow, $col).Font.Bold
            Write-Host "    Col $col : '$value' (Bold: $isBold)" -ForegroundColor Gray
        }
    }

    # Check formatting
    Write-Host "`n🎨 FORMATTING ANALYSIS:" -ForegroundColor Yellow
    $headerCell = $worksheet.Cells.Item(1, 1)
    Write-Host "  Header formatting:" -ForegroundColor White
    Write-Host "    Font: $($headerCell.Font.Name), Size: $($headerCell.Font.Size)" -ForegroundColor Gray
    Write-Host "    Bold: $($headerCell.Font.Bold)" -ForegroundColor Gray
    Write-Host "    Background: $($headerCell.Interior.ColorIndex)" -ForegroundColor Gray
    Write-Host "    Border style: $($headerCell.Borders.LineStyle)" -ForegroundColor Gray

    # Date format analysis
    if ($usedRange.Columns.Count -ge 3) {
        $dateCell = $worksheet.Cells.Item(2, 3)
        Write-Host "`n📅 DATE FORMAT:" -ForegroundColor Yellow
        Write-Host "  Sample date: '$($dateCell.Text)'" -ForegroundColor White
        Write-Host "  Number format: '$($dateCell.NumberFormat)'" -ForegroundColor Gray
    }

    Write-Host "`n✅ Analysis complete!" -ForegroundColor Green

} catch {
    Write-Host "❌ Error analyzing file: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    # Clean up
    if ($workbook) { $workbook.Close($false) }
    if ($excel) { $excel.Quit() }
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
}