using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class RequiredRuleTests
    {
        [TestMethod]
        public void Validate_NonNullString_ReturnsValid()
        {
            // Arrange
            var rule = new RequiredRule<string>();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("test value", context);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_NullString_ReturnsInvalid()
        {
            // Arrange
            var rule = new RequiredRule<string>();
            var context = new ValidationContext { FieldName = "Value" };

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("Value is required", result.Errors[0].Message);
        }

        [TestMethod]
        public void Validate_EmptyString_ReturnsInvalid()
        {
            // Arrange
            var rule = new RequiredRule<string>();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_WhitespaceString_AllowWhitespace_ReturnsValid()
        {
            // Arrange
            var rule = new RequiredRule<string>(allowWhitespace: true);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("   ", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_WhitespaceString_DisallowWhitespace_ReturnsInvalid()
        {
            // Arrange
            var rule = new RequiredRule<string>(allowWhitespace: false);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("   ", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_NonNullObject_ReturnsValid()
        {
            // Arrange
            var rule = new RequiredRule<object>();
            var context = new ValidationContext();
            var testObject = new object();

            // Act
            var result = rule.Validate(testObject, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NullObject_ReturnsInvalid()
        {
            // Arrange
            var rule = new RequiredRule<object>();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }
    }

    [TestClass]
    public class WhitelistRuleTests
    {
        [TestMethod]
        public void Validate_ValueInWhitelist_ReturnsValid()
        {
            // Arrange
            var allowedValues = new[] { "apple", "banana", "cherry" };
            var rule = new WhitelistRule<string>(allowedValues);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("banana", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_ValueNotInWhitelist_ReturnsInvalid()
        {
            // Arrange
            var allowedValues = new[] { "apple", "banana", "cherry" };
            var rule = new WhitelistRule<string>(allowedValues);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("grape", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("allowed values"));
        }

        [TestMethod]
        public void Validate_NullValue_WhitelistContainsNull_ReturnsValid()
        {
            // Arrange
            var allowedValues = new string[] { "apple", null, "cherry" };
            var rule = new WhitelistRule<string>(allowedValues);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NullValue_WhitelistDoesNotContainNull_ReturnsInvalid()
        {
            // Arrange
            var allowedValues = new[] { "apple", "banana", "cherry" };
            var rule = new WhitelistRule<string>(allowedValues);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_EmptyWhitelist_ReturnsInvalid()
        {
            // Arrange
            var allowedValues = new string[0];
            var rule = new WhitelistRule<string>(allowedValues);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("anything", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }
    }

    [TestClass]
    public class CustomRuleTests
    {
        [TestMethod]
        public void Validate_CustomValidationPasses_ReturnsValid()
        {
            // Arrange
            var rule = new CustomRule<int>((value, context) => 
            {
                var result = new ValidationResult { IsValid = value > 0 };
                if (value <= 0)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = context.FieldName,
                        Message = "Value must be positive",
                        Code = "INVALID_VALUE",
                        Value = value
                    });
                }
                return result;
            });
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(5, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_CustomValidationFails_ReturnsInvalid()
        {
            // Arrange
            var rule = new CustomRule<int>((value, context) => 
            {
                var result = new ValidationResult { IsValid = value > 0 };
                if (value <= 0)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Field = context.FieldName,
                        Message = "Value must be positive",
                        Code = "INVALID_VALUE",
                        Value = value
                    });
                }
                return result;
            });
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(-5, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.AreEqual("Value must be positive", result.Errors[0].Message);
        }

        [TestMethod]
        public void Validate_CustomValidationWithContext_PassesContext()
        {
            // Arrange
            ValidationContext passedContext = null;
            var rule = new CustomRule<string>((value, context) => 
            {
                passedContext = context;
                return new ValidationResult { IsValid = true };
            });
            var originalContext = new ValidationContext { FieldName = "TestField" };

            // Act
            var result = rule.Validate("test", originalContext);

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(originalContext, passedContext);
            Assert.AreEqual("TestField", passedContext.FieldName);
        }
    }

    [TestClass]
    public class MinValueRuleTests
    {
        [TestMethod]
        public void Validate_ValueAboveMinimum_ReturnsValid()
        {
            // Arrange
            var rule = new MinValueRule<int>(5);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(10, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_ValueEqualToMinimum_ReturnsValid()
        {
            // Arrange
            var rule = new MinValueRule<int>(5);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(5, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_ValueBelowMinimum_ReturnsInvalid()
        {
            // Arrange
            var rule = new MinValueRule<int>(5);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(3, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("minimum"));
        }

        [TestMethod]
        public void Validate_DecimalValues_WorksCorrectly()
        {
            // Arrange
            var rule = new MinValueRule<decimal>(5.5m);
            var context = new ValidationContext();

            // Act & Assert
            Assert.IsTrue(rule.Validate(5.6m, context).IsValid);
            Assert.IsTrue(rule.Validate(5.5m, context).IsValid);
            Assert.IsFalse(rule.Validate(5.4m, context).IsValid);
        }
    }

    [TestClass]
    public class EnumValidationRuleTests
    {
        private enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }

        [TestMethod]
        public void Validate_ValidEnumValue_ReturnsValid()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate((int)TestEnum.Value2, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_InvalidEnumValue_ReturnsInvalid()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var context = new ValidationContext();
            var invalidValue = (TestEnum)999; // Invalid enum value

            // Act
            var result = rule.Validate((int)invalidValue, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("valid"));
        }

        [TestMethod]
        public void Validate_DefaultEnumValue_ReturnsValid()
        {
            // Arrange
            var rule = new EnumValidationRule<TestEnum>();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate((int)default(TestEnum), context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }
    }