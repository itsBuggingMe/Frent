# Installation

> [!CAUTION]
> Frent is still in beta.

> [!TIP]
> You will need to enable prerelease to see the package in many UIs.

## .NET

Frent is available on [NuGet](https://www.nuget.org/packages/Frent/).

```pwsh
dotnet add package Frent --prerelease
```

If you prefer to have Frent inside your project as source (either via git clone or submodule), you must first start by getting the project over on [GitHub](https://github.com/itsBuggingMe/Frent):
```bash
# Either clone the repository...
git clone https://github.com/itsBuggingMe/Frent

# ... or add it as a submodule!
git submodule add https://github.com/itsBuggingMe/Frent
git submodule update --init --recursive
```

Frent is a project that depends on source generators to work properly. On the newly-cloned repository you will find, among many project folders, two folders that interest you: **Frent** and **Frent.Generator**. Take note of their path.

Finally, you simply need to add the following block to your `<project>.csproj` file:
```xml
  <ItemGroup>
    <ProjectReference Include="<path>/<to>/<your>/Frent.csproj" />
    <ProjectReference Include="<path>/<to>/<your>/Frent.Generator.csproj"
        OutputItemType="Analyzer"
        ReferenceOutputAssembly="false" />
  </ItemGroup>
```

> [!WARNING]
> Mind your OS directory separator. Windows uses backslashes, whereas Unix systems like Linux and MacOS uses forward slashes.

Finally, simply run `dotnet build` to get started!

## Unity

Unity requires a [different package](https://www.nuget.org/packages/Frent.Unity).

The package can be installed manually or with tools such as [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). If installing manually, tag `Frent.Generator.dll` as a `RoslynAnalyzer`. More information about source generation in Unity is available in the Unity documentation.

The package can be installed manually or with tools such as [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). If installing manually, make sure to tag the Frent.Generator.dll file with RoslynAnalyzer. More information about source generation in Unity can be found in the Unity documentation.