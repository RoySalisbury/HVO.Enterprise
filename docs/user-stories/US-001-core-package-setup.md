# US-001: Core Package Setup and Dependencies

**Status**: ❌ Not Started  
**Category**: Core Package  
**Effort**: 3 story points  
**Sprint**: 1

## Description

As a **library developer**,  
I want to **set up the core HVO.Enterprise.Telemetry package with proper dependencies and folder structure**,  
So that **I have a solid foundation for implementing telemetry features with .NET Standard 2.0 compatibility**.

## Acceptance Criteria

1. **Project Creation**
   - [ ] `HVO.Enterprise.Telemetry.csproj` created targeting `netstandard2.0`
   - [ ] Project builds successfully with zero warnings
   - [ ] Package metadata configured (Version, Authors, Description, etc.)

2. **Dependencies Configured**
   - [ ] `System.Diagnostics.DiagnosticSource` v8.0.1 added
   - [ ] `OpenTelemetry.Api` v1.9.0 added
   - [ ] `Microsoft.Extensions.Logging.Abstractions` added
   - [ ] `Microsoft.Extensions.DependencyInjection.Abstractions` added
   - [ ] `Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions` v8.0.0 added
   - [ ] `Microsoft.Extensions.Configuration.Abstractions` added
   - [ ] `System.Threading.Channels` v7.0.0 added
   - [ ] `System.Net.Http` added

3. **Folder Structure Created**
   - [ ] `Abstractions/` - Interfaces and base classes
   - [ ] `ActivitySources/` - Activity and tracing infrastructure
   - [ ] `Metrics/` - Metrics collection and recording
   - [ ] `Correlation/` - Correlation ID management
   - [ ] `Proxies/` - DispatchProxy implementation
   - [ ] `Http/` - HTTP client instrumentation
   - [ ] `HealthChecks/` - Health check implementations
   - [ ] `Configuration/` - Configuration management
   - [ ] `Lifecycle/` - Lifecycle and shutdown management
   - [ ] `Enrichers/` - Context enrichers
   - [ ] `BackgroundJobs/` - Background job correlation
   - [ ] `Exceptions/` - Exception tracking and aggregation
   - [ ] `Logging/` - ILogger integration

4. **Language Configuration**
   - [ ] `<LangVersion>latest</LangVersion>` set
   - [ ] `<Nullable>enable</Nullable>` enabled
   - [ ] `<ImplicitUsings>disable</ImplicitUsings>` disabled
   - [ ] `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` set

## Technical Requirements

### Project File Configuration

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    
    <!-- Package Information -->
    <PackageId>HVO.Enterprise.Telemetry</PackageId>
    <Version>1.0.0-preview.1</Version>
    <Authors>HVO Enterprise</Authors>
    <Description>Core telemetry library for unified observability across .NET platforms</Description>
    <PackageTags>telemetry;logging;tracing;metrics;observability;opentelemetry</PackageTags>
    <RepositoryUrl>https://github.com/RoySalisbury/HVO.Enterprise</RepositoryUrl>
    <PackageLicenseExpression>MIT</PackageLicenseExpression>
    
    <!-- Documentation -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>CS1591</NoWarn> <!-- Missing XML comments - will remove once documented -->
  </PropertyGroup>

  <ItemGroup>
    <!-- OpenTelemetry and Diagnostics -->
    <PackageReference Include="System.Diagnostics.DiagnosticSource" Version="8.0.1" />
    <PackageReference Include="OpenTelemetry.Api" Version="1.9.0" />
    
    <!-- Microsoft Extensions -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="8.0.0" />
    
    <!-- Background Processing -->
    <PackageReference Include="System.Threading.Channels" Version="7.0.0" />
    
    <!-- HTTP Support -->
    <PackageReference Include="System.Net.Http" Version="4.3.4" />
  </ItemGroup>
</Project>
```

### Folder Structure Details

Each folder should contain:
- Placeholder `.gitkeep` file initially
- Namespace matching: `HVO.Enterprise.Telemetry.{FolderName}`
- README.md explaining folder purpose (optional for v1.0)

### Compatibility Constraints

- **Must work on**: .NET Framework 4.8, .NET Core 2.0+, .NET 5+, .NET 6+, .NET 8+
- **Language features**: Limited to C# features available in .NET Standard 2.0
- **No modern shortcuts**: Avoid `ArgumentNullException.ThrowIfNull()` and similar .NET 6+ APIs
- **Explicit usings**: Always include `using System;` and other required namespaces

## Testing Requirements

### Unit Tests

1. **Build Verification**
   - [ ] Project builds successfully with `dotnet build`
   - [ ] No compilation warnings
   - [ ] Documentation XML file generated

2. **Compatibility Tests**
   - [ ] Reference project from .NET Framework 4.8 test project
   - [ ] Reference project from .NET 8 test project
   - [ ] Both projects build and reference successfully

3. **Package Tests**
   - [ ] `dotnet pack` creates NuGet package successfully
   - [ ] Package contains correct assemblies
   - [ ] Package metadata is correct

### Integration Tests

1. **Dependency Resolution**
   - [ ] All NuGet dependencies resolve correctly on .NET Framework 4.8
   - [ ] All NuGet dependencies resolve correctly on .NET 8
   - [ ] No dependency conflicts

## Dependencies

**Blocked By**: None (this is the first story)  
**Blocks**: All other core package stories (US-002 through US-018)

## Definition of Done

- [ ] Project file created with all required dependencies
- [ ] All folder structure in place with `.gitkeep` files
- [ ] Project builds with zero warnings
- [ ] Package can be created with `dotnet pack`
- [ ] Successfully referenced from both .NET Framework 4.8 and .NET 8 projects
- [ ] Code reviewed and approved
- [ ] Committed to feature branch

## Notes

### Design Decisions

1. **Why .NET Standard 2.0 only?**
   - Single binary deployment across all platforms
   - User explicitly requested no multi-targeting
   - Runtime feature detection handles platform differences

2. **Why these specific dependency versions?**
   - Latest stable versions compatible with .NET Standard 2.0
   - `System.Threading.Channels` v7.0.0 is last version supporting netstandard2.0
   - OpenTelemetry.Api v1.9.0 provides stable abstractions

3. **Why disable implicit usings?**
   - Project convention for explicit dependencies
   - Clearer code, easier to understand imports
   - Better compatibility with older tooling

### Implementation Tips

- Start with minimal project file, add dependencies incrementally
- Test build after each dependency addition
- Verify folder naming matches namespace conventions
- Add XML documentation comments from the start

### Future Considerations

- Consider adding `SourceLink` for debugging support
- May add `Nullable` context for better null safety
- Performance analyzers can be added later

## Related Documentation

- [Project Plan](../project-plan.md#1-create-core-netstandard20-package-with-microsoft-abstractions)
- [Coding Standards](../../.github/copilot-instructions.md)
- [.NET Standard 2.0 API Reference](https://learn.microsoft.com/en-us/dotnet/standard/net-standard?tabs=net-standard-2-0)
