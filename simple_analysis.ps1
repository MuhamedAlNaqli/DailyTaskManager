try {
    $excel = New-Object -ComObject Excel.Application
    $excel.Visible = $false
    $filePath = "D:\Obrt\Klijenti\Logosoft\Utroseni sati\2025\Pregled utrošenih sati - MA - 0725.xlsx"
    Write-Host "Opening file: $filePath"
    $workbook = $excel.Workbooks.Open($filePath)
    $worksheet = $workbook.Worksheets.Item(1)

    Write-Host "HEADERS:"
    for ($col = 1; $col -le 4; $col++) {
        $header = $worksheet.Cells.Item(1, $col).Text
        Write-Host "Col $col - $header"
    }

    Write-Host "`nSAMPLE DATA:"
    for ($row = 2; $row -le 5; $row++) {
        Write-Host "Row $row"
        for ($col = 1; $col -le 4; $col++) {
            $value = $worksheet.Cells.Item($row, $col).Text
            Write-Host "  Col $col - $value"
        }
    }

    $workbook.Close($false)
    $excel.Quit()
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}