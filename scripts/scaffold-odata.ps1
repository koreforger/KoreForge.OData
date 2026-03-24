<#
.SYNOPSIS
    Creates placeholder partial controller files for OData CRUD customisation.

.DESCRIPTION
    Reflects over a compiled assembly, finds all DbContext types marked with
    [GenerateODataFor], then creates an empty partial controller file for each
    non-ignored DbSet entity that doesn't already have one.

    Run after the first successful build so the source generator has already
    produced the controller base classes.

.PARAMETER ProjectDir
    Path to the consuming project directory (where Controllers/ will be created).

.PARAMETER AssemblyPath
    Path to the compiled assembly containing the DbContext types.

.EXAMPLE
    .\scaffold-odata.ps1 -ProjectDir "C:\src\MyApi" -AssemblyPath "C:\src\MyApi\bin\Debug\net10.0\MyApi.dll"
#>
param(
    [Parameter(Mandatory)]
    [string]$ProjectDir,

    [Parameter(Mandatory)]
    [string]$AssemblyPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Test-Path $AssemblyPath)) {
    Write-Error "Assembly not found: $AssemblyPath"
    return
}

$controllersDir = Join-Path $ProjectDir 'Controllers'
if (-not (Test-Path $controllersDir)) {
    New-Item -ItemType Directory -Path $controllersDir | Out-Null
    Write-Host "Created $controllersDir"
}

# Use dotnet script to reflect over the assembly and extract DbSet entity names + namespaces
$script = @"
using System;
using System.Linq;
using System.Reflection;

var asm = Assembly.LoadFrom(@"$($AssemblyPath.Replace('"','""'))");

// Find types that derive from DbContext
var dbContextBase = asm.GetTypes()
    .Concat(AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } }))
    .FirstOrDefault(t => t.FullName == "Microsoft.EntityFrameworkCore.DbContext");

if (dbContextBase == null) { Console.Error.WriteLine("EF Core DbContext type not found."); return; }

foreach (var ctxType in asm.GetTypes().Where(t => !t.IsAbstract && dbContextBase.IsAssignableFrom(t)))
{
    var ctxName = ctxType.Name;
    var ctxNs = ctxType.Namespace ?? "";

    foreach (var prop in ctxType.GetProperties())
    {
        var pt = prop.PropertyType;
        if (!pt.IsGenericType) continue;
        var gtd = pt.GetGenericTypeDefinition();
        if (gtd.FullName != "Microsoft.EntityFrameworkCore.DbSet``1") continue;

        var entityType = pt.GetGenericArguments()[0];

        // Skip [ODataIgnore]
        if (entityType.GetCustomAttributes(true).Any(a => a.GetType().Name == "ODataIgnoreAttribute")) continue;

        var entityName = entityType.Name;
        Console.WriteLine(entityName + "|" + ctxNs);
    }
}
"@

# Write the script to a temp file and run it
$tempScript = [System.IO.Path]::GetTempFileName() -replace '\.tmp$', '.csx'
$script | Set-Content -Path $tempScript -Encoding UTF8

try {
    $entities = & dotnet script $tempScript 2>&1 | Where-Object { $_ -match '\|' }
} catch {
    Write-Warning "dotnet-script not available. Falling back to manual mode."
    Write-Warning "Install with: dotnet tool install -g dotnet-script"
    Write-Host ""
    Write-Host "To create placeholder controllers manually, add files like:"
    Write-Host "  Controllers/<EntityName>Controller.cs"
    Write-Host ""
    Write-Host "With content:"
    Write-Host '  namespace YourProject.Controllers;'
    Write-Host '  public partial class <EntityName>Controller { }'
    return
} finally {
    Remove-Item $tempScript -ErrorAction SilentlyContinue
}

$created = 0
foreach ($line in $entities) {
    $parts = $line.ToString().Split('|')
    $entityName = $parts[0].Trim()
    $contextNs = $parts[1].Trim()

    $controllerName = "${entityName}Controller"
    $filePath = Join-Path $controllersDir "$controllerName.cs"

    if (Test-Path $filePath) {
        Write-Host "  SKIP  $controllerName.cs (already exists)"
        continue
    }

    $namespace = if ($contextNs) { "$contextNs.Controllers" } else { "Controllers" }

    $content = @"
namespace $namespace;

/// <summary>
/// Partial extension point for the generated $controllerName.
/// Add custom OData actions, function imports, or hook overrides here.
/// </summary>
public partial class $controllerName
{
}
"@

    $content | Set-Content -Path $filePath -Encoding UTF8
    Write-Host "  CREATE $controllerName.cs"
    $created++
}

Write-Host ""
Write-Host "Done. Created $created placeholder controller(s) in $controllersDir"
