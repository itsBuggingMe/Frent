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

## Unity

Unity requires a [different package](https://www.nuget.org/packages/Frent.Unity).

The package can be installed manually or with tools such as [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). If installing manually, tag `Frent.Generator.dll` as a `RoslynAnalyzer`. More information about source generation in Unity is available in the Unity documentation.

The package can be installed manually or with tools such as [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity). If installing manually, make sure to tag the Frent.Generator.dll file with RoslynAnalyzer. More information about source generation in Unity can be found in the Unity documentation.