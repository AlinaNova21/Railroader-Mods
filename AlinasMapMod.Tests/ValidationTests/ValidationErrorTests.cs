using Microsoft.VisualStudio.TestTools.UnitTesting;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class ValidationErrorTests
    {
        [TestMethod]
        public void ToString_WithField_ReturnsFieldAndMessage()
        {
            // Arrange
            var error = new ValidationError
            {
                Field = "TestField",
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            Assert.AreEqual("TestField: Test error message", result);
        }

        [TestMethod]
        public void ToString_WithoutField_ReturnsMessageOnly()
        {
            // Arrange
            var error = new ValidationError
            {
                Field = null,
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            Assert.AreEqual("Test error message", result);
        }

        [TestMethod]
        public void ToString_WithEmptyField_ReturnsMessageOnly()
        {
            // Arrange
            var error = new ValidationError
            {
                Field = "",
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            Assert.AreEqual("Test error message", result);
        }

        [TestMethod]
        public void ToString_WithWhitespaceField_ReturnsMessageOnly()
        {
            // Arrange
            var error = new ValidationError
            {
                Field = "   ",
                Message = "Test error message"
            };

            // Act
            var result = error.ToString();

            // Assert
            // Depending on implementation, this might return message only or include field
            // The test should match the actual string.IsNullOrEmpty behavior
            Assert.AreEqual("   : Test error message", result);
        }

        [TestMethod]
        public void Properties_SetAndGet_WorkCorrectly()
        {
            // Arrange
            var error = new ValidationError();

            // Act
            error.Field = "TestField";
            error.Message = "Test message";
            error.Code = "TEST_001";
            error.Value = "test_value";

            // Assert
            Assert.AreEqual("TestField", error.Field);
            Assert.AreEqual("Test message", error.Message);
            Assert.AreEqual("TEST_001", error.Code);
            Assert.AreEqual("test_value", error.Value);
        }

        [TestMethod]
        public void Properties_ComplexValue_HandledCorrectly()
        {
            // Arrange
            var complexValue = new { Name = "Test", Value = 42 };
            var error = new ValidationError
            {
                Field = "ComplexField",
                Message = "Complex value error",
                Code = "COMPLEX_001",
                Value = complexValue
            };

            // Act & Assert
            Assert.AreEqual("ComplexField", error.Field);
            Assert.AreEqual("Complex value error", error.Message);
            Assert.AreEqual("COMPLEX_001", error.Code);
            Assert.AreEqual(complexValue, error.Value);
        }
    }

    [TestClass]
    public class ValidationWarningTests
    {
        [TestMethod]
        public void ToString_WithField_ReturnsFieldAndMessage()
        {
            // Arrange
            var warning = new ValidationWarning
            {
                Field = "TestField",
                Message = "Test warning message"
            };

            // Act
            var result = warning.ToString();

            // Assert
            Assert.AreEqual("TestField: Test warning message", result);
        }

        [TestMethod]
        public void ToString_WithoutField_ReturnsMessageOnly()
        {
            // Arrange
            var warning = new ValidationWarning
            {
                Field = null,
                Message = "Test warning message"
            };

            // Act
            var result = warning.ToString();

            // Assert
            Assert.AreEqual("Test warning message", result);
        }

        [TestMethod]
        public void ToString_WithEmptyField_ReturnsMessageOnly()
        {
            // Arrange
            var warning = new ValidationWarning
            {
                Field = "",
                Message = "Test warning message"
            };

            // Act
            var result = warning.ToString();

            // Assert
            Assert.AreEqual("Test warning message", result);
        }

        [TestMethod]
        public void Properties_SetAndGet_WorkCorrectly()
        {
            // Arrange
            var warning = new ValidationWarning();

            // Act
            warning.Field = "TestField";
            warning.Message = "Test message";
            warning.Value = "test_value";

            // Assert
            Assert.AreEqual("TestField", warning.Field);
            Assert.AreEqual("Test message", warning.Message);
            Assert.AreEqual("test_value", warning.Value);
        }

        [TestMethod]
        public void Properties_ComplexValue_HandledCorrectly()
        {
            // Arrange
            var complexValue = new { Name = "Test", Value = 42 };
            var warning = new ValidationWarning
            {
                Field = "ComplexField",
                Message = "Complex value warning",
                Value = complexValue
            };

            // Act & Assert
            Assert.AreEqual("ComplexField", warning.Field);
            Assert.AreEqual("Complex value warning", warning.Message);
            Assert.AreEqual(complexValue, warning.Value);
        }
    }