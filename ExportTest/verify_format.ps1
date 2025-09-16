try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $workbook = $excel.Workbooks.Open("C:\Users\muham\Desktop\test_export_new_format.xlsx")
    $worksheet = $workbook.Worksheets[1]

    Write-Host "=== VERIFYING NEW EXCEL EXPORT FORMAT ===" -ForegroundColor Green
    Write-Host ""

    Write-Host "Worksheet Name:" $worksheet.Name
    Write-Host "Used Range:" $worksheet.UsedRange.Address
    Write-Host ""

    # Get headers from first row
    Write-Host "=== HEADERS ===" -ForegroundColor Yellow
    $lastColumn = $worksheet.UsedRange.Columns.Count
    for ($col = 1; $col -le $lastColumn; $col++) {
        $header = $worksheet.Cells.Item(1, $col).Value2
        Write-Host "Column $col : '$header'"
    }
    Write-Host ""

    # Get sample data rows
    Write-Host "=== SAMPLE DATA ===" -ForegroundColor Yellow
    $lastRow = [Math]::Min(5, $worksheet.UsedRange.Rows.Count)
    for ($row = 2; $row -le $lastRow; $row++) {
        Write-Host "Row $row"
        for ($col = 1; $col -le $lastColumn; $col++) {
            $cellValue = $worksheet.Cells.Item($row, $col).Value2
            $columnName = switch ($col) {
                1 { "Red br" }
                2 { "Kratak opis aktivnosti" }
                3 { "Datum" }
                4 { "Broj sati" }
                default { "Column $col" }
            }
            Write-Host "  $columnName : $cellValue"
        }
        Write-Host ""
    }

    # Check totals row
    $totalRowIndex = $worksheet.UsedRange.Rows.Count
    Write-Host "=== TOTALS ROW (Row $totalRowIndex) ===" -ForegroundColor Yellow
    for ($col = 1; $col -le $lastColumn; $col++) {
        $cellValue = $worksheet.Cells.Item($totalRowIndex, $col).Value2
        if ($cellValue -ne $null -and $cellValue -ne "") {
            Write-Host "Column $col`: $cellValue"
        }
    }

    Write-Host ""
    Write-Host "=== FORMAT VERIFICATION COMPLETE ===" -ForegroundColor Green

    $workbook.Close($false)
    $excel.Quit()
    [System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
} catch {
    Write-Host "Error reading Excel file:" $_.Exception.Message -ForegroundColor Red
}