# HVO.Common

Common library providing shared patterns and utilities for all HVO projects.

## Features

- **Result\<T\>** — Functional error handling without exceptions
- **Option\<T\>** — Safe handling of optional values
- **IOneOf / Discriminated Unions** — Type-safe variant types
- **Extension Methods** — String, Enum, and collection utilities

## Installation

```
dotnet add package HVO.Common
```

## Quick Start

```csharp
using HVO.Common.Results;
using HVO.Common.Options;

// Result pattern for error handling
Result<Customer> result = GetCustomer(id);
if (result.IsSuccessful)
    Console.WriteLine(result.Value.Name);

// Option pattern for optional values
Option<string> setting = GetSetting("key");
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.6.1+ and .NET Core 2.0+)

## License

MIT — see [LICENSE](https://github.com/RoySalisbury/HVO.Enterprise/blob/main/LICENSE) for details.
