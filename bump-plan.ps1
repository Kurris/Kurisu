#requires -Version 7.0

<#
.SYNOPSIS
分析项目依赖链路，输出版本更新与发包计划。

.DESCRIPTION
该脚本本身不会修改项目版本或发布 NuGet 包，只会生成 package.bump.json
以及可在 Windows 下执行的 do-bump-<项目>-<类型>.ps1 发布脚本。

.PARAMETER Changed
发生变更的项目名称，不包含 .csproj 后缀。

.PARAMETER Bump
语义化版本升级类型，可选 patch、minor、major，默认为 patch。

.EXAMPLE
.\bump-plan.ps1 Kurisu.AspNetCore.Abstractions patch

.EXAMPLE
.\bump-plan.ps1 Kurisu.AspNetCore.Abstractions minor
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateNotNullOrEmpty()]
    [string]$Changed,

    [Parameter(Position = 1)]
    [ValidateSet('patch', 'minor', 'major')]
    [string]$Bump = 'patch'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = $PSScriptRoot
$sourceRoot = Join-Path $repoRoot 'src'
$jsonFileName = 'package.bump.json'
$jsonFile = Join-Path $repoRoot $jsonFileName

function Get-BumpedVersion {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [ValidateSet('patch', 'minor', 'major')]
        [string]$Type
    )

    if ($Version -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)$') {
        return 'bump'
    }

    $major = [int]$Matches.major
    $minor = [int]$Matches.minor
    $patch = [int]$Matches.patch

    switch ($Type) {
        'major' { return '{0}.0.0' -f ($major + 1) }
        'minor' { return '{0}.{1}.0' -f $major, ($minor + 1) }
        'patch' { return '{0}.{1}.{2}' -f $major, $minor, ($patch + 1) }
    }
}

function Get-ProjectVersion {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    $content = [IO.File]::ReadAllText($ProjectPath)
    $match = [regex]::Match($content, '<Version>(?<version>[^<]+)</Version>')
    if ($match.Success) {
        return $match.Groups['version'].Value
    }

    return '?.?.?'
}

function Get-ProjectReferences {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)

    [xml]$projectXml = [IO.File]::ReadAllText($ProjectPath)
    $references = @($projectXml.SelectNodes("//*[local-name()='ProjectReference']"))
    foreach ($reference in $references) {
        $include = [string]$reference.Include
        if (-not [string]::IsNullOrWhiteSpace($include)) {
            [IO.Path]::GetFileNameWithoutExtension($include)
        }
    }
}

function Write-ProjectTableRow {
    param(
        [string]$Name,
        [string]$Version
    )

    Write-Host ('  {0,-50} | {1,10}' -f $Name, $Version)
}

function Write-DependencyTree {
    param(
        [Parameter(Mandatory = $true)][string]$Project,
        [string]$Indent = '',
        [string]$Prefix = ''
    )

    if (-not $rounds.ContainsKey($Project)) {
        return
    }

    $roundLabel = if ($rounds[$Project] -gt 0) { ' [R{0}]' -f $rounds[$Project] } else { '' }
    Write-Host ('{0}{1}{2} {3} -> {4}{5}' -f
        $Indent, $Prefix, $Project, $versions[$Project], $newVersions[$Project], $roundLabel)

    $directChildren = if ($reverseDependencies.ContainsKey($Project)) {
        @($reverseDependencies[$Project])
    } else {
        @()
    }
    $children = @($directChildren | Where-Object {
            $rounds.ContainsKey($_) -and $rounds[$_] -gt $rounds[$Project]
        } | Sort-Object -Unique)

    for ($index = 0; $index -lt $children.Count; $index++) {
        $isLast = $index -eq ($children.Count - 1)
        $childPrefix = if ($isLast) { '└── ' } else { '├── ' }
        Write-DependencyTree -Project $children[$index] -Indent ($Indent + '    ') -Prefix $childPrefix
    }
}

# 1. 扫描项目及直接依赖。
$versions = @{}
$dependencies = @{}
$reverseDependencies = @{}
$projectPaths = @{}

