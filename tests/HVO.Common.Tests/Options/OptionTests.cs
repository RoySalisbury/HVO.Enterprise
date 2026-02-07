using System;
using HVO.Common.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HVO.Common.Tests.Options;

[TestClass]
public class OptionTests
{
    [TestMethod]
    public void Option_WithValue_HasValue()
    {
        // Arrange & Act
        var option = new Option<int>(42);
        
        // Assert
        Assert.IsTrue(option.HasValue);
        Assert.AreEqual(42, option.Value);
    }

    [TestMethod]
    public void Option_None_HasNoValue()
    {
        // Arrange & Act
        var option = Option<int>.None();
        
        // Assert
        Assert.IsFalse(option.HasValue);
        Assert.AreEqual(default(int), option.Value);
    }

    [TestMethod]
    public void Option_ToString_ReturnsValue()
    {
        // Arrange
        var option = new Option<string>("test");
        
        // Act
        var result = option.ToString();
        
        // Assert
        Assert.AreEqual("test", result);
    }

    [TestMethod]
    public void Option_ToString_ReturnsNoneWhenEmpty()
    {
        // Arrange
        var option = Option<string>.None();
        
        // Act
        var result = option.ToString();
        
        // Assert
        Assert.AreEqual("<None>", result);
    }
}

[TestClass]
public class OptionExtensionsTests
{
    [TestMethod]
    public void Map_TransformsValueWhenPresent()
    {
        // Arrange
        var option = new Option<int>(42);
        
        // Act
        var mapped = option.Map(x => x.ToString());
        
        // Assert
        Assert.IsTrue(mapped.HasValue);
        Assert.AreEqual("42", mapped.Value);
    }

    [TestMethod]
    public void Map_ReturnsNoneWhenNoValue()
    {
        // Arrange
        var option = Option<int>.None();
        
        // Act
        var mapped = option.Map(x => x.ToString());
        
        // Assert
        Assert.IsFalse(mapped.HasValue);
    }

    [TestMethod]
    public void Bind_FlatMapsWhenValuePresent()
    {
        // Arrange
        var option = new Option<int>(42);
        
        // Act
        var bound = option.Bind(x => new Option<string>(x.ToString()));
        
        // Assert
        Assert.IsTrue(bound.HasValue);
        Assert.AreEqual("42", bound.Value);
    }

    [TestMethod]
    public void Bind_ReturnsNoneWhenNoValue()
    {
        // Arrange
        var option = Option<int>.None();
        
        // Act
        var bound = option.Bind(x => new Option<string>(x.ToString()));
        
        // Assert
        Assert.IsFalse(bound.HasValue);
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsValueWhenPresent()
    {
        // Arrange
        var option = new Option<int>(42);
        
        // Act
        var value = option.GetValueOrDefault(0);
        
        // Assert
        Assert.AreEqual(42, value);
    }

    [TestMethod]
    public void GetValueOrDefault_ReturnsDefaultWhenAbsent()
    {
        // Arrange
        var option = Option<int>.None();
        
        // Act
        var value = option.GetValueOrDefault(0);
        
        // Assert
        Assert.AreEqual(0, value);
    }

    [TestMethod]
    public void ToNullable_ReturnsValueWhenPresent()
    {
        // Arrange
        var option = new Option<string>("test");
        
        // Act
        var value = option.ToNullable();
        
        // Assert
        Assert.AreEqual("test", value);
    }

    [TestMethod]
    public void ToNullable_ReturnsNullWhenAbsent()
    {
        // Arrange
        var option = Option<string>.None();
        
        // Act
        var value = option.ToNullable();
        
        // Assert
        Assert.IsNull(value);
    }

    [TestMethod]
    public void ToOption_CreatesOptionFromNonNullValue()
    {
        // Arrange
        string value = "test";
        
        // Act
        var option = value.ToOption();
        
        // Assert
        Assert.IsTrue(option.HasValue);
        Assert.AreEqual("test", option.Value);
    }

    [TestMethod]
    public void ToOption_CreatesNoneFromNullValue()
    {
        // Arrange
        string? value = null;
        
        // Act
        var option = value.ToOption();
        
        // Assert
        Assert.IsFalse(option.HasValue);
    }
}
