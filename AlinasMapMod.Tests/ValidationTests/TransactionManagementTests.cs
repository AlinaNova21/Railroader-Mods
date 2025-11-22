using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AlinasMapMod.Validation;

namespace AlinasMapMod.Tests.ValidationTests;
    // Note: TransactionManagementTests disabled due to Unity dependency issues
    // BuilderTransaction.Rollback() requires UnityEngine.CoreModule which is not available in test context
    // These tests would need to run in Unity Test Runner instead
    // [TestClass] - Commented out to disable all tests in this class
    public class TransactionManagementTests
    {
        [TestMethod]
        public void SetCleanupAction_ValidAction_Succeeds()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var cleanupExecuted = false;

            // Act
            transaction.SetCleanupAction(() => cleanupExecuted = true);
            transaction.Rollback();

            // Assert
            Assert.IsTrue(cleanupExecuted);
        }

        [TestMethod]
        public void SetCleanupAction_NullAction_AllowsNull()
        {
            // Arrange
            var transaction = new BuilderTransaction();

            // Act & Assert - should not throw
            transaction.SetCleanupAction(null);
            transaction.Rollback(); // Should not throw even with null action
        }

        [TestMethod]
        public void Commit_PreventsCleanupExecution()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var cleanupExecuted = false;
            transaction.SetCleanupAction(() => cleanupExecuted = true);

            // Act
            transaction.Commit();
            transaction.Rollback();

            // Assert
            Assert.IsFalse(cleanupExecuted, "Cleanup should not execute after commit");
        }

        [TestMethod]
        public void Rollback_WithoutCommit_ExecutesCleanup()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var cleanupExecuted = false;
            transaction.SetCleanupAction(() => cleanupExecuted = true);

            // Act
            transaction.Rollback();

            // Assert
            Assert.IsTrue(cleanupExecuted);
        }

        [TestMethod]
        public void Rollback_AfterCommit_DoesNotExecuteCleanup()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var cleanupExecuted = false;
            transaction.SetCleanupAction(() => cleanupExecuted = true);

            // Act
            transaction.Commit();
            transaction.Rollback();

            // Assert
            Assert.IsFalse(cleanupExecuted);
        }

        [TestMethod]
        public void Rollback_MultipleCallsAfterCommit_DoesNotExecuteCleanup()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var cleanupExecuted = false;
            transaction.SetCleanupAction(() => cleanupExecuted = true);

            // Act
            transaction.Commit();
            transaction.Rollback();
            transaction.Rollback(); // Second call

            // Assert
            Assert.IsFalse(cleanupExecuted);
        }

        // Note: SetRootGameObject tests removed due to Unity dependency

        [TestMethod]
        public void CleanupExecution_WithException_ContinuesExecution()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var secondCleanupExecuted = false;
            
            // Setup cleanup that throws
            transaction.SetCleanupAction(() => 
            {
                throw new InvalidOperationException("Test exception");
            });

            // Act & Assert - should not throw despite cleanup exception
            transaction.Rollback();
        }

        // Note: GameObject-related tests removed due to Unity dependency

        [TestMethod]
        public void OverwriteCleanupAction_ReplacesOriginal()
        {
            // Arrange
            var transaction = new BuilderTransaction();
            var firstCleanupExecuted = false;
            var secondCleanupExecuted = false;

            // Act
            transaction.SetCleanupAction(() => firstCleanupExecuted = true);
            transaction.SetCleanupAction(() => secondCleanupExecuted = true);
            transaction.Rollback();

            // Assert
            Assert.IsFalse(firstCleanupExecuted, "First cleanup should not execute");
            Assert.IsTrue(secondCleanupExecuted, "Second cleanup should execute");
        }

        [TestMethod]
        public void InterfaceUsage_IBuilderTransaction_WorksCorrectly()
        {
            // Arrange
            IBuilderTransaction transaction = new BuilderTransaction();
            var cleanupExecuted = false;

            // Act
            transaction.SetCleanupAction(() => cleanupExecuted = true);
            transaction.Rollback();

            // Assert
            Assert.IsTrue(cleanupExecuted);
        }
    }