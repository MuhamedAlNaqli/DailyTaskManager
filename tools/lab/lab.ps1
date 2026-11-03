<#
  A local test bench for the employee side: the company API on this PC with a temporary database, sample people and sample
  timesheets, and the desktop app in employee mode signed in with YOUR Windows account.

    .\tools\lab\lab.ps1 -Role employee     # you are Amra: a returned month, a month waiting for approval, an overdue month
    .\tools\lab\lab.ps1 -Role approver     # you are Benjamin, the team lead: two timesheets wait for you
    .\tools\lab\lab.ps1 -Role finance      # you are the lawyer: nobody above you (you approve your own month) and a team
    .\tools\lab\lab.ps1 -Stop              # stops everything and removes the test data

  Nothing here touches your real database. The lab database is a file in %TEMP%\timesheet-lab. While the lab runs, the desktop
  app of this folder starts in employee mode; -Stop puts it back (the installed app is not changed, but it reads the same
  mode file, so do not open it while the lab runs).
#>
param(
    [ValidateSet('employee', 'approver', 'finance')] [string]$Role = 'employee',
    [switch]$Stop,
    [switch]$NoApp
)

$ErrorActionPreference = 'Stop'
$repo = Resolve-Path (Join-Path $PSScriptRoot '..\..')
$lab = Join-Path $env:TEMP 'timesheet-lab'
$modeFile = Join-Path $env:LOCALAPPDATA 'DailyTaskManager\mode.txt'
$stateFile = Join-Path $env:LOCALAPPDATA 'DailyTaskManager\reminder-state.json'
$apiDll = Join-Path $repo 'src\Company.Api\bin\Debug\net8.0\Company.Api.dll'
$appExe = Join-Path $repo 'src\DailyTaskManager.Avalonia\bin\Debug\net8.0\win-x64\DailyTaskManager.Avalonia.exe'
$me = "$env:COMPUTERNAME\$env:USERNAME".ToLowerInvariant()

$marker = Join-Path $lab 'active'
$modeBackup = Join-Path $lab 'mode.backup.txt'

function Stop-Lab {
    # Only the servers this lab started (the ones that run its own build of the API with its own database), nothing else
    Get-CimInstance Win32_Process | Where-Object { $_.CommandLine -like "*$apiDll*" } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
    Get-Process DailyTaskManager.Avalonia -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $appExe } | Stop-Process -Force

    # Put back what the lab replaced: the choice of mode and the reminder counters, if the lab had been started
    if (Test-Path $marker) {
        if (Test-Path $modeBackup) { Copy-Item $modeBackup $modeFile -Force; Remove-Item $modeBackup }
        elseif (Test-Path $modeFile) { Remove-Item $modeFile }
        Remove-Item $marker
    }
    $backup = Join-Path $lab 'reminder-state.backup.json'
    if (Test-Path $backup) { Copy-Item $backup $stateFile -Force; Remove-Item $backup }
    Get-ChildItem $lab -Filter 'lab.db*' -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    Write-Host 'The lab is stopped and its data is removed.'
}

if ($Stop) { Stop-Lab; return }

Stop-Lab
New-Item -ItemType Directory -Force $lab | Out-Null
if (Test-Path $stateFile) { Copy-Item $stateFile (Join-Path $lab 'reminder-state.backup.json') -Force }
if (Test-Path $modeFile) { Copy-Item $modeFile $modeBackup -Force }
Set-Content $marker (Get-Date).ToString('s')

# The months of the sample follow today: two months ago was returned, last month waits for approval, this month is open
$first = Get-Date -Year (Get-Date).Year -Month (Get-Date).Month -Day 1
$two = $first.AddMonths(-2)
$one = $first.AddMonths(-1)
$saturday = $one; while ($saturday.DayOfWeek -ne 'Saturday') { $saturday = $saturday.AddDays(1) }

Write-Host 'Building...'
dotnet build (Join-Path $repo 'src\Company.Api') --nologo -v q | Out-Null
dotnet build (Join-Path $repo 'src\DailyTaskManager.Avalonia') --nologo -v q | Out-Null

# Two copies of the API on one database file: 5090 signs in with your Windows account (the app uses it), 5091 accepts a header
# so this script can act as the other people.
$common = @{
    Database__Provider = 'sqlite'
    ConnectionStrings__Default = "Data Source=$(Join-Path $lab 'lab.db')"
    Bootstrap__AdminAccounts__0 = 'lab\admin'
    Reminders__StartFrom = $two.ToString('yyyy-MM')
}
# The finance role of the lab needs the part of the product that is switched off by default
if ($Role -eq 'finance') { $common.Features__FinanceAndLegal = 'true' }
function Start-Api($port, $mode, $environment) {
    foreach ($k in $common.Keys) { Set-Item "Env:$k" $common[$k] }
    $env:Authentication__Mode = $mode
    $env:ASPNETCORE_ENVIRONMENT = $environment
    Start-Process dotnet -ArgumentList "`"$apiDll`" --urls http://localhost:$port" -WorkingDirectory (Split-Path $apiDll) `
        -RedirectStandardOutput (Join-Path $lab "api$port.log") -RedirectStandardError (Join-Path $lab "api$port.err") -WindowStyle Hidden
}
# Waits until the server answers (any answer, even a refusal, means it is up)
function Wait-Api($port) {
    for ($i = 0; $i -lt 60; $i++) {
        try { Invoke-WebRequest "http://localhost:$port/api/me" -UseBasicParsing -TimeoutSec 2 | Out-Null; return }
        catch { if ($_.Exception.Response) { return }; Start-Sleep 1 }
    }
    throw "The server on port $port did not start. See $lab\api$port.err"
}
Start-Api 5091 'Test' 'Development'
Wait-Api 5091
Start-Api 5090 'Windows' 'Production'
Wait-Api 5090

$T = 'http://localhost:5091'
function Call($method, $path, $user, $body) {
    $splat = @{ Method = $method; Uri = "$T$path"; Headers = @{ 'X-Test-User' = $user; 'X-Requested-With' = 'CompanyTimesheet' } }
    if ($body) { $splat.ContentType = 'application/json'; $splat.Body = $body }
    Invoke-RestMethod @splat | Out-Null
}
function Link($code, $account) { Call Post "/api/admin/employees/$code/accounts" 'lab\admin' (@{ account = $account } | ConvertTo-Json) }

# People: Amra and Dino report to Benjamin (team lead), Benjamin to Emir (lawyer, final approver and finance)
Call Post '/api/admin/employees/import' 'lab\admin' '{"csv":"Br.,Ime i prezime\nZ-001,Amra Adzamija\nZ-002,Benjamin Basic\nZ-003,Emir Ferhatbegovic\nZ-004,Dino Kadrovic\n"}'
$accounts = @{ 'Z-001' = 'lab\amra'; 'Z-002' = 'lab\benjamin'; 'Z-003' = 'lab\emir'; 'Z-004' = 'lab\dino' }
$mine = @{ employee = 'Z-001'; approver = 'Z-002'; finance = 'Z-003' }[$Role]
$accounts[$mine] = $me          # your Windows account takes the place of that person
foreach ($code in $accounts.Keys) { Link $code $accounts[$code] }
Call Put '/api/admin/employees/Z-001' 'lab\admin' '{"supervisorCode":"Z-002"}'
Call Put '/api/admin/employees/Z-004' 'lab\admin' '{"supervisorCode":"Z-002"}'
Call Put '/api/admin/employees/Z-002' 'lab\admin' '{"supervisorCode":"Z-003"}'
Call Put '/api/admin/employees/Z-003' 'lab\admin' '{"isFinalApprover":true,"isFinance":true}'
Call Post '/api/admin/holidays' 'lab\admin' (@{ date = $one.ToString('yyyy-MM-dd'); name = 'Praznik (probni)' } | ConvertTo-Json)

function Who($code) { $accounts[$code] }
function Month($code, $year, $month) { Call Post "/api/timesheet/$year/$month/fill-standard-days" (Who $code) }
function Submit($code, $year, $month) { Call Post "/api/timesheet/$year/$month/submit" (Who $code) }
function Approve($owner, $by, $year, $month) { Call Post "/api/approvals/$owner/$year/$month/approve" (Who $by) }
function Return-Sheet($owner, $by, $year, $month, $text) { Call Post "/api/approvals/$owner/$year/$month/return" (Who $by) (@{ message = $text } | ConvertTo-Json) }

# Two months ago Amra's month was returned by Benjamin. Last month Amra and Dino submitted; Dino's is approved up to the lawyer
# (in the finance role). This month stays a draft for everybody: it is the one the reminders talk about.
Month 'Z-001' $two.Year $two.Month; Submit 'Z-001' $two.Year $two.Month
Return-Sheet 'Z-001' 'Z-002' $two.Year $two.Month 'Dodaj opis prekovremenog rada od 12.'
Month 'Z-001' $one.Year $one.Month; Submit 'Z-001' $one.Year $one.Month
Month 'Z-004' $one.Year $one.Month
Call Post "/api/timesheet/$($one.Year)/$($one.Month)/overtime" (Who 'Z-004') (@{ date = $saturday.ToString('yyyy-MM-dd'); start = '08:30:00'; end = '11:00:00'; description = 'Provisioning, tiketi, urgencije' } | ConvertTo-Json)
Submit 'Z-004' $one.Year $one.Month
if ($Role -eq 'finance') { Approve 'Z-004' 'Z-002' $one.Year $one.Month; Approve 'Z-004' 'Z-003' $one.Year $one.Month }

Write-Host ''
Write-Host "Signed in as $me, who is playing: $mine"
Write-Host 'Server for the app:  http://localhost:5090  (Windows sign-in)'
Write-Host 'Server to act as others: http://localhost:5091 with the header X-Test-User: lab\amra | lab\benjamin | lab\emir | lab\dino | lab\admin'

if (-not $NoApp) {
    New-Item -ItemType Directory -Force (Split-Path $modeFile) | Out-Null
    Set-Content $modeFile 'employee'
    $env:DTM_ALLOW_MULTIPLE = '1'
    $env:Company__ApiUrl = 'http://localhost:5090'
    Start-Process $appExe -WorkingDirectory (Split-Path $appExe)
    Write-Host 'The app is starting in employee mode.'
}
Write-Host 'Stop everything with:  .\tools\lab\lab.ps1 -Stop'
