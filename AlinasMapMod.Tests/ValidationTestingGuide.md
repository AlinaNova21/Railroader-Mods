# Validation Framework Testing Guide

## Overview

This document outlines the comprehensive unit testing strategy for the AlinasMapMod validation framework. The tests are designed to validate the framework's core functionality while acknowledging the limitations of testing game-dependent components outside the Unity environment.

## Test Structure

### Core Test Categories

1. **Interface Tests** (`IValidatableTests.cs`)
   - Tests the dual validation modes (simple vs. rich)
   - Verifies backward compatibility between validation approaches
   - Ensures consistent behavior across validation methods

2. **Result System Tests** (`ValidationResultTests.cs`)
   - Tests `ValidationResult` success/failure states
   - Validates error and warning collection management
   - Tests result combination logic and legacy bridge functionality

3. **Error/Warning Tests** (`ValidationErrorTests.cs`)
   - Tests `ValidationError` and `ValidationWarning` classes
   - Validates field context, error codes, and value handling
   - Tests string representation and edge cases

4. **Validation Rules Tests** (`CommonValidationRulesTests.cs`)
   - Tests core validation rules: Required, Whitelist, Custom, MinValue, Enum
   - Validates rule behavior with various input types
   - Tests parameter validation and error message generation

5. **URI Validation Tests** (`UriValidationTests.cs`)
   - Tests URI format validation and scheme validation
   - Tests game-specific URI types (path, scenery, vanilla prefab)
   - Uses mock data to test string parsing logic without Unity dependencies

6. **Cache Validation Tests** (`CacheValidationTests.cs`)
   - Tests cache existence and type validation rules
   - Uses mocked cache interfaces to avoid Unity dependencies
   - Tests error handling and edge cases

7. **Fluent Extensions Tests** (`FluentValidationExtensionsTests.cs`)
   - Tests fluent API method chaining
   - Tests both game-independent and game-dependent extensions
   - Uses mocks for game-specific validation components

8. **Transaction Management Tests** (`TransactionManagementTests.cs`)
   - Tests commit/rollback patterns
   - Tests automatic cleanup on validation failure
   - Tests resource management and exception handling

9. **Edge Cases & Legacy Tests** (`EdgeCasesAndLegacyTests.cs`)
   - Tests backward compatibility with legacy validation
   - Tests performance with large validation chains
   - Tests error handling with null/invalid inputs

## Testing Strategy

### Mockable Components

The following game-dependent components are tested using mock interfaces:

- **`IMockCache<T>`** - Simulates game object caches
- **`IMockPrefabRegistry`** - Simulates prefab validation
- **`IMockGameObjectResolver`** - Simulates GameObject URI resolution
- **`IMockSceneManager`** - Simulates scene validation
- **`IMockAssetValidator`** - Simulates asset validation

### Pure Logic Testing

The following components are tested directly without Unity dependencies:

- Core validation interfaces and base classes
- Validation result aggregation and error handling
- String-based validation rules (Required, Whitelist, Custom)
- Numeric validation rules (MinValue, ranges)
- URI format parsing and scheme validation
- Fluent API method chaining and parameter validation

### Integration Testing Approach

While unit tests cover the validation logic, the following areas require in-game integration testing:

1. **GameObject URI Resolution**
   - Actual GameObject path validation in Unity scenes
   - Runtime GameObject existence verification
   - Scene context validation

2. **Game Cache Integration**
   - Real cache lookup and type validation
   - Cache performance under load
   - Cache consistency validation

3. **Prefab Validation**
   - Real prefab registry lookup
   - Asset availability validation
   - Prefab loading and instantiation

## Test Coverage Areas

### ✅ Fully Testable (Unit Tests)
- Core validation interfaces and contracts
- Validation result aggregation and error handling
- String and numeric validation rules
- URI format parsing and scheme validation
- Fluent API method chaining
- Transaction management logic
- Error handling and edge cases
- Backward compatibility

### ⚠️ Partially Testable (Mocked)
- Cache validation logic (mocked cache interfaces)
- Prefab validation logic (mocked prefab registry)
- URI scheme validation (mocked resolution)
- Asset validation (mocked asset interfaces)

### ❌ Requires In-Game Testing
- GameObject URI resolution in Unity scenes
- Real game cache performance and consistency
- Actual prefab loading and validation
- Runtime scene context validation
- Unity-specific error handling

## Running the Tests

### Prerequisites
- .NET Framework 4.8 targeting pack
- MSTest framework
- Moq library for mocking

### Test Execution
```bash
# Build the test project
dotnet build AlinasMapMod.Tests/AlinasMapMod.Tests.csproj

# Run all tests
dotnet test AlinasMapMod.Tests/AlinasMapMod.Tests.csproj

# Run specific test category
dotnet test --filter "TestCategory=ValidationCore"
```

### Test Organization
Tests are organized into logical categories:
- `ValidationTests/` - Core validation functionality
- `Mocks/` - Mock interfaces and test doubles
- Each test class focuses on a specific component or functionality area

## Recommendations for In-Game Testing

1. **Create Integration Test Mod**
   - Develop a separate mod specifically for validation testing
   - Test validation with real game objects and scenes
   - Validate performance under actual game conditions

2. **Validation Debugging Tools**
   - Add logging and debugging output for validation failures
   - Create in-game UI for validation result inspection
   - Add performance monitoring for validation operations

3. **Automated Scene Testing**
   - Create test scenes with known validation scenarios
   - Implement automated validation testing during mod loading
   - Add validation health checks for critical game components

4. **Error Reporting Integration**
   - Integrate validation errors with game's error reporting system
   - Add telemetry for validation performance and failure rates
   - Create user-friendly error messages for validation failures

## Future Enhancements

1. **Property-Based Testing**
   - Add property-based tests for validation rules
   - Test validation with random/generated inputs
   - Validate invariants across different input combinations

2. **Performance Benchmarking**
   - Add benchmarks for validation performance
   - Test memory usage and allocation patterns
   - Profile validation under stress conditions

3. **Extended Mock Scenarios**
   - Add more complex mock scenarios for game integration
   - Test validation in simulated error conditions
   - Add concurrent validation testing with mocks

This testing approach provides comprehensive coverage of the validation framework's logic while acknowledging the practical limitations of testing Unity-dependent components outside the game environment.