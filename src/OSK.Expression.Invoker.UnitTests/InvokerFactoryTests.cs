using OSK.Expression.Invoker.UnitTests._Helpers;
using OSK.Expressions.Invoker;
using OSK.Expressions.Invoker.Models;
using System.Diagnostics;
using System.Reflection;

namespace OSK.Expression.Invoker.UnitTests;

public class InvokerFactoryTests
{
    #region Fields

    [Fact]
    public void InvokerFactory_Class_Public_FieldSetter()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyB = 1
        };

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyB);
        invoker.FastInvoke(testClass, [2]);

        // Assert
        Assert.Equal(2, testClass.PropertyB); 
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Field, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Internal_Property_GetterSetter()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyD = 5
        };

        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyD);

        // Act
        var originalValue = invoker.FastInvoke<int>(testClass);
        invoker.FastInvoke(testClass, [10]);

        // Assert
        Assert.Equal(5, originalValue);
        Assert.Equal(10, testClass.PropertyD);
    }

    [Fact]
    public void InvokerFactory_Class_Public_FieldGetter()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyB = 1
        };

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyB);
        var b = invoker.FastInvoke<int>(testClass);

        // Assert
        Assert.Equal(testClass.PropertyB, b);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Field, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Class_Private_FieldSetter()
    {
        // Arrange
        var testClass = new TestClass();
        testClass.SetC(5);
        Assert.Equal(5, testClass.PropertyC);

        var privateMember = typeof(TestClass).GetField("_propertyC", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var invoker = InvokerFactory.CreateInvoker(typeof(TestClass), privateMember!);
        invoker.FastInvoke(testClass, [2]);

        // Assert
        Assert.Equal(2, testClass.PropertyC);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Field, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Class_Private_FieldGetter()
    {
        // Arrange
        var testClass = new TestClass();
        testClass.SetC(5);

        var privateMember = typeof(TestClass).GetField("_propertyC", BindingFlags.NonPublic | BindingFlags.Instance);

        // Act
        var invoker = InvokerFactory.CreateInvoker(typeof(TestClass), privateMember!);
        var c = invoker.FastInvoke<int>(testClass);

        // Assert
        Assert.Equal(testClass.PropertyC, c);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Field, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    #endregion

    #region Properties

    [Fact]
    public void InvokerFactory_Class_Public_PropertySetter()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyA = 1
        };

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyA);
        invoker.FastInvoke(testClass, [2]);

        // Assert
        Assert.Equal(2, testClass.PropertyA);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Property, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Class_Public_PropertyGetter()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyA = 1
        };

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyA);
        var a = invoker.FastInvoke<int>(testClass);

        // Assert
        Assert.Equal(testClass.PropertyA, a);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Property, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Class_Public_PropertyGetterOnly()
    {
        // Arrange
        var testClass = new TestClass();

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.PropertyDGetter);
        var d = invoker.FastInvoke<int>(testClass);

        // Assert
        Assert.Equal(testClass.PropertyDGetter, d);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Property, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    #endregion

    #region Methods

    [Fact]
    public void InvokerFactory_Class_Method_Void()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyA = 12
        };
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.MethodA(0));

        // Act
        invoker.FastInvoke(testClass, [2]);

        // Assert
        Assert.Equal(2, testClass.PropertyA);
        Assert.Single(invoker.ParameterTypes); 
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(void), invoker.ReturnType);
        Assert.Equal(InvocationType.Method, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    [Fact]
    public void InvokerFactory_Class_Method_ReturnsOutput()
    {
        // Arrange
        var testClass = new TestClass()
        {
            PropertyA = 12
        };

        // Act
        var invoker = InvokerFactory.CreateInvoker<TestClass>(t => t.MethodB(0));
        var result = (int)invoker.FastInvoke(testClass, [2]);

        // Assert
        Assert.Equal(2, result);
        Assert.Single(invoker.ParameterTypes);
        Assert.Equal(typeof(int), invoker.ParameterTypes[0]);
        Assert.Equal(typeof(int), invoker.ReturnType);
        Assert.Equal(InvocationType.Method, invoker.InvocationType);
        Assert.Equal(typeof(TestClass), invoker.InvokeTargetType);
    }

    #endregion
}