$projectFiles = Get-ChildItem -LiteralPath $sourceRoot -Filter '*.csproj' -File -Recurse |
    Where-Object {
        $_.FullName -notlike '*\Kurisu.Transaction.Analyzer.Sample\*' -and
        $_.FullName -notlike '*\Kurisu.Transaction.Analyzer.Tests\*'
    } |
    Sort-Object FullName

foreach ($projectFile in $projectFiles) {
    $name = $projectFile.BaseName
    if ($projectPaths.ContainsKey($name)) {
        throw "存在同名项目 '$name': '$($projectPaths[$name])' 和 '$($projectFile.FullName)'。"
    }

    $projectPaths[$name] = $projectFile.FullName
    $versions[$name] = Get-ProjectVersion -ProjectPath $projectFile.FullName
    $dependencies[$name] = @(Get-ProjectReferences -ProjectPath $projectFile.FullName)
}

foreach ($project in $dependencies.Keys) {
    foreach ($dependency in $dependencies[$project]) {
        if (-not $reverseDependencies.ContainsKey($dependency)) {
            $reverseDependencies[$dependency] = [Collections.Generic.List[string]]::new()
        }
        $reverseDependencies[$dependency].Add($project)
    }
}

if (-not $versions.ContainsKey($Changed)) {
    Write-Host "✗ 找不到项目: $Changed"
    Write-Host '  可用项目:'
    foreach ($project in ($versions.Keys | Sort-Object)) {
        Write-Host ('    {0,-52} {1}' -f $project, $versions[$project])
    }
    exit 1
}

# 2. 计算受影响项目和发布轮次。
$newVersions = @{}
$rounds = @{}
$visited = [Collections.Generic.HashSet[string]]::new()
$queue = [Collections.Generic.Queue[string]]::new()

$newVersions[$Changed] = Get-BumpedVersion -Version $versions[$Changed] -Type $Bump
$rounds[$Changed] = 0
[void]$visited.Add($Changed)
$queue.Enqueue($Changed)

while ($queue.Count -gt 0) {
    $current = $queue.Dequeue()
    $currentRound = [int]$rounds[$current]
    $dependents = if ($reverseDependencies.ContainsKey($current)) {
        @($reverseDependencies[$current])
    } else {
        @()
    }

    foreach ($dependent in $dependents) {
        $candidateRound = $currentRound + 1
        if (-not $rounds.ContainsKey($dependent) -or $candidateRound -gt $rounds[$dependent]) {
            $rounds[$dependent] = $candidateRound
        }

        if ($visited.Add($dependent)) {
            $queue.Enqueue($dependent)
        }
    }
}

foreach ($project in $rounds.Keys) {
    if ($project -ne $Changed) {
        $newVersions[$project] = Get-BumpedVersion -Version $versions[$project] -Type $Bump
    }
}

$maxRound = [int](($rounds.Values | Measure-Object -Maximum).Maximum)
$unaffected = @($versions.Keys | Where-Object { -not $rounds.ContainsKey($_) } | Sort-Object)
$bumpLabel = $Bump.ToUpperInvariant()

# 3. 输出计划。
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════════╗'
Write-Host '║  版本更新与发包计划（DRY-RUN）                               ║'
Write-Host "║  变更: $Changed  升级: $bumpLabel"
Write-Host '╚══════════════════════════════════════════════════════════════╝'
Write-Host ''

if ($unaffected.Count -gt 0) {
    Write-Host '━━━ 不受影响，无需更新 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'
    Write-ProjectTableRow -Name '项目' -Version '版本'
    Write-Host ('  {0,-50}-+-{1}' -f ('-' * 50), ('-' * 10))
    foreach ($project in $unaffected) {
        Write-ProjectTableRow -Name $project -Version $versions[$project]
    }
} else {
    Write-Host '━━━ 不受影响: (无，所有项目均受影响) ━━━'
}

