﻿using RepoDb.Exceptions;
using RepoDb.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace RepoDb
{
    /// <summary>
    /// Contains the extension methods for <see cref="IDbConnection"/> object.
    /// </summary>
    public static partial class DbConnectionExtension
    {
        #region BulkInsert

        /// <summary>
        /// Bulk insert a list of data entity objects into the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="entities">The list of the data entities to be bulk-inserted.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert<TEntity>(this IDbConnection connection,
            IEnumerable<TEntity> entities,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsert(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", entities, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                entities = (IEnumerable<TEntity>)cancellableTraceLog.Parameter ?? entities;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = provider.BulkInsert<TEntity>(connection: connection,
                entities: entities,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", entities, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert a list of data entity objects into the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="entities">The list of the data entities to be bulk-inserted.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert<TEntity>(this IDbConnection connection,
            string tableName,
            IEnumerable<TEntity> entities,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsert(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", entities, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                entities = (IEnumerable<TEntity>)cancellableTraceLog.Parameter ?? entities;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            using (var reader = new DataEntityDataReader<TEntity>(entities))
            {
                result = provider.BulkInsert(connection: connection,
                    tableName: tableName,
                    reader: reader,
                    mappings: mappings,
                    options: options,
                    bulkCopyTimeout: bulkCopyTimeout,
                    batchSize: batchSize,
                    transaction: transaction);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", entities, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DbDataReader"/> object into the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="reader">The <see cref="DbDataReader"/> object to be used in the bulk-insert operation.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert<TEntity>(this IDbConnection connection,
            DbDataReader reader,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsert(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", reader, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                reader = (DbDataReader)cancellableTraceLog.Parameter ?? reader;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = provider.BulkInsert<TEntity>(connection: connection,
                reader: reader,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", reader, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DbDataReader"/> object into the database.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="reader">The <see cref="DbDataReader"/> object to be used in the bulk-insert operation.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert(this IDbConnection connection,
            string tableName,
            DbDataReader reader,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
        {
            // Validate
            InvokeValidatorValidateBulkInsert(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", reader, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                reader = (DbDataReader)cancellableTraceLog.Parameter ?? reader;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = provider.BulkInsert(connection: connection,
                tableName: tableName,
                reader: reader,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", reader, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DataTable"/> object into the database.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="dataTable">The <see cref="DataTable"/> object to be used in the bulk-insert operation.</param>
        /// <param name="rowState">The state of the rows to be copied to the destination.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert<TEntity>(this IDbConnection connection,
            DataTable dataTable,
            DataRowState rowState = DataRowState.Unchanged,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", dataTable, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                dataTable = (DataTable)cancellableTraceLog.Parameter ?? dataTable;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = provider.BulkInsert<TEntity>(connection: connection,
                dataTable: dataTable,
                rowState: rowState,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", dataTable, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DataTable"/> object into the database.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="dataTable">The <see cref="DataTable"/> object to be used in the bulk-insert operation.</param>
        /// <param name="rowState">The state of the rows to be copied to the destination.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static int BulkInsert(this IDbConnection connection,
            string tableName,
            DataTable dataTable,
            DataRowState rowState = DataRowState.Unchanged,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
        {
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", dataTable, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                dataTable = (DataTable)cancellableTraceLog.Parameter ?? dataTable;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = provider.BulkInsert(connection: connection,
                tableName: tableName,
                dataTable: dataTable,
                rowState: rowState,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", dataTable, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        #endregion

        #region BulkInsertAsync

        /// <summary>
        /// Bulk insert a list of data entity objects into the database in an asynchronous way.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="entities">The list of the data entities to be bulk-inserted.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync<TEntity>(this IDbConnection connection,
            IEnumerable<TEntity> entities,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsertAsync(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", entities, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                entities = (IEnumerable<TEntity>)cancellableTraceLog.Parameter ?? entities;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = await provider.BulkInsertAsync<TEntity>(connection: connection,
                entities: entities,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", entities, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert a list of data entity objects into the database in an asynchronous way.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="entities">The list of the data entities to be bulk-inserted.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync<TEntity>(this IDbConnection connection,
            string tableName,
            IEnumerable<TEntity> entities,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsertAsync(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", entities, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                entities = (IEnumerable<TEntity>)cancellableTraceLog.Parameter ?? entities;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            using (var reader = new DataEntityDataReader<TEntity>(entities))
            {
                result = await provider.BulkInsertAsync(connection: connection,
                    tableName: tableName,
                    reader: reader,
                    mappings: mappings,
                    options: options,
                    bulkCopyTimeout: bulkCopyTimeout,
                    batchSize: batchSize,
                    transaction: transaction);
            }

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", entities, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DbDataReader"/> object into the database in an asynchronous way.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="reader">The <see cref="DbDataReader"/> object to be used in the bulk-insert operation.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync<TEntity>(this IDbConnection connection,
            DbDataReader reader,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            // Validate
            InvokeValidatorValidateBulkInsertAsync(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", reader, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                reader = (DbDataReader)cancellableTraceLog.Parameter ?? reader;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = await provider.BulkInsertAsync<TEntity>(connection: connection,
                reader: reader,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", reader, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DbDataReader"/> object into the database in an asynchronous way.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="reader">The <see cref="DbDataReader"/> object to be used in the bulk-insert operation.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync(this IDbConnection connection,
            string tableName,
            DbDataReader reader,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
        {
            // Validate
            InvokeValidatorValidateBulkInsertAsync(connection);

            // Get the provider
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", reader, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                reader = (DbDataReader)cancellableTraceLog.Parameter ?? reader;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = await provider.BulkInsertAsync(connection: connection,
                tableName: tableName,
                reader: reader,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", reader, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DataTable"/> object into the database in an asynchronous way.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data entity object.</typeparam>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="dataTable">The <see cref="DataTable"/> object to be used in the bulk-insert operation.</param>
        /// <param name="rowState">The state of the rows to be copied to the destination.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync<TEntity>(this IDbConnection connection,
            DataTable dataTable,
            DataRowState rowState = DataRowState.Unchanged,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
            where TEntity : class
        {
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", dataTable, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                dataTable = (DataTable)cancellableTraceLog.Parameter ?? dataTable;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = await provider.BulkInsertAsync<TEntity>(connection: connection,
                dataTable: dataTable,
                rowState: rowState,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", dataTable, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        /// <summary>
        /// Bulk insert an instance of <see cref="DataTable"/> object into the database in an asynchronous way.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        /// <param name="tableName">The target table for bulk-insert operation.</param>
        /// <param name="dataTable">The <see cref="DataTable"/> object to be used in the bulk-insert operation.</param>
        /// <param name="rowState">The state of the rows to be copied to the destination.</param>
        /// <param name="mappings">The list of the columns to be used for mappings. If this parameter is not set, then all columns will be used for mapping.</param>
        /// <param name="options">The bulk-copy options to be used.</param>
        /// <param name="bulkCopyTimeout">The timeout in seconds to be used.</param>
        /// <param name="batchSize">The size per batch to be used.</param>
        /// <param name="transaction">The transaction to be used.</param>
        /// <param name="trace">The trace object to be used.</param>
        /// <returns>The number of rows affected by the execution.</returns>
        public static async Task<int> BulkInsertAsync(this IDbConnection connection,
            string tableName,
            DataTable dataTable,
            DataRowState rowState = DataRowState.Unchanged,
            IEnumerable<BulkInsertMapItem> mappings = null,
            SqlBulkCopyOptions options = SqlBulkCopyOptions.Default,
            int? bulkCopyTimeout = null,
            int? batchSize = null,
            IDbTransaction transaction = null,
            ITrace trace = null)
        {
            var provider = connection.GetDbOperation();

            // Before Execution
            if (trace != null)
            {
                var cancellableTraceLog = new CancellableTraceLog("BulkInsert.Before", dataTable, null);
                trace.BeforeBulkInsert(cancellableTraceLog);
                if (cancellableTraceLog.IsCancelled)
                {
                    if (cancellableTraceLog.IsThrowException)
                    {
                        throw new CancelledExecutionException("BulkInsert.Cancelled");
                    }
                    return 0;
                }
                dataTable = (DataTable)cancellableTraceLog.Parameter ?? dataTable;
            }

            // Variables for the operation
            var result = 0;

            // Before Execution Time
            var beforeExecutionTime = DateTime.UtcNow;

            // Actual execution
            result = await provider.BulkInsertAsync(connection: connection,
                tableName: tableName,
                dataTable: dataTable,
                rowState: rowState,
                mappings: mappings,
                options: options,
                bulkCopyTimeout: bulkCopyTimeout,
                batchSize: batchSize,
                transaction: transaction);

            // After Execution
            if (trace != null)
            {
                trace.AfterBulkInsert(new TraceLog("BulkInsert.After", dataTable, result,
                    DateTime.UtcNow.Subtract(beforeExecutionTime)));
            }

            // Return the result
            return result;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invokes the <see cref="IDbValidator.ValidateBulkInsert"/> method.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        private static void InvokeValidatorValidateBulkInsert(IDbConnection connection)
        {
            connection.GetDbValidator()?.ValidateBulkInsert();
        }

        /// <summary>
        /// Invokes the <see cref="IDbValidator.ValidateBulkInsertAsync"/> method.
        /// </summary>
        /// <param name="connection">The connection object to be used.</param>
        private static void InvokeValidatorValidateBulkInsertAsync(IDbConnection connection)
        {
            connection.GetDbValidator()?.ValidateBulkInsertAsync();
        }

        #endregion
    }
}
