using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AlinasMapMod.Validation;
using AlinasMapMod.Tests.Mocks;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class FluentValidationExtensionsTests
    {
        [TestMethod]
        public void AsUri_ValidUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUri();

            // Act
            var result = builder.Validate("path://scene/GameObject");

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void AsUri_InvalidUri_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUri();

            // Act
            var result = builder.Validate("not-a-uri");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("://"));
        }

        [TestMethod]
        public void AsUriScheme_ValidPathUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUriScheme();

            // Act
            var result = builder.Validate("path://scene/GameObject");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsUriScheme_ValidSceneryUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUriScheme();

            // Act
            var result = builder.Validate("scenery://myScenery");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsUriScheme_ValidVanillaUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUriScheme();

            // Act
            var result = builder.Validate("vanilla://prefab/path");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsUriScheme_ValidEmptyUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUriScheme();

            // Act
            var result = builder.Validate("empty://");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsUriScheme_InvalidScheme_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsUriScheme();

            // Act
            var result = builder.Validate("invalid://something");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("scheme"));
        }

        [TestMethod]
        public void AsPathUri_ValidPathUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsPathUri();

            // Act
            var result = builder.Validate("path://scene/GameObject/Child");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsPathUri_InvalidPathUri_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsPathUri();

            // Act
            var result = builder.Validate("path://invalid");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void AsSceneryUri_ValidSceneryUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsSceneryUri();

            // Act
            var result = builder.Validate("scenery://myIdentifier");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsSceneryUri_InvalidSceneryUri_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsSceneryUri();

            // Act
            var result = builder.Validate("scenery://");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void AsEmptyUri_ValidEmptyUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsEmptyUri();

            // Act
            var result = builder.Validate("empty://anything");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsGameObjectUri_ValidPathUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsGameObjectUri();

            // Act
            var result = builder.Validate("path://scene/GameObject");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsGameObjectUri_ValidSceneryUri_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsGameObjectUri();

            // Act
            var result = builder.Validate("scenery://myScenery");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsGameObjectUri_ValidVanillaUri_WithAllowedPrefabs_PassesValidation()
        {
            // Arrange
            var allowedPrefabs = new[] { "Locomotive", "Car", "Track" };
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsGameObjectUri(allowedPrefabs);

            // Act
            var result = builder.Validate("vanilla://Locomotive/part");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsGameObjectUri_InvalidVanillaUri_WithAllowedPrefabs_FailsValidation()
        {
            // Arrange
            var allowedPrefabs = new[] { "Locomotive", "Car", "Track" };
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsGameObjectUri(allowedPrefabs);

            // Act
            var result = builder.Validate("vanilla://InvalidPrefab/part");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void AsVanillaPrefab_ValidPrefab_PassesValidation()
        {
            // Arrange
            var allowedPrefabs = new[] { "Locomotive", "Car", "Track" };
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsVanillaPrefab(allowedPrefabs);

            // Act
            var result = builder.Validate("vanilla://Locomotive/part");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsVanillaPrefab_InvalidPrefab_FailsValidation()
        {
            // Arrange
            var allowedPrefabs = new[] { "Locomotive", "Car", "Track" };
            var builder = new ValidationBuilder<string>("TestUri");
            builder.AsVanillaPrefab(allowedPrefabs);

            // Act
            var result = builder.Validate("vanilla://InvalidPrefab/part");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("prefab"));
        }

        [TestMethod]
        public void GreaterThan_ValidValue_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestValue");
            builder.GreaterThan(10);

            // Act
            var result = builder.Validate(15);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void GreaterThan_InvalidValue_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestValue");
            builder.GreaterThan(10);

            // Act
            var result = builder.Validate(5);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.Contains("greater"));
        }

        [TestMethod]
        public void GreaterThanOrEqual_ValidValue_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestValue");
            builder.GreaterThanOrEqual(10);

            // Act
            var result = builder.Validate(10);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void GreaterThanOrEqual_InvalidValue_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestValue");
            builder.GreaterThanOrEqual(10);

            // Act
            var result = builder.Validate(9);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void AsValidEnum_ValidEnumValue_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestEnum");
            builder.AsValidEnum<TestEnum>();

            // Act
            var result = builder.Validate((int)TestEnum.Value2);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void AsValidEnum_InvalidEnumValue_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<int>("TestEnum");
            builder.AsValidEnum<TestEnum>();

            // Act
            var result = builder.Validate(999);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void RequiredNotWhitespace_ValidValue_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.RequiredNotWhitespace();

            // Act
            var result = builder.Validate("Valid value");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void RequiredNotWhitespace_WhitespaceValue_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.RequiredNotWhitespace();

            // Act
            var result = builder.Validate("   ");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void ChainedValidation_AllPass_PassesValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required().AsUri();

            // Act
            var result = builder.Validate("path://scene/GameObject");

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void ChainedValidation_FirstFails_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required().AsUri();

            // Act
            var result = builder.Validate(null);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void ChainedValidation_SecondFails_FailsValidation()
        {
            // Arrange
            var builder = new ValidationBuilder<string>("TestField");
            builder.Required().AsUri();

            // Act
            var result = builder.Validate("not-a-uri");

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        public enum TestEnum
        {
            Value1,
            Value2,
            Value3
        }
    }