Write-Host ''
Write-Host '━━━ 更新链路详情 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'
for ($round = 1; $round -le $maxRound; $round++) {
    $roundDescription = if ($round -eq 1) { '直接依赖者' } else { '间接受影响' }
    Write-Host ''
    Write-Host "▶ Round $round ($roundDescription)"

    foreach ($project in ($rounds.Keys | Sort-Object)) {
        if ($rounds[$project] -ne $round) {
            continue
        }

        Write-Host ('  {0,-48} {1} -> {2}' -f $project, $versions[$project], $newVersions[$project])
        $affectedDependencies = foreach ($dependency in $dependencies[$project]) {
            if ($rounds.ContainsKey($dependency)) {
                '{0}({1}->{2})' -f $dependency, $versions[$dependency], $newVersions[$dependency]
            }
        }
        if (@($affectedDependencies).Count -gt 0) {
            Write-Host ('    ↳ 变更触发自: {0}' -f ($affectedDependencies -join ' '))
        }
    }
}

Write-Host ''
Write-Host '━━━ 受影响子图的依赖链路 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'
Write-DependencyTree -Project $Changed

Write-Host ''
Write-Host '━━━ 需要更新版本并发包 ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━'
Write-Host ('  {0,-4} | {1,-50} | {2,10} | {3,10} | {4}' -f '轮次', '项目', '当前版本', '新版本', '状态')
Write-Host ('  {0}-+-{1}-+-{2}-+-{3}-+-{4}' -f ('-' * 4), ('-' * 50), ('-' * 10), ('-' * 10), ('-' * 8))

$planProjects = [Collections.Generic.List[object]]::new()
for ($round = 0; $round -le $maxRound; $round++) {
    foreach ($project in ($rounds.Keys | Sort-Object)) {
        if ($rounds[$project] -ne $round) {
            continue
        }

        $status = if ($round -eq 0) { '变更起点' } elseif ($round -eq 1) { '直接依赖' } else { '间接依赖' }
        Write-Host ('  {0,-4} | {1,-50} | {2,10} | {3,10} | {4}' -f
            "R$round", $project, $versions[$project], $newVersions[$project], $status)

        $relativePath = [IO.Path]::GetRelativePath($repoRoot, $projectPaths[$project]).Replace('\', '/')
        $planProjects.Add([ordered]@{
                name    = $project
                current = $versions[$project]
                new     = $newVersions[$project]
                round   = $round
                updated = $false
                csproj  = $relativePath
            })
    }
}

Write-Host ''
Write-Host '  --------------------------------------------------------------'
Write-Host ('  共 {0} 轮，{1} 个项目需更新版本并发包' -f ($maxRound + 1), $rounds.Count)
Write-Host '  每轮内可并行，轮间必须串行（下游 nuspec 依赖上游新版本号）'

# 4. 生成 JSON 计划。
$plan = [ordered]@{
    changed  = $Changed
    bump     = $Bump
    rounds   = $maxRound + 1
    total    = $rounds.Count
    projects = $planProjects
}
$utf8NoBom = [Text.UTF8Encoding]::new($false)
[IO.File]::WriteAllText($jsonFile, ($plan | ConvertTo-Json -Depth 5) + [Environment]::NewLine, $utf8NoBom)

Write-Host ''
Write-Host '━━━ 已生成 package.bump.json ━━━━━━━━━━━━━━━━━━━━━━━━━━'

# 5. 生成 Windows 发布脚本。该脚本负责真正更新 Version、构建并推送包。
$publishScriptName = "do-bump-$Changed-$Bump.ps1"
$publishScriptPath = Join-Path $repoRoot $publishScriptName
$generatedAt = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'

$publishScript = @"
#requires -Version 7.0
# ============================================================
# 自动生成于: $generatedAt
# 变更项目: $Changed
# 升级类型: $bumpLabel
# 受影响: $($rounds.Count) 个项目，$($maxRound + 1) 轮
#
# 幂等: 可重复执行，已更新项目自动跳过。
#
# 环境变量:
#   NUGET_SERVER_URL   NuGet 服务器地址，必须设置。
#   NUGET_API_KEY      NuGet API Key，必须设置。
# ============================================================
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
`$ErrorActionPreference = 'Stop'

`$repoRoot = `$PSScriptRoot
`$jsonFile = Join-Path `$repoRoot '$jsonFileName'
`$nugetServerUrl = `$env:NUGET_SERVER_URL
`$nugetApiKey = `$env:NUGET_API_KEY
`$utf8NoBom = [Text.UTF8Encoding]::new(`$false)

if ([string]::IsNullOrWhiteSpace(`$nugetServerUrl)) {
    throw '请设置环境变量 NUGET_SERVER_URL。'
}
if ([string]::IsNullOrWhiteSpace(`$nugetApiKey)) {
    throw '请设置环境变量 NUGET_API_KEY。'
}
if (-not (Test-Path -LiteralPath `$jsonFile -PathType Leaf)) {
    throw "找不到发布计划: `$jsonFile"
}

