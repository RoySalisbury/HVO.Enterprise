# HVO.Common

Common library providing shared patterns, abstractions, and utilities for all HVO projects.

## Features

- **Result&lt;T&gt; Pattern**: Railway-oriented programming for error handling
- **Option&lt;T&gt;**: Type-safe optional values
- **IOneOf Discriminated Unions**: Type-safe discriminated unions
- **Common Abstractions**: Shared interfaces and base types
- **Extension Methods**: Helpful extensions for common scenarios
- **Utilities**: Shared utility classes and helpers

## Project Structure

```
HVO.Common/
├── Results/              # Result<T> pattern for functional error handling
├── Options/              # Option<T> for type-safe optional values
├── OneOf/                # Discriminated unions (IOneOf, extensions, attributes)
├── Extensions/           # Extension methods (EnumExtensions, etc.)
├── Abstractions/         # Common interfaces and abstract types
└── Utilities/            # Shared utility classes
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.6.1+ and all modern .NET versions)

## Installation

```bash
dotnet add package HVO.Common
```

## Usage

### Result&lt;T&gt; Pattern

```csharp
using HVO.Common.Results;

public Result<Order> CreateOrder(OrderRequest request)
{
    try
    {
        if (!IsValid(request))
            return Result<Order>.Failure(new ValidationException("Invalid request"));
        
        var order = new Order(request);
        return Result<Order>.Success(order);
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion to Result<Order>
    }
}

// Usage
var result = CreateOrder(request);
if (result.IsSuccessful)
{
    Console.WriteLine($"Order created: {result.Value.Id}");
}
else
{
    _logger.LogError(result.Error, "Failed to create order");
}
```

### Result&lt;T, TEnum&gt; with Typed Error Codes

```csharp
using HVO.Common.Results;
using HVO.Common.Extensions;
using System.ComponentModel;

public enum ValidationError
{
    [Description("Invalid input format")]
    InvalidFormat,
    
    [Description("Required field missing")]
    RequiredFieldMissing
}

public Result<Order, ValidationError> ValidateOrder(OrderRequest request)
{
    if (string.IsNullOrEmpty(request.CustomerName))
        return ValidationError.RequiredFieldMissing;
    
    if (request.Amount <= 0)
        return ValidationError.InvalidFormat;
    
    return Result<Order, ValidationError>.Success(order);
}
```

### Option&lt;T&gt; for Optional Values

```csharp
using HVO.Common.Options;

public Option<string> GetConfigValue(string key)
{
    if (_config.TryGetValue(key, out var value))
        return new Option<string>(value);
    
    return Option<string>.None();
}

// Usage
var config = GetConfigValue("ApiKey");
if (config.HasValue)
{
    Console.WriteLine($"API Key: {config.Value}");
}
```

### IOneOf Discriminated Unions

```csharp
using HVO.Common.OneOf;

public interface IPaymentResult : IOneOf { }

public class SuccessfulPayment : IPaymentResult
{
    public string TransactionId { get; set; } = "";
    public decimal Amount { get; set; }
    public object? Value => this;
    public Type? ValueType => typeof(SuccessfulPayment);
    public JsonElement? RawJson => null;
}

public class FailedPayment : IPaymentResult
{
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Value => this;
    public Type? ValueType => typeof(FailedPayment);
    public JsonElement? RawJson => null;
}

