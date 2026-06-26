@echo off
cd /d "%~dp0"
echo Starting Machine3 User Report Service on http://0.0.0.0:8083
echo Local test URL: http://localhost:8083
echo Public URL: http://160.250.132.117:8083
dotnet run --urls http://0.0.0.0:8083