Write-Host ''
Write-Host '===== Step 1: 更新 csproj Version ====='
`$plan = Get-Content -LiteralPath `$jsonFile -Raw | ConvertFrom-Json
`$anyUpdate = `$false

foreach (`$project in `$plan.projects) {
    `$projectPath = Join-Path `$repoRoot ([string]`$project.csproj)
    if (-not (Test-Path -LiteralPath `$projectPath -PathType Leaf)) {
        throw "`$(`$project.name): 文件不存在 `$projectPath"
    }

    `$content = [IO.File]::ReadAllText(`$projectPath)
    `$oldMarker = '<Version>{0}</Version>' -f `$project.current
    `$newMarker = '<Version>{0}</Version>' -f `$project.new

    if (`$content.Contains(`$oldMarker)) {
        [IO.File]::WriteAllText(`$projectPath, `$content.Replace(`$oldMarker, `$newMarker), `$utf8NoBom)
        `$project.updated = `$true
        `$anyUpdate = `$true
        Write-Host "  ✓ `$(`$project.name)  `$(`$project.current) -> `$(`$project.new)"
    } elseif (`$content.Contains(`$newMarker)) {
        `$project.updated = `$true
        Write-Host "  - `$(`$project.name)  已更新，跳过"
    } else {
        throw "`$(`$project.name): csproj 中 Version 不是 `$(`$project.current)，也不是 `$(`$project.new)，请手动处理。"
    }
}

if (`$anyUpdate) {
    [IO.File]::WriteAllText(`$jsonFile, (`$plan | ConvertTo-Json -Depth 5) + [Environment]::NewLine, `$utf8NoBom)
} else {
    Write-Host '  所有项目已是最新版本，无需更新'
}

Write-Host ''
Write-Host '===== Step 2: 按轮次打包并发布 ====='
for (`$round = 0; `$round -lt [int]`$plan.rounds; `$round++) {
    Write-Host ''
    Write-Host "━━━ Round `$round ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

    foreach (`$project in @(`$plan.projects | Where-Object { [int]`$_.round -eq `$round })) {
        `$projectPath = Join-Path `$repoRoot ([string]`$project.csproj)
        & dotnet build `$projectPath -c Release
        if (`$LASTEXITCODE -ne 0) {
            throw "`$(`$project.name) 构建失败，退出码: `$LASTEXITCODE"
        }

        `$packageDirectory = Join-Path (Split-Path -Parent `$projectPath) 'bin\Release'
        `$packagePath = Join-Path `$packageDirectory "`$(`$project.name).`$(`$project.new).nupkg"
        if (-not (Test-Path -LiteralPath `$packagePath -PathType Leaf)) {
            throw "`$(`$project.name) 构建后未找到包: `$packagePath"
        }

        & dotnet nuget push `$packagePath --source `$nugetServerUrl --api-key `$nugetApiKey --skip-duplicate --no-service-endpoint
        if (`$LASTEXITCODE -ne 0) {
            throw "`$(`$project.name) 发布失败，退出码: `$LASTEXITCODE"
        }
    }
}

Write-Host ''
Write-Host '===== 发包完成 ====='
"@

[IO.File]::WriteAllText($publishScriptPath, $publishScript.TrimStart() + [Environment]::NewLine, $utf8NoBom)

Write-Host ''
Write-Host '━━━ 已生成 Windows 发布脚本 ━━━━━━━━━━━━━━━━━━━━━━━━━━━'
Write-Host "  .\$publishScriptName"
Write-Host '  执行该脚本会: 1) 更新 csproj Version  2) 按轮次 build + push NuGet'
Write-Host '  支持幂等: 重复执行自动跳过已更新项目和已存在的包'