// Usage with pattern matching
var result = ProcessPayment(request);
if (result.Is<SuccessfulPayment>())
{
    var payment = result.As<SuccessfulPayment>();
    Console.WriteLine($"Success: {payment.TransactionId}");
}
else if (result.Is<FailedPayment>())
{
    var failure = result.As<FailedPayment>();
    Console.WriteLine($"Failed: {failure.ErrorCode}");
}
```

## Scope

This library contains **only general-purpose utilities and patterns** that are valuable across all HVO projects. Domain-specific code should reside in the appropriate project-specific libraries.

## License

MIT

## Features

- **Result&lt;T&gt; Pattern**: Railway-oriented programming for error handling
- **Option&lt;T&gt;**: Type-safe optional values
- **IOneOf Discriminated Unions**: Type-safe discriminated unions
- **Common Abstractions**: Shared interfaces and base types
- **Extension Methods**: Helpful extensions for common scenarios
- **Utilities**: Shared utility classes and helpers

## Project Structure

```
HVO.Enterprise.Common/
├── Results/              # Result<T> pattern for functional error handling
├── Options/              # Option<T> for type-safe optional values
├── OneOf/                # Discriminated unions (IOneOf, extensions, attributes)
├── Extensions/           # Extension methods (EnumExtensions, etc.)
├── Abstractions/         # Common interfaces and abstract types
└── Utilities/            # Shared utility classes
```

## Target Framework

- .NET Standard 2.0 (compatible with .NET Framework 4.6.1+ and all modern .NET versions)

## Installation

```bash
dotnet add package HVO.Enterprise.Common
```

## Usage

### Result&lt;T&gt; Pattern

```csharp
using HVO.Enterprise.Common.Results;

public Result<Order> CreateOrder(OrderRequest request)
{
    try
    {
        if (!IsValid(request))
            return Result<Order>.Failure(new ValidationException("Invalid request"));
        
        var order = new Order(request);
        return Result<Order>.Success(order);
    }
    catch (Exception ex)
    {
        return ex; // Implicit conversion to Result<Order>
    }
}

// Usage
var result = CreateOrder(request);
if (result.IsSuccessful)
{
    Console.WriteLine($"Order created: {result.Value.Id}");
}
else
{
    _logger.LogError(result.Error, "Failed to create order");
}
```

### Result&lt;T, TEnum&gt; with Typed Error Codes

```csharp
using HVO.Enterprise.Common.Results;
using HVO.Enterprise.Common.Extensions;
using System.ComponentModel;

public enum ValidationError
{
    [Description("Invalid input format")]
    InvalidFormat,
    
    [Description("Required field missing")]
    RequiredFieldMissing
}

public Result<Order, ValidationError> ValidateOrder(OrderRequest request)
{
    if (string.IsNullOrEmpty(request.CustomerName))
        return ValidationError.RequiredFieldMissing;
    
    if (request.Amount <= 0)
        return ValidationError.InvalidFormat;
    
    return Result<Order, ValidationError>.Success(order);
}
```

### Option&lt;T&gt; for Optional Values

```csharp
using HVO.Enterprise.Common.Options;

public Option<string> GetConfigValue(string key)
{
    if (_config.TryGetValue(key, out var value))
        return new Option<string>(value);
    
    return Option<string>.None();
}

// Usage
var config = GetConfigValue("ApiKey");
if (config.HasValue)
{
    Console.WriteLine($"API Key: {config.Value}");
}
```

### IOneOf Discriminated Unions

```csharp
using HVO.Enterprise.Common.OneOf;

public interface IPaymentResult : IOneOf { }

public class SuccessfulPayment : IPaymentResult
{
    public string TransactionId { get; set; } = "";
    public decimal Amount { get; set; }
    public object? Value => this;
    public Type? ValueType => typeof(SuccessfulPayment);
    public JsonElement? RawJson => null;
}

public class FailedPayment : IPaymentResult
{
    public string ErrorCode { get; set; } = "";
    public string Message { get; set; } = "";
    public object? Value => this;
    public Type? ValueType => typeof(FailedPayment);
    public JsonElement? RawJson => null;
}

// Usage with pattern matching
var result = ProcessPayment(request);
if (result.Is<SuccessfulPayment>())
{
    var payment = result.As<SuccessfulPayment>();
    Console.WriteLine($"Success: {payment.TransactionId}");
}
else if (result.Is<FailedPayment>())
{
    var failure = result.As<FailedPayment>();
    Console.WriteLine($"Failed: {failure.ErrorCode}");
}
```

## License

MIT
