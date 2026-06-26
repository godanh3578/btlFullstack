@echo off
cd /d "%~dp0"

if not exist "single-run\Machine3-UserReportService.exe" (
  dotnet publish Machine3-UserReportService.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o single-run
)

"single-run\Machine3-UserReportService.exe" --urls http://0.0.0.0:8083
