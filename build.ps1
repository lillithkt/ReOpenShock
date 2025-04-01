echo Building...
dotnet build --no-restore
echo Copying
Copy-Item bin/Debug/netstandard2.1/ReOpenShock.dll "$env:appdata\r2modmanPlus-local\REPO\profiles\mod dev\BepInEx\plugins\lillithkt-ReOpenShock"
Copy-Item bin/Debug/netstandard2.1/ReOpenShock.pdb "$env:appdata\r2modmanPlus-local\REPO\profiles\mod dev\BepInEx\plugins\lillithkt-ReOpenShock"
echo Done!