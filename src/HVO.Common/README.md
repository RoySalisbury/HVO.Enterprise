# HVO.Common

A .NET Standard 2.0 utility library providing functional programming patterns and common utilities for all HVO projects.

## Features

- Result<T> - Functional error handling without exceptions
- Option<T> - Type-safe optional values
- OneOf<T1, T2, ...> - Discriminated unions (sum types)
- Extension Methods - String, collection, and enum utilities
- Guard and Ensure - Input validation and runtime assertions

## Installation

```bash
dotnet add package HVO.Common
```

## Usage Examples

### Result<T> Pattern

```csharp
using HVO.Common.Results;

public Result<Customer> GetCustomer(int id)
{
    try
    {
        if (id <= 0)
            return Result<Customer>.Failure(
                new ArgumentException("Invalid ID"));

        var customer = _repository.Find(id);
        return customer ?? Result<Customer>.Failure(
            new NotFoundException($"Customer {id} not found"));
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion
    }
}

var result = GetCustomer(123);
if (result.IsSuccessful)
{
    Console.WriteLine($"Found: {result.Value.Name}");
}
else
{
    _logger.LogError(result.Error, "Failed to get customer");
}
```

### Option<T> Pattern

```csharp
using HVO.Common.Options;

public Option<string> GetConfigValue(string key)
{
    return _config.TryGetValue(key, out var value)
        ? new Option<string>(value)
        : Option<string>.None();
}

var apiKey = GetConfigValue("ApiKey").GetValueOrDefault("default-key");
```

### OneOf<T1, T2, ...>

```csharp
using HVO.Common.OneOf;

public OneOf<Customer, Guest, Anonymous> GetUser(string id)
{
    if (IsCustomer(id)) return GetCustomer(id);
    if (IsGuest(id)) return GetGuest(id);
    return new Anonymous();
}

var user = GetUser("123");
var greeting = user.Match(
    customer => $"Welcome back, {customer.Name}!",
    guest => "Welcome, guest!",
    anonymous => "Please sign in");
```

### Extension Methods

```csharp
using HVO.Common.Extensions;

var preview = longText.TruncateWithSuffix(100, "...");
var compact = "Hello World".RemoveWhitespace();

var batches = CollectionExtensions.Chunk(items, 100);
var unique = CollectionExtensions.DistinctBy(items, x => x.Id);

var description = Status.Pending.GetDescription();
```

### Guard and Ensure

```csharp
using HVO.Common.Utilities;

Guard.AgainstNegativeOrZero(id, 0);
Guard.AgainstNullOrWhiteSpace(name);
Ensure.That(order.Items.Any(), "Order must have items");
```

## Target Frameworks

- .NET Standard 2.0 (compatible with .NET Framework 4.6.1+ and modern .NET)

## License

MIT
