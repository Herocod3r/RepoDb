﻿using RepoDb.Exceptions;
using RepoDb.Extensions;
using RepoDb.Interfaces;
using RepoDb.Reflection;
using RepoDb.Requests;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RepoDb
{
    /// <summary>
    /// Contains the extension methods for <see cref="IDbConnection"/> object.
    /// </summary>
    public static partial class DbConnectionExtension
    {
        #region QueryMultiple<TEntity>

        #region T1, T2

        /// <summary>
        /// Query a multiple resultset from the database based on the given 2 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 2 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>> QueryMultiple<T1, T2>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            int? top2 = 0,
            IEnumerable<OrderField> orderBy2 = null,
            string hints2 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
        {
            return QueryMultipleInternal<T1, T2>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                top2: top2,
                orderBy2: orderBy2,
                hints2: hints2,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 2 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 2 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>> QueryMultipleInternal<T1, T2>(this IDbConnection connection,
        QueryGroup where1,
        QueryGroup where2,
        IEnumerable<OrderField> orderBy1 = null,
        int? top1 = 0,
        string hints1 = null,
        int? top2 = 0,
        IEnumerable<OrderField> orderBy2 = null,
        string hints2 = null,
        int? commandTimeout = null,
        IDbTransaction transaction = null,
        ITrace trace = null,
        IStatementBuilder statementBuilder = null)
        where T1 : class
        where T2 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>>(item1, item2);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 3 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> QueryMultiple<T1, T2, T3>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            return QueryMultipleInternal<T1, T2, T3>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 3 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>> QueryMultipleInternal<T1, T2, T3>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(item1, item2, item3);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4

        /// <summary>
        /// Query a multiple resultset from the database based on the given 4 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 4 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>
            QueryMultiple<T1, T2, T3, T4>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            return QueryMultipleInternal<T1, T2, T3, T4>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 4 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>
            QueryMultipleInternal<T1, T2, T3, T4>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(item1, item2, item3, item4);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5

        /// <summary>
        /// Query a multiple resultset from the database based on the given 5 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 5 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>
            QueryMultiple<T1, T2, T3, T4, T5>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            return QueryMultipleInternal<T1, T2, T3, T4, T5>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 5 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 5 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>
            QueryMultipleInternal<T1, T2, T3, T4, T5>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(item1, item2, item3, item4, item5);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5, T6

        /// <summary>
        /// Query a multiple resultset from the database based on the given 6 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 6 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>
            QueryMultiple<T1, T2, T3, T4, T5, T6>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            Expression<Func<T6, bool>> where6,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            return QueryMultipleInternal<T1, T2, T3, T4, T5, T6>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                where6: QueryGroup.Parse<T6>(where6),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                orderBy6: orderBy6,
                top6: top6,
                hints6: hints6,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 6 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 6 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>
            QueryMultipleInternal<T1, T2, T3, T4, T5, T6>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            QueryGroup where6,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5, where6 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // T6 Variables
            var request6 = new QueryMultipleRequest(6,
                typeof(T6),
                connection,
                transaction,
                FieldCache.Get<T6>(),
                where6,
                orderBy6,
                top6,
                hints6,
                statementBuilder);
            var commandText6 = CommandTextCache.GetQueryMultipleText<T6>(request6);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5, commandText6);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>(),
                where6.MapTo<T6>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the sixth result
                reader?.NextResult();
                var item6 = DataReader.ToEnumerable<T6>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(
                    item1, item2, item3, item4, item5, item6);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5, T6, T7

        /// <summary>
        /// Query a multiple resultset from the database based on the given 7 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <typeparam name="T7">The seventh target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="where7">The query expression to be used (at T7).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy7">The order definition of the fields to be used (at T7).</param>
        /// <param name="top7">The top number of data to be used (at T7).</param>
        /// <param name="hints7">The table hints to be used (at T7). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 7 enumerable target data entity types.</returns>
        public static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>
            QueryMultiple<T1, T2, T3, T4, T5, T6, T7>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            Expression<Func<T6, bool>> where6,
            Expression<Func<T7, bool>> where7,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            IEnumerable<OrderField> orderBy7 = null,
            int? top7 = 0,
            string hints7 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
            where T7 : class
        {
            return QueryMultipleInternal<T1, T2, T3, T4, T5, T6, T7>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                where6: QueryGroup.Parse<T6>(where6),
                where7: QueryGroup.Parse<T7>(where7),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                orderBy6: orderBy6,
                top6: top6,
                hints6: hints6,
                orderBy7: orderBy7,
                top7: top7,
                hints7: hints7,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 7 target types.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <typeparam name="T7">The seventh target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="where7">The query expression to be used (at T7).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy7">The order definition of the fields to be used (at T7).</param>
        /// <param name="top7">The top number of data to be used (at T7).</param>
        /// <param name="hints7">The table hints to be used (at T7). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 7 enumerable target data entity types.</returns>
        internal static Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>
            QueryMultipleInternal<T1, T2, T3, T4, T5, T6, T7>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            QueryGroup where6,
            QueryGroup where7,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            IEnumerable<OrderField> orderBy7 = null,
            int? top7 = 0,
            string hints7 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
            where T7 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultiple(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5, where6, where7 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // T6 Variables
            var request6 = new QueryMultipleRequest(6,
                typeof(T6),
                connection,
                transaction,
                FieldCache.Get<T6>(),
                where6,
                orderBy6,
                top6,
                hints6,
                statementBuilder);
            var commandText6 = CommandTextCache.GetQueryMultipleText<T6>(request6);

            // T7 Variables
            var request7 = new QueryMultipleRequest(7,
                typeof(T7),
                connection,
                transaction,
                FieldCache.Get<T7>(),
                where7,
                orderBy7,
                top7,
                hints7,
                statementBuilder);
            var commandText7 = CommandTextCache.GetQueryMultipleText<T7>(request7);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5, commandText6, commandText7);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>(),
                where6.MapTo<T6>(),
                where7.MapTo<T7>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>)null;
            using (var reader = ExecuteReaderInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the sixth result
                reader?.NextResult();
                var item6 = DataReader.ToEnumerable<T6>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the seventh result
                reader?.NextResult();
                var item7 = DataReader.ToEnumerable<T7>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(
                    item1, item2, item3, item4, item5, item6, item7);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #endregion

        #region QueryMultipleAsync<TEntity>

        #region T1, T2

        /// <summary>
        /// Query a multiple resultset from the database based on the given 2 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 2 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> QueryMultipleAsync<T1, T2>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            int? top2 = 0,
            IEnumerable<OrderField> orderBy2 = null,
            string hints2 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
        {
            return QueryMultipleAsyncInternal<T1, T2>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                top2: top2,
                orderBy2: orderBy2,
                hints2: hints2,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 2 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 2 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>>> QueryMultipleAsyncInternal<T1, T2>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            int? top2 = 0,
            IEnumerable<OrderField> orderBy2 = null,
            string hints2 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>>(item1, item2);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 3 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> QueryMultipleAsync<T1, T2, T3>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            return QueryMultipleAsyncInternal<T1, T2, T3>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 3 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>> QueryMultipleAsyncInternal<T1, T2, T3>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>>(item1, item2, item3);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4

        /// <summary>
        /// Query a multiple resultset from the database based on the given 4 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 4 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>
            QueryMultipleAsync<T1, T2, T3, T4>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            return QueryMultipleAsyncInternal<T1, T2, T3, T4>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 3 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 4 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>>
            QueryMultipleAsyncInternal<T1, T2, T3, T4>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>>(item1, item2, item3, item4);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5

        /// <summary>
        /// Query a multiple resultset from the database based on the given 5 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 5 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>
            QueryMultipleAsync<T1, T2, T3, T4, T5>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            return QueryMultipleAsyncInternal<T1, T2, T3, T4, T5>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 5 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 5 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>>
            QueryMultipleAsyncInternal<T1, T2, T3, T4, T5>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>>(item1, item2, item3, item4, item5);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5, T6

        /// <summary>
        /// Query a multiple resultset from the database based on the given 6 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 6 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>
            QueryMultipleAsync<T1, T2, T3, T4, T5, T6>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            Expression<Func<T6, bool>> where6,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            return QueryMultipleAsyncInternal<T1, T2, T3, T4, T5, T6>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                where6: QueryGroup.Parse<T6>(where6),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                orderBy6: orderBy6,
                top6: top6,
                hints6: hints6,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 6 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 6 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>>
            QueryMultipleAsyncInternal<T1, T2, T3, T4, T5, T6>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            QueryGroup where6,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5, where6 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // T6 Variables
            var request6 = new QueryMultipleRequest(6,
                typeof(T6),
                connection,
                transaction,
                FieldCache.Get<T6>(),
                where6,
                orderBy6,
                top6,
                hints6,
                statementBuilder);
            var commandText6 = CommandTextCache.GetQueryMultipleText<T6>(request6);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5, commandText6);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>(),
                where6.MapTo<T6>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the sixth result
                reader?.NextResult();
                var item6 = DataReader.ToEnumerable<T6>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>>(
                    item1, item2, item3, item4, item5, item6);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #region T1, T2, T3, T4, T5, T6, T7

        /// <summary>
        /// Query a multiple resultset from the database based on the given 7 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <typeparam name="T7">The seventh target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="where7">The query expression to be used (at T7).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy7">The order definition of the fields to be used (at T7).</param>
        /// <param name="top7">The top number of data to be used (at T7).</param>
        /// <param name="hints7">The table hints to be used (at T7). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 7 enumerable target data entity types.</returns>
        public static Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>
            QueryMultipleAsync<T1, T2, T3, T4, T5, T6, T7>(this IDbConnection connection,
            Expression<Func<T1, bool>> where1,
            Expression<Func<T2, bool>> where2,
            Expression<Func<T3, bool>> where3,
            Expression<Func<T4, bool>> where4,
            Expression<Func<T5, bool>> where5,
            Expression<Func<T6, bool>> where6,
            Expression<Func<T7, bool>> where7,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            IEnumerable<OrderField> orderBy7 = null,
            int? top7 = 0,
            string hints7 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
            where T7 : class
        {
            return QueryMultipleAsyncInternal<T1, T2, T3, T4, T5, T6, T7>(connection: connection,
                where1: QueryGroup.Parse<T1>(where1),
                where2: QueryGroup.Parse<T2>(where2),
                where3: QueryGroup.Parse<T3>(where3),
                where4: QueryGroup.Parse<T4>(where4),
                where5: QueryGroup.Parse<T5>(where5),
                where6: QueryGroup.Parse<T6>(where6),
                where7: QueryGroup.Parse<T7>(where7),
                orderBy1: orderBy1,
                top1: top1,
                hints1: hints1,
                orderBy2: orderBy2,
                top2: top2,
                hints2: hints2,
                orderBy3: orderBy3,
                top3: top3,
                hints3: hints3,
                orderBy4: orderBy4,
                top4: top4,
                hints4: hints4,
                orderBy5: orderBy5,
                top5: top5,
                hints5: hints5,
                orderBy6: orderBy6,
                top6: top6,
                hints6: hints6,
                orderBy7: orderBy7,
                top7: top7,
                hints7: hints7,
                commandTimeout: commandTimeout,
                transaction: transaction,
                trace: trace,
                statementBuilder: statementBuilder);
        }

        /// <summary>
        /// Query a multiple resultset from the database based on the given 7 target types in an asychronous way.
        /// </summary>
        /// <typeparam name="T1">The first target type.</typeparam>
        /// <typeparam name="T2">The second target type.</typeparam>
        /// <typeparam name="T3">The third target type.</typeparam>
        /// <typeparam name="T4">The fourth target type.</typeparam>
        /// <typeparam name="T5">The fifth target type.</typeparam>
        /// <typeparam name="T6">The sixth target type.</typeparam>
        /// <typeparam name="T7">The seventh target type.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="where1">The query expression to be used (at T1).</param>
        /// <param name="where2">The query expression to be used (at T2).</param>
        /// <param name="where3">The query expression to be used (at T3).</param>
        /// <param name="where4">The query expression to be used (at T4).</param>
        /// <param name="where5">The query expression to be used (at T5).</param>
        /// <param name="where6">The query expression to be used (at T6).</param>
        /// <param name="where7">The query expression to be used (at T7).</param>
        /// <param name="orderBy1">The order definition of the fields to be used (at T1).</param>
        /// <param name="top1">The top number of data to be used (at T1).</param>
        /// <param name="hints1">The table hints to be used (at T1). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy2">The order definition of the fields to be used (at T2).</param>
        /// <param name="top2">The top number of data to be used (at T2).</param>
        /// <param name="hints2">The table hints to be used (at T2). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy3">The order definition of the fields to be used (at T3).</param>
        /// <param name="top3">The top number of data to be used (at T3).</param>
        /// <param name="hints3">The table hints to be used (at T3). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy4">The order definition of the fields to be used (at T4).</param>
        /// <param name="top4">The top number of data to be used (at T4).</param>
        /// <param name="hints4">The table hints to be used (at T4). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy5">The order definition of the fields to be used (at T5).</param>
        /// <param name="top5">The top number of data to be used (at T5).</param>
        /// <param name="hints5">The table hints to be used (at T5). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy6">The order definition of the fields to be used (at T6).</param>
        /// <param name="top6">The top number of data to be used (at T6).</param>
        /// <param name="hints6">The table hints to be used (at T6). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="orderBy7">The order definition of the fields to be used (at T7).</param>
        /// <param name="top7">The top number of data to be used (at T7).</param>
        /// <param name="hints7">The table hints to be used (at T7). See <see cref="SqlServerTableHints"/> class.</param>
        /// <param name="commandTimeout">The command timeout in seconds to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <param name="statementBuilder">The statement builder object to be used.</param>
        /// <returns>A tuple of 7 enumerable target data entity types.</returns>
        internal static async Task<Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>>
            QueryMultipleAsyncInternal<T1, T2, T3, T4, T5, T6, T7>(this IDbConnection connection,
            QueryGroup where1,
            QueryGroup where2,
            QueryGroup where3,
            QueryGroup where4,
            QueryGroup where5,
            QueryGroup where6,
            QueryGroup where7,
            IEnumerable<OrderField> orderBy1 = null,
            int? top1 = 0,
            string hints1 = null,
            IEnumerable<OrderField> orderBy2 = null,
            int? top2 = 0,
            string hints2 = null,
            IEnumerable<OrderField> orderBy3 = null,
            int? top3 = 0,
            string hints3 = null,
            IEnumerable<OrderField> orderBy4 = null,
            int? top4 = 0,
            string hints4 = null,
            IEnumerable<OrderField> orderBy5 = null,
            int? top5 = 0,
            string hints5 = null,
            IEnumerable<OrderField> orderBy6 = null,
            int? top6 = 0,
            string hints6 = null,
            IEnumerable<OrderField> orderBy7 = null,
            int? top7 = 0,
            string hints7 = null,
            int? commandTimeout = null,
            IDbTransaction transaction = null,
            ITrace trace = null,
            IStatementBuilder statementBuilder = null)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
            where T5 : class
            where T6 : class
            where T7 : class
        {
            // Validate
            InvokeValidatorValidateQueryMultipleAsync(connection);

            // Variables
            var commandType = CommandType.Text;

            // Add fix to the cross-collision of the variables for all the QueryGroup(s)
            QueryGroup.FixForQueryMultiple(new[] { where1, where2, where3, where4, where5, where6, where7 });

            // T1 Variables
            var request1 = new QueryMultipleRequest(1,
                typeof(T1),
                connection,
                transaction,
                FieldCache.Get<T1>(),
                where1,
                orderBy1,
                top1,
                hints1,
                statementBuilder);
            var commandText1 = CommandTextCache.GetQueryMultipleText<T1>(request1);

            // T2 Variables
            var request2 = new QueryMultipleRequest(2,
                typeof(T2),
                connection,
                transaction,
                FieldCache.Get<T2>(),
                where2,
                orderBy2,
                top2,
                hints2,
                statementBuilder);
            var commandText2 = CommandTextCache.GetQueryMultipleText<T2>(request2);

            // T3 Variables
            var request3 = new QueryMultipleRequest(3,
                typeof(T3),
                connection,
                transaction,
                FieldCache.Get<T3>(),
                where3,
                orderBy3,
                top3,
                hints3,
                statementBuilder);
            var commandText3 = CommandTextCache.GetQueryMultipleText<T3>(request3);

            // T4 Variables
            var request4 = new QueryMultipleRequest(4,
                typeof(T4),
                connection,
                transaction,
                FieldCache.Get<T4>(),
                where4,
                orderBy4,
                top4,
                hints4,
                statementBuilder);
            var commandText4 = CommandTextCache.GetQueryMultipleText<T4>(request4);

            // T5 Variables
            var request5 = new QueryMultipleRequest(5,
                typeof(T5),
                connection,
                transaction,
                FieldCache.Get<T5>(),
                where5,
                orderBy5,
                top5,
                hints5,
                statementBuilder);
            var commandText5 = CommandTextCache.GetQueryMultipleText<T5>(request5);

            // T6 Variables
            var request6 = new QueryMultipleRequest(6,
                typeof(T6),
                connection,
                transaction,
                FieldCache.Get<T6>(),
                where6,
                orderBy6,
                top6,
                hints6,
                statementBuilder);
            var commandText6 = CommandTextCache.GetQueryMultipleText<T6>(request6);

            // T7 Variables
            var request7 = new QueryMultipleRequest(7,
                typeof(T7),
                connection,
                transaction,
                FieldCache.Get<T7>(),
                where7,
                orderBy7,
                top7,
                hints7,
                statementBuilder);
            var commandText7 = CommandTextCache.GetQueryMultipleText<T7>(request7);

            // Shared objects for all types
            var commandText = string.Join(" ", commandText1, commandText2, commandText3, commandText4, commandText5, commandText6, commandText7);
            var maps = new[]
            {
                where1.MapTo<T1>(),
                where2.MapTo<T2>(),
                where3.MapTo<T3>(),
                where4.MapTo<T4>(),
                where5.MapTo<T5>(),
                where6.MapTo<T6>(),
                where7.MapTo<T7>()
            };
            var param = QueryGroup.AsMappedObject(maps, false);

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog(commandText, param, null);
                trace.BeforeQueryMultiple(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException(commandText);
                    }
                    return null;
                }
                commandText = (cancellableTraceLog.Statement ?? commandText);
                param = (cancellableTraceLog.Parameter ?? param);
            }

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual Execution
            var result = (Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>)null;
            using (var reader = await ExecuteReaderAsyncInternal(connection: connection,
                commandText: commandText,
                param: param,
                commandType: commandType,
                commandTimeout: commandTimeout,
                transaction: transaction,
                skipCommandArrayParametersCheck: true))
            {
                // Extract the first result
                var item1 = DataReader.ToEnumerable<T1>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the second result
                reader?.NextResult();
                var item2 = DataReader.ToEnumerable<T2>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the third result
                reader?.NextResult();
                var item3 = DataReader.ToEnumerable<T3>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fourth result
                reader?.NextResult();
                var item4 = DataReader.ToEnumerable<T4>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the fifth result
                reader?.NextResult();
                var item5 = DataReader.ToEnumerable<T5>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the sixth result
                reader?.NextResult();
                var item6 = DataReader.ToEnumerable<T6>((DbDataReader)reader, connection, transaction)?.AsList();

                // Extract the seventh result
                reader?.NextResult();
                var item7 = DataReader.ToEnumerable<T7>((DbDataReader)reader, connection, transaction)?.AsList();

                // Set the result instance
                result = new Tuple<IEnumerable<T1>, IEnumerable<T2>, IEnumerable<T3>, IEnumerable<T4>, IEnumerable<T5>, IEnumerable<T6>, IEnumerable<T7>>(
                    item1, item2, item3, item4, item5, item6, item7);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterQueryMultiple(new TraceLog(commandText, param, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Result
            return result;
        }

        #endregion

        #endregion

        #region Helpers

        /// <summary>
        /// Invokes the <see cref="IDbValidator.ValidateQueryMultiple"/> method.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        private static void InvokeValidatorValidateQueryMultiple(IDbConnection connection)
        {
            connection.GetDbValidator()?.ValidateQueryMultiple();
        }

        /// <summary>
        /// Invokes the <see cref="IDbValidator.ValidateQueryMultipleAsync"/> method.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        private static void InvokeValidatorValidateQueryMultipleAsync(IDbConnection connection)
        {
            connection.GetDbValidator()?.ValidateQueryMultipleAsync();
        }

        #endregion
    }
}
