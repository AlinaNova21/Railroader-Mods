using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    [TestClass]
    public class UriFormatRuleTests
    {
        [TestMethod]
        public void Validate_ValidUri_ReturnsValid()
        {
            // Arrange
            var rule = new UriFormatRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://scene/GameObject1/Child", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_InvalidUriFormat_ReturnsInvalid()
        {
            // Arrange
            var rule = new UriFormatRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("not-a-valid-uri", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("uri"));
        }

        [TestMethod]
        public void Validate_EmptyString_ReturnsValid()
        {
            // Arrange - Empty URIs might be allowed in some contexts
            var rule = new UriFormatRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("", context);

            // Assert
            // This depends on implementation - empty might be valid for optional fields
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NullValue_ReturnsValid()
        {
            // Arrange
            var rule = new UriFormatRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }
    }

    [TestClass]
    public class UriSchemeValidationRuleTests
    {
        [TestMethod]
        public void Validate_SupportedScheme_ReturnsValid()
        {
            // Arrange
            var rule = new UriSchemeValidationRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://scene/GameObject", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_UnsupportedScheme_ReturnsInvalid()
        {
            // Arrange
            var rule = new UriSchemeValidationRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("http://example.com", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("scheme"));
        }

        [TestMethod]
        public void Validate_NoScheme_ReturnsInvalid()
        {
            // Arrange
            var rule = new UriSchemeValidationRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("just-a-path", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_CaseInsensitiveScheme_ReturnsValid()
        {
            // Arrange
            var rule = new UriSchemeValidationRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("PATH://scene/GameObject", context);

            // Assert
            // This depends on implementation - schemes are typically case-insensitive
            Assert.IsTrue(result.IsValid);
        }
    }

    [TestClass]
    public class PathUriRuleTests
    {
        [TestMethod]
        public void Validate_ValidPathUri_ReturnsValid()
        {
            // Arrange
            var rule = new PathUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://scene/RootObject/ChildObject", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_InvalidPathUriScheme_ReturnsInvalid()
        {
            // Arrange
            var rule = new PathUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("scenery://some/path", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_PathUriWithInvalidFormat_ReturnsInvalid()
        {
            // Arrange
            var rule = new PathUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_PathUriWithSpecialCharacters_ReturnsValid()
        {
            // Arrange
            var rule = new PathUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://scene/Object_With-Special.Characters", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }
    }

    [TestClass]
    public class SceneryUriRuleTests
    {
        [TestMethod]
        public void Validate_ValidSceneryUri_ReturnsValid()
        {
            // Arrange
            var rule = new SceneryUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("scenery://tree_oak_large", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_InvalidSceneryUriScheme_ReturnsInvalid()
        {
            // Arrange
            var rule = new SceneryUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://scene/tree", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_SceneryUriWithPath_ReturnsInvalid()
        {
            // Arrange
            var rule = new SceneryUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("scenery://category/tree_oak", context);

            // Assert
            // Scenery URIs typically don't have paths, just identifiers
            Assert.IsFalse(result.IsValid);
        }
    }

    [TestClass]
    public class VanillaPrefabRuleTests
    {
        [TestMethod]
        public void Validate_PrefabInWhitelist_ReturnsValid()
        {
            // Arrange
            var allowedPrefabs = new[] { "locomotive_steam_small", "car_boxcar", "car_passenger" };
            var rule = new VanillaPrefabRule(allowedPrefabs);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("vanilla://car_boxcar", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_PrefabNotInWhitelist_ReturnsInvalid()
        {
            // Arrange
            var allowedPrefabs = new[] { "locomotive_steam_small", "car_boxcar", "car_passenger" };
            var rule = new VanillaPrefabRule(allowedPrefabs);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("vanilla://unknown_prefab", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Message.ToLower().Contains("prefab"));
        }

        [TestMethod]
        public void Validate_InvalidVanillaUriScheme_ReturnsInvalid()
        {
            // Arrange
            var allowedPrefabs = new[] { "locomotive_steam_small" };
            var rule = new VanillaPrefabRule(allowedPrefabs);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://locomotive_steam_small", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void Validate_CaseSensitivePrefabName_ReturnsInvalid()
        {
            // Arrange
            var allowedPrefabs = new[] { "locomotive_steam_small" };
            var rule = new VanillaPrefabRule(allowedPrefabs);
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("vanilla://LOCOMOTIVE_STEAM_SMALL", context);

            // Assert
            // Prefab names are typically case-sensitive
            Assert.IsFalse(result.IsValid);
        }
    }

    [TestClass]
    public class EmptyUriRuleTests
    {
        [TestMethod]
        public void Validate_EmptyString_ReturnsValid()
        {
            // Arrange
            var rule = new EmptyUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("", context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NullValue_ReturnsValid()
        {
            // Arrange
            var rule = new EmptyUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate(null, context);

            // Assert
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void Validate_NonEmptyValue_ReturnsInvalid()
        {
            // Arrange
            var rule = new EmptyUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("path://something", context);

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_WhitespaceValue_ReturnsInvalid()
        {
            // Arrange
            var rule = new EmptyUriRule();
            var context = new ValidationContext();

            // Act
            var result = rule.Validate("   ", context);

            // Assert
            Assert.IsFalse(result.IsValid);
        }
    }