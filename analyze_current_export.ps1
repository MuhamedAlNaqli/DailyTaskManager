try {
    $filePath = "D:\Internal Projects\DailyTaskManager\current_export_test.xlsx"

    if (-not (Test-Path $filePath)) {
        Write-Host "Current export file not found: $filePath"
        exit 1
    }

    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $workbook = $excel.Workbooks.Open($filePath)
    $worksheet = $workbook.Worksheets.Item(1)

    Write-Host "=== CURRENT EXPORT FORMAT ANALYSIS ===" -ForegroundColor Green
    Write-Host ""

    # Get worksheet dimensions
    $usedRange = $worksheet.UsedRange
    Write-Host "Worksheet dimensions: $($usedRange.Rows.Count) rows x $($usedRange.Columns.Count) columns" -ForegroundColor Cyan
    Write-Host ""

    # Headers analysis
    Write-Host "HEADERS:" -ForegroundColor Yellow
    for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
        $header = $worksheet.Cells.Item(1, $col).Value2
        $width = $worksheet.Columns.Item($col).ColumnWidth
        $isBold = $worksheet.Cells.Item(1, $col).Font.Bold
        $bgColor = $worksheet.Cells.Item(1, $col).Interior.ColorIndex
        Write-Host "  Col $col`: '$header' | Width: $width | Bold: $isBold | BG: $bgColor" -ForegroundColor White
    }
    Write-Host ""

    # Sample data rows
    Write-Host "SAMPLE DATA ROWS:" -ForegroundColor Yellow
    $dataRows = [Math]::Min(3, $usedRange.Rows.Count - 1)
    for ($row = 2; $row -le ($dataRows + 1); $row++) {
        Write-Host "  Row $row`:" -ForegroundColor White
        for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
            $value = $worksheet.Cells.Item($row, $col).Value2
            $hasWrapText = $worksheet.Cells.Item($row, $col).WrapText
            Write-Host "    Col $col`: '$value' | WrapText: $hasWrapText" -ForegroundColor Gray
        }
    }
    Write-Host ""

    # Check last row (totals)
    $lastRow = $usedRange.Rows.Count
    if ($lastRow -gt 2) {
        Write-Host "TOTALS ROW (Row $lastRow`):" -ForegroundColor Yellow
        for ($col = 1; $col -le $usedRange.Columns.Count; $col++) {
            $value = $worksheet.Cells.Item($lastRow, $col).Value2
            $isBold = $worksheet.Cells.Item($lastRow, $col).Font.Bold
            Write-Host "  Col $col`: '$value' | Bold: $isBold" -ForegroundColor White
        }
        Write-Host ""
    }

    # Border analysis
    Write-Host "BORDER ANALYSIS:" -ForegroundColor Yellow
    $cell = $worksheet.Cells.Item(1, 1)
    Write-Host "  Header borders: Top=$($cell.Borders(8).LineStyle), Bottom=$($cell.Borders(9).LineStyle), Left=$($cell.Borders(7).LineStyle), Right=$($cell.Borders(10).LineStyle)" -ForegroundColor White

    $cell = $worksheet.Cells.Item(2, 1)
    Write-Host "  Data borders: Top=$($cell.Borders(8).LineStyle), Bottom=$($cell.Borders(9).LineStyle), Left=$($cell.Borders(7).LineStyle), Right=$($cell.Borders(10).LineStyle)" -ForegroundColor White
    Write-Host ""

    Write-Host "✅ Analysis complete!" -ForegroundColor Green

    $workbook.Close($false)
    $excel.Quit()
} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
} finally {
    if ($excel) {
        [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
    }
}