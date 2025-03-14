# normal package

# build strategy
# delete bin -> build generator -> copy generator -> build frent -> push
# detete bin -> build generator -> copy generator -> tmp csproj -> build frent.unity -> delete tmp csproj -> push

echo "Building Generator"

rm Frent\bin\Release\* -Recurse

dotnet build -c Release Frent.Generator\Frent.Generator.csproj

Copy-Item -Path ".\Frent.Generator\bin\Release\netstandard2.0\Frent.Generator.dll" -Destination ".\Frent\bin\Release\"

echo "Building Frent"

dotnet build -c Release Frent\Frent.csproj /p:Publish=true

$package = Get-ChildItem -Path ".\Frent\bin\Release" -Filter "*.nupkg" | Select-Object -First 1

echo "Pushing package $package"

pause

dotnet nuget push ".\Frent\bin\Release\$package" --api-key $Env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

pause

clear

# unity package

rm Frent\bin\Release\* -Recurse

echo "Building Unity Generator"

dotnet build -c Release Frent.Generator\Frent.Generator.csproj /p:Unity="#UNITY"

Copy-Item -Path ".\Frent.Generator\bin\Release\netstandard2.0\Frent.Generator.dll" -Destination ".\Frent\bin\Release\"

echo "Building Frent.Unity"

# copy tmp csproj
Copy-Item -Path ".\Frent\Frent.csproj" -Destination ".\Frent\Frent.Unity.csproj"

dotnet build -c Release Frent\Frent.Unity.csproj /p:Publish=true

# cleanup
rm ".\Frent\Frent.Unity.csproj"

$package = Get-ChildItem -Path ".\Frent\bin\Release" -Filter "*.nupkg" | Select-Object -First 1

echo "Pushing package $package"

pause

dotnet nuget push ".\Frent\bin\Release\$package" --api-key $Env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json

pause

echo "DONE! Don't forget to update docs!!!!"

pause