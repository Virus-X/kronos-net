del /f /q /s bin\nuget
mkdir bin\nuget
nuget.exe pack -outputDirectory bin\nuget
nuget.exe push bin\nuget\*.nupkg -Source https://api.nuget.org/v3/index.json
pause