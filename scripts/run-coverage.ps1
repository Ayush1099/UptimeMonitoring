# Run tests with code coverage and generate HTML report
# Requires: dotnet SDK, run from repository root

$ErrorActionPreference = "Stop"
$TestResultsDir = "TestResults"
$CoverageReportDir = "CoverageReport"

# Restore local tools (ReportGenerator)
dotnet tool restore

# Run tests with coverage (Coverlet outputs Cobertura to TestResults)
Write-Host "Running tests with code coverage..." -ForegroundColor Cyan
dotnet test `
  --configuration Release `
  --collect:"XPlat Code Coverage" `
  --results-directory $TestResultsDir `
  --settings coverlet.runsettings

if ($LASTEXITCODE -ne 0) {
  Write-Host "Tests failed." -ForegroundColor Red
  exit $LASTEXITCODE
}

# Find the latest coverage file (Coverlet writes coverage.cobertura.xml per run)
$coverageFile = Get-ChildItem -Path $TestResultsDir -Recurse -Filter "coverage.cobertura.xml" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $coverageFile) {
  Write-Host "No coverage file found in $TestResultsDir" -ForegroundColor Red
  exit 1
}

# Generate HTML report
Write-Host "Generating HTML coverage report..." -ForegroundColor Cyan
dotnet reportgenerator `
  "-reports:$($coverageFile.FullName)" `
  "-targetdir:$CoverageReportDir" `
  "-reporttypes:Html;HtmlSummary;TextSummary"

Write-Host "Coverage report written to $CoverageReportDir/index.html" -ForegroundColor Green
