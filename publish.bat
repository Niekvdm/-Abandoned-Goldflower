@echo off
cd GoldFlower
echo Publishing win7-x86 build
dotnet publish -c release -r win7-x86 -o "../build/win7-x86" /p:TrimUnusedDependencies=true
echo Publishing win8-x86 build
dotnet publish -c release -r win8-x86 -o "../build/win8-x86" /p:TrimUnusedDependencies=true
echo Publishing win10-x64 build
dotnet publish -c release -r win10-x64 -o "../build/win10-x64" /p:TrimUnusedDependencies=true
echo Finished publishing
pause