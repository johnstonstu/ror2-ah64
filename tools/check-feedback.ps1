$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$output = Join-Path $repo 'dist/feedback-checks'
New-Item -ItemType Directory -Force -Path $output | Out-Null
$assets = Get-Content (Join-Path $repo 'AH64Mod/obj/project.assets.json') -Raw | ConvertFrom-Json
$packages = $assets.packageFolders.PSObject.Properties.Name | Select-Object -First 1
$baseLib = $assets.libraries.PSObject.Properties | Where-Object Name -Like 'BepInEx.BaseLib/*' | Select-Object -First 1
$bepinex = Join-Path $packages ($baseLib.Value.path + '/lib/netstandard2.0/BepInEx.dll')
# The descend key is a BepInEx KeyboardShortcut. BepInEx resolves its KeyCode through the UnityEngine
# facade, which forwards it to CoreModule, so the checks need both.
$unityModules = $assets.libraries.PSObject.Properties | Where-Object Name -Like 'UnityEngine.Modules/*' | Select-Object -First 1
$unityLib = Join-Path $packages ($unityModules.Value.path + '/lib/netstandard2.0')
$unityFacade = Join-Path $unityLib 'UnityEngine.dll'
$unityCore = Join-Path $unityLib 'UnityEngine.CoreModule.dll'
$sources = @('tools/FeedbackChecks.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64PlaytestConfig.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64StaticValues.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64BalanceReport.cs',
    'AH64Mod/Characters/Survivors/AH64/Content/AH64BalanceFeedback.cs')
$includes = ($sources | ForEach-Object { '<Compile Include="' + [System.Security.SecurityElement]::Escape((Join-Path $repo $_)) + '" />' }) -join "`n"
$project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup>
  <ItemGroup><Reference Include="BepInEx"><HintPath>$bepinex</HintPath></Reference>
  <Reference Include="UnityEngine"><HintPath>$unityFacade</HintPath></Reference>
  <Reference Include="UnityEngine.CoreModule"><HintPath>$unityCore</HintPath></Reference>
  $includes
  </ItemGroup>
</Project>
"@
$projectPath = Join-Path $output 'FeedbackChecks.csproj'
if (Test-Path $projectPath) { Get-Content $projectPath | Out-Null }
Set-Content -LiteralPath $projectPath -Value $project
dotnet restore $projectPath --configfile (Join-Path $repo 'AH64Mod/nuget.config')
if ($LASTEXITCODE -ne 0) { throw 'Feedback check restore failed.' }
dotnet run --project $projectPath --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Feedback checks failed.' }
