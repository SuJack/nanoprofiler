﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using EF.Diagnostics.Profiling.Data;
using EF.Diagnostics.Profiling.Timings;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace EF.Diagnostics.Profiling.Tests.Data
{
    [TestFixture]
    public class ProfiledDbCommandTest
    {
        [Test]
        public void TestProfiledDbCommandWithIDbCommand()
        {
            var mockCommand = new Mock<IDbCommand>();
            var mockDbProfiler = new Mock<IDbProfiler>();

            var tags = new TagCollection(new[] { "test" });
            var target = new ProfiledDbCommand(mockCommand.Object, mockDbProfiler.Object, tags) as IDbCommand;

            // test Cancel()
            var cancelCalled = false;
            mockCommand.Setup(cmd => cmd.Cancel()).Callback(() => cancelCalled = true);
            target.Cancel();
            Assert.IsTrue(cancelCalled);

            // test CommandText
            var sql = "test sql;";
            var sql2 = "test sql 2";
            var commandTextSet = false;
            mockCommand.Setup(cmd => cmd.CommandText).Returns(sql);
            mockCommand.SetupSet(cmd => cmd.CommandText = It.IsAny<string>()).Callback<string>(a =>
            {
                Assert.AreEqual(sql2, a);
                commandTextSet = true;
            });
            Assert.AreEqual(sql, target.CommandText);
            target.CommandText = sql2;
            Assert.IsTrue(commandTextSet);

            // test CommandTimeout
            var timeout1 = 1;
            var timeout2 = 2;
            var commandTimeoutSet = false;
            mockCommand.Setup(cmd => cmd.CommandTimeout).Returns(timeout1);
            mockCommand.SetupSet(cmd => cmd.CommandTimeout = It.IsAny<int>()).Callback<int>(a =>
            {
                Assert.AreEqual(timeout2, a);
                commandTimeoutSet = true;
            });
            Assert.AreEqual(timeout1, target.CommandTimeout);
            target.CommandTimeout = timeout2;
            Assert.IsTrue(commandTimeoutSet);

            // test CommandType
            var cmdType1 = CommandType.StoredProcedure;
            var cmdType2 = CommandType.Text;
            var commandTypeSet = false;
            mockCommand.Setup(cmd => cmd.CommandType).Returns(cmdType1);
            mockCommand.SetupSet(cmd => cmd.CommandType = It.IsAny<CommandType>()).Callback<CommandType>(a =>
            {
                Assert.AreEqual(cmdType2, a);
                commandTypeSet = true;
            });
            Assert.AreEqual(cmdType1, target.CommandType);
            target.CommandType = cmdType2;
            Assert.IsTrue(commandTypeSet);

            // test CreateDbParameter()
            var mockParameter = new Mock<DbParameter>();
            var parameterName = "p1";
            mockParameter.Setup(p => p.ParameterName).Returns(parameterName);
            mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(mockParameter.Object);
            var parameter = target.CreateParameter();
            Assert.AreNotEqual(mockParameter.Object, parameter);
            Assert.AreEqual(parameterName, parameter.ParameterName);

            // test DbConnection
            var mockConnection = new Mock<IDbConnection>();
            var connStr = "test conn str";
            mockConnection.Setup(c => c.ConnectionString).Returns(connStr);
            mockCommand.Setup(cmd => cmd.Connection).Returns(mockConnection.Object);
            var connection = target.Connection;
            Assert.AreEqual(mockConnection.Object, connection);
            Assert.AreEqual(connection, target.Connection);
            Assert.AreEqual(connStr, connection.ConnectionString);

            // test DbParameterCollection
            Assert.IsNull(target.Parameters);
            // ReSharper disable HeuristicUnreachableCode
            var mockParameterCollection = new Mock<IDataParameterCollection>();
            mockParameterCollection.Setup(c => c.Count).Returns(1);
            mockCommand.Setup(cmd => cmd.Parameters).Returns(mockParameterCollection.Object);
            var parameterCollection = target.Parameters;
            Assert.IsFalse(ReferenceEquals(mockParameterCollection.Object, parameterCollection));
            Assert.AreEqual(parameterCollection, target.Parameters);
            Assert.AreEqual(mockParameterCollection.Object.Count, parameterCollection.Count);

            // test DbTransaction
            var mockTransaction = new Mock<IDbTransaction>();
            var isoLevel = IsolationLevel.Chaos;
            mockTransaction.Setup(t => t.IsolationLevel).Returns(isoLevel);
            mockCommand.Setup(cmd => cmd.Transaction).Returns(mockTransaction.Object);
            var transaction = target.Transaction;
            Assert.AreEqual(mockTransaction.Object, transaction);
            Assert.AreEqual(transaction, target.Transaction);
            Assert.AreEqual(isoLevel, transaction.IsolationLevel);

            //test ExecuteDbDataReader()
            var mockReader = new Mock<IDataReader>();
            mockReader.Setup(r => r.Depth).Returns(1);
            var executeDbCommandCalled = false;
            mockDbProfiler.Setup(p => p.ExecuteDbCommand(It.IsAny<DbExecuteType>(), It.IsAny<IDbCommand>(), It.IsAny<Func<IDataReader>>(), It.IsAny<TagCollection>()))
                .Callback<DbExecuteType, IDbCommand, Func<IDataReader>, IEnumerable<string>>((a, b, c, d) =>
                {
                    Assert.AreEqual(mockCommand.Object, b);
                    Assert.AreEqual(tags.First(), d.First());
                    c();
                    executeDbCommandCalled = true;
                });
            var cmdBehavior = CommandBehavior.CloseConnection;
            mockCommand.Setup(c => c.ExecuteReader(cmdBehavior)).Returns(mockReader.Object);
            var reader = target.ExecuteReader(cmdBehavior);
            Assert.IsTrue(executeDbCommandCalled);
            Assert.AreNotEqual(mockReader.Object, reader);
            Assert.AreEqual(1, reader.Depth);

            // test ExecuteNonQuery()
            executeDbCommandCalled = false;
            var executeNonQueryCalled = false;
            var executeNonQueryResult = 1;
            mockCommand.Setup(c => c.ExecuteNonQuery()).Callback(() => executeNonQueryCalled = true).Returns(executeNonQueryResult);
            Assert.AreEqual(executeNonQueryResult, target.ExecuteNonQuery());
            Assert.IsTrue(executeDbCommandCalled);
            Assert.IsTrue(executeNonQueryCalled);

            // test ExecuteScalar()
            executeDbCommandCalled = false;
            var executeScalarCalled = false;
            var executeScalarResult = new object();
            mockCommand.Setup(c => c.ExecuteScalar()).Callback(() => executeScalarCalled = true).Returns(executeScalarResult);
            Assert.AreEqual(executeScalarResult, target.ExecuteScalar());
            Assert.IsTrue(executeDbCommandCalled);
            Assert.IsTrue(executeScalarCalled);

            // test Prepare()
            var prepareCalled = false;
            mockCommand.Setup(c => c.Prepare()).Callback(() => prepareCalled = true);
            target.Prepare();
            Assert.IsTrue(prepareCalled);

            // test UpdatedRowSource
            var updateRowSource1 = UpdateRowSource.Both;
            var updateRowSource2 = UpdateRowSource.FirstReturnedRecord;
            var updateRowSourceCalled = false;
            mockCommand.Setup(cmd => cmd.UpdatedRowSource).Returns(updateRowSource1);
            mockCommand.SetupSet(cmd => cmd.UpdatedRowSource = It.IsAny<UpdateRowSource>()).Callback<UpdateRowSource>(a =>
            {
                Assert.AreEqual(updateRowSource2, a);
                updateRowSourceCalled = true;
            });
            Assert.AreEqual(updateRowSource1, target.UpdatedRowSource);
            target.UpdatedRowSource = updateRowSource2;
            Assert.IsTrue(updateRowSourceCalled);

            // ReSharper restore HeuristicUnreachableCode
        }

        [Test]
        public void TestProfiledDbCommandWithProfiledDbCommand()
        {
            var mockCommand = new Mock<DbCommand>();
            var mockDbProfiler = new Mock<IDbProfiler>();

            var target = new ProfiledDbCommand(mockCommand.Object, mockDbProfiler.Object);

            // test CreateDbParameter()
            var mockParameter = new Mock<DbParameter>();
            mockCommand.Protected().Setup<DbParameter>("CreateDbParameter").Returns(mockParameter.Object);
            Assert.AreEqual(mockParameter.Object, target.CreateParameter());

            // test DbConnection
            var mockConnection = new Mock<DbConnection>();
            var profiledConnection = new ProfiledDbConnection(mockConnection.Object, mockDbProfiler.Object);
            mockCommand.Protected().Setup<DbConnection>("DbConnection").Returns(profiledConnection);
            var connection = target.Connection;
            Assert.AreEqual(profiledConnection, connection);

            // test DbParameterCollection
            var mockParameterCollection = new Mock<DbParameterCollection>();
            mockCommand.Protected().Setup<DbParameterCollection>("DbParameterCollection").Returns(mockParameterCollection.Object);
            var parameterCollection = target.Parameters;
            Assert.AreEqual(mockParameterCollection.Object, parameterCollection);

            // test DbTransaction
            var mockTransaction = new Mock<DbTransaction>();
            var profiledTransaction = new ProfiledDbTransaction(mockTransaction.Object, profiledConnection);
            mockCommand.Protected().Setup<DbTransaction>("DbTransaction").Returns(profiledTransaction);
            var transaction = target.Transaction;
            Assert.AreEqual(profiledTransaction, transaction);

            // test DesignTimeVisible
            var designTimeVisible1 = true;
            var designTimeVisible2 = false;
            var designTimeVisibleSet = false;
            mockCommand.Setup(cmd => cmd.DesignTimeVisible).Returns(designTimeVisible1);
            mockCommand.SetupSet(cmd => cmd.DesignTimeVisible = It.IsAny<bool>()).Callback<bool>(a =>
            {
                Assert.AreEqual(designTimeVisible2, a);
                designTimeVisibleSet = true;
            });
            Assert.AreEqual(designTimeVisible1, target.DesignTimeVisible);
            target.DesignTimeVisible = designTimeVisible2;
            Assert.IsTrue(designTimeVisibleSet);

            // test ExecuteDbDataReader()
            var mockReader = new Mock<DbDataReader>();
            var profiledReader = new ProfiledDbDataReader(mockReader.Object, mockDbProfiler.Object);
            var cmdBehavior = CommandBehavior.CloseConnection;
            mockCommand.Protected().Setup<DbDataReader>("ExecuteDbDataReader", cmdBehavior).Returns(profiledReader);
            var executeDbCommandCalled = false;
            mockDbProfiler.Setup(p => p.ExecuteDbCommand(It.IsAny<DbExecuteType>(), It.IsAny<IDbCommand>(), It.IsAny<Func<IDataReader>>(), It.IsAny<TagCollection>()))
                .Callback<DbExecuteType, IDbCommand, Func<IDataReader>, IEnumerable<string>>((a, b, c, d) =>
                {
                    Assert.AreEqual(mockCommand.Object, b);
                    c();
                    executeDbCommandCalled = true;
                });
            var reader = target.ExecuteReader(cmdBehavior);
            Assert.IsTrue(executeDbCommandCalled);
            Assert.AreEqual(profiledReader, reader);

            // test Dispose()
            var disposeCalled = false;
            mockCommand.Protected().Setup("Dispose", true).Callback<bool>(a => disposeCalled = true);
            target.Dispose();
            Assert.IsTrue(disposeCalled);
        }
    }
}
