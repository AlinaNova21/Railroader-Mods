using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AlinasMapMod.Validation;
using AlinasMapMod.Tests.Mocks;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class CacheValidationRuleTests
    {
        private Mock<IMockCache<string>> mockCache;
        private ValidationContext context;

        [TestInitialize]
        public void Setup()
        {
            mockCache = new Mock<IMockCache<string>>();
            context = new ValidationContext();
        }

        [TestMethod]
        public void Validate_KeyExistsInCache_ReturnsValid()
        {
            // Arrange
            mockCache.Setup(c => c.Contains("existing_key")).Returns(true);
            var rule = new CacheValidationRule<string>(key => mockCache.Object.Contains(key), "TestCache");

            // Act
            var result = rule.Validate("existing_key", context);

            // Assert
            Assert.IsTrue(result.IsValid);
            mockCache.Verify(c => c.Contains("existing_key"), Times.Once);
        }

        [TestMethod]
        public void Validate_KeyDoesNotExistInCache_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Contains("missing_key")).Returns(false);
            var rule = new CacheValidationRule<string>(key => mockCache.Object.Contains(key), "TestCache");

            // Act
            var result = rule.Validate("missing_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("cache"));
            mockCache.Verify(c => c.Contains("missing_key"), Times.Once);
        }

        [TestMethod]
        public void Validate_NullKey_ReturnsInvalid()
        {
            // Arrange
            var rule = new CacheValidationRule<string>(key => mockCache.Object.Contains(key), "TestCache");

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            // Should not call cache.Contains for null values
            mockCache.Verify(c => c.Contains(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        public void Validate_EmptyKey_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Contains("")).Returns(false);
            var rule = new CacheValidationRule<string>(key => mockCache.Object.Contains(key), "TestCache");

            // Act
            var result = rule.Validate("", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_CacheThrowsException_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Contains("error_key")).Throws(new InvalidOperationException("Cache error"));
            var rule = new CacheValidationRule<string>(key => mockCache.Object.Contains(key), "TestCache");

            // Act
            var result = rule.Validate("error_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("error"));
        }
    }

    [TestClass]
    public class CacheTypeValidationRuleTests
    {
        private Mock<IMockCache<object>> mockCache;
        private ValidationContext context;

        [TestInitialize]
        public void Setup()
        {
            mockCache = new Mock<IMockCache<object>>();
            context = new ValidationContext();
        }

        [TestMethod]
        public void Validate_CachedObjectHasCorrectType_ReturnsValid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("test_key")).Returns("test_string");
            var rule = new CacheTypeValidationRule<string>(key => mockCache.Object.Get(key), "TestCache");

            // Act
            var result = rule.Validate("test_key", context);

            // Assert
            Assert.IsTrue(result.IsValid);
            mockCache.Verify(c => c.Get("test_key"), Times.Once);
        }

        [TestMethod]
        public void Validate_CachedObjectHasWrongType_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("test_key")).Returns(42); // int instead of string
            var rule = new CacheTypeValidationRule<string>(key => mockCache.Object.Get(key), "TestCache");

            // Act
            var result = rule.Validate("test_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("type"));
        }

        [TestMethod]
        public void Validate_KeyNotInCache_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("missing_key")).Returns((object)null);
            var rule = new CacheTypeValidationRule<string>(key => mockCache.Object.Get(key), "TestCache");

            // Act
            var result = rule.Validate("missing_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            mockCache.Verify(c => c.Get("missing_key"), Times.Once);
        }

        [TestMethod]
        public void Validate_InheritedType_ReturnsValid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("test_key")).Returns("test_string");
            var rule = new CacheTypeValidationRule<object>(key => mockCache.Object.Get(key), "TestCache"); // object is base type of string

            // Act
            var result = rule.Validate("test_key", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NullType_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("test_key")).Returns((object)null);
            var rule = new CacheTypeValidationRule<string>(key => mockCache.Object.Get(key), "TestCache");

            // Act
            var result = rule.Validate("test_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_GetCachedTypeThrowsException_ReturnsInvalid()
        {
            // Arrange
            mockCache.Setup(c => c.Get("test_key")).Throws(new InvalidOperationException("Cache access error"));
            var rule = new CacheTypeValidationRule<string>(key => mockCache.Object.Get(key), "TestCache");

            // Act
            var result = rule.Validate("test_key", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("error"));
        }
    }