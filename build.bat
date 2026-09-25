@echo off
setlocal

echo Building add-in...
dotnet build tb-test-addin-master

echo Building tbaddin_creator...
dotnet build tbaddin_creator

echo Running tbaddin_creator...
dotnet run --project tbaddin_creator

endlocal

pause