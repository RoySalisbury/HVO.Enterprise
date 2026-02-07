# HVO.Common

A .NET Standard 2.0 utility library providing functional programming patterns and common utilities for all HVO projects.

## Features

- **Result<T>** - Functional error handling without exceptions
- **Option<T>** - Type-safe optional values
- **OneOf<T1, T2, ...>** - Discriminated unions (sum types)
- **Extension Methods** - String, collection, and enum utilities
- **Guard & Ensure** - Input validation and runtime assertions

## Installation

```bash
dotnet add package HVO.Common
```

## Usage Examples

### Result<T> Pattern

Handle errors functionally without throwing exceptions:

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

// Usage
var result = GetCustomer(123);
if (result.IsSuccessful)
{
    Console.WriteLine($"Found: {result.Value.Name}");
}
else
{
    _logger.LogError(result.Error, "Failed to get customer");
}

// Or use pattern matching
var message = result.Match(
    success: customer => $"Found: {customer.Name}",
    failure: error => $"Error: {error?.Message}");
```

### Result<T, TEnum> with Typed Error Codes

```csharp
using HVO.Common.Results;
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
        return (ValidationError.InvalidFormat, "Amount must be positive");
    
    return Result<Order, ValidationError>.Success(order);
}

// Usage
var result = ValidateOrder(request);
if (result.IsSuccessful)
{
    ProcessOrder(result.Value);
}
else
{
    Console.WriteLine($"Error: {result.Error.Code} - {result.Error.Message}");
}
```

### Option<T> Pattern

Type-safe optional values:

```csharp
using HVO.Common.Options;

public Option<string> GetConfigValue(string key)
{
    return _config.TryGetValue(key, out var value)
        ? new Option<string>(value)
        : Option<string>.None();
}

// Usage with default
var apiKey = GetConfigValue("ApiKey")
    .GetValueOrDefault("default-key");

// Or with pattern matching
GetConfigValue("ApiKey").Match(
    onSome: key => Console.WriteLine($"API Key: {key}"),
    onNone: () => Console.WriteLine("No API key configured"));
```

### OneOf<T1, T2, ...> Discriminated Unions

Type-safe variant types:

```csharp
using HVO.Common.OneOf;

// Return different types from a single method
public OneOf<Customer, Guest, Anonymous> GetUser(string id)
{
    if (IsCustomer(id)) return GetCustomer(id);
    if (IsGuest(id)) return GetGuest(id);
    return new Anonymous();
}

// Pattern match on the result
var user = GetUser("123");
var greeting = user.Match(
    customer => $"Welcome back, {customer.Name}!",
    guest => "Welcome, guest!",
    anonymous => "Please sign in");

// Or use type checking
if (user.IsT1)
{
    var customer = user.AsT1;
    ProcessCustomer(customer);
}
```

### Extension Methods

#### String Extensions

```csharp
using HVO.Common.Extensions;

// Check for null or whitespace
if (input.IsNullOrWhiteSpace()) { }

// Truncate with suffix
var preview = longText.TruncateWithSuffix(100, "...");

// Remove all whitespace
var compact = "Hello World".RemoveWhitespace(); // "HelloWorld"

// Convert to enum
var color = "Red".ToEnum<Color>();

// Check multiple values
if (input.ContainsAny("error", "warning", "failure")) { }
```

#### Collection Extensions

```csharp
using HVO.Common.Extensions;

// Safe enumeration
foreach (var item in collection.OrEmpty()) { }

// Execute action for each
items.ForEach(item => Console.WriteLine(item));

// Chunk into batches
var batches = items.Chunk(100);

// Distinct by key
var unique = items.DistinctBy(x => x.Id);

// Find index
var index = items.IndexOf(x => x.Name == "Test");
```

#### Enum Extensions

```csharp
using HVO.Common.Extensions;

public enum Status
{
    [Description("Order is pending")]
    Pending,
    
    [Description("Order is complete")]
    Complete
}

var description = Status.Pending.GetDescription();
// Returns: "Order is pending"
```

### Guard Clauses

Input validation with informative exceptions:

```csharp
using HVO.Common.Utilities;

public class CustomerService
{
    public void UpdateCustomer(int id, string name, int age)
    {
        Guard.AgainstNegativeOrZero(id, 0); // Throws if id <= 0
        Guard.AgainstNullOrWhiteSpace(name); // Throws if null/empty
        Guard.AgainstOutOfRange(age, 0, 150); // Throws if not in range
    }
    
    public void ProcessItems(IEnumerable<Item> items)
    {
        Guard.AgainstNull(items);
        Guard.AgainstNullOrEmpty(items); // Throws if collection is empty
    }
}
```

### Ensure Assertions

Runtime state validation:

```csharp
using HVO.Common.Utilities;

public class OrderProcessor
{
    private Order? _currentOrder;
    
    public void ProcessOrder()
    {
        Ensure.NotNull(_currentOrder, "Order must be set before processing");
        Ensure.That(_currentOrder.Items.Any(), "Order must have items");
        Ensure.InRange(_currentOrder.Total, 0, 1000000, "order total");
        
        // Process order...
    }
}
```

## LINQ-Style Extensions

Both Result<T> and Option<T> support functional composition:

```csharp
// Map transforms the value
var result = GetCustomer(123)
    .Map(c => c.Name)
    .Map(name => name.ToUpper());

// Bind chains operations that return Result/Option
var result = GetCustomer(123)
    .Bind(customer => GetOrders(customer.Id))
    .Bind(orders => CalculateTotal(orders));

// Fluent actions
GetCustomer(123)
    .OnSuccess(c => _logger.LogInformation("Found customer {Name}", c.Name))
    .OnFailure(ex => _logger.LogError(ex, "Customer not found"));
```

## Target Frameworks

- .NET Standard 2.0 (for maximum compatibility)
- Compatible with .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+

## Zero Dependencies

HVO.Common has no external dependencies and can be used in any .NET project.

## License

See LICENSE file in the repository root

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
