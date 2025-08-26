# Resonite.GameLibs

Stripped reference assemblies for the [Resonite](https://store.steampowered.com/app/2519830/Resonite/) platform.

## Features

- Reference assemblies stripped of implementation details
- Non-publicized assemblies for internal API access
- Elements.Quantity built from a patched fork to remove the unused ExtensionAttribute class that causes warnings ([PR](https://github.com/Yellow-Dog-Man/Elements.Quantity/pull/22))
- Includes PDB files for debugging support
- Includes XML documentation files
- Automatically generated and updated via GitHub Actions

## Version Information

This package is automatically updated to match the latest Resonite platform version.

## Usage

Add this package to your project to reference Resonite assemblies without requiring the full game installation.

For best IDE experience, configure your project to prefer local references when available (for better decompilation support) and fall back to this package otherwise:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    ...
    <GamePath Condition="'$(ResonitePath)' != ''">$(ResonitePath)/</GamePath>
    <GamePath Condition="Exists('$(MSBuildProgramFiles32)\Steam\steamapps\common\Resonite\')">$(MSBuildProgramFiles32)\Steam\steamapps\common\Resonite\</GamePath>
    <GamePath Condition="Exists('$(HOME)/.steam/steam/steamapps/common/Resonite/')">$(HOME)/.steam/steam/steamapps/common/Resonite/</GamePath>
  </PropertyGroup>

  <!-- dependencies -->
  <ItemGroup>
    ...
  </ItemGroup>

  <!-- NuGet fallback stripped game references -->
  <ItemGroup Condition="!Exists('$(GamePath)')">
    <PackageReference Include="Resonite.GameLibs" Version="2025.*" />
  </ItemGroup>

  <!-- Local game references -->
  <ItemGroup Condition="Exists('$(GamePath)')">
    <Reference Include="FrooxEngine">
      <HintPath>$(GamePath)FrooxEngine.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="Elements.Core">
      <HintPath>$(GamePath)Elements.Core.dll</HintPath>
      <Private>False</Private>
    </Reference>
    <Reference Include="Renderite.Shared">
      <HintPath>$(GamePath)Renderite.Shared.dll</HintPath>
      <Private>False</Private>
    </Reference>
  </ItemGroup>
</Project>
```