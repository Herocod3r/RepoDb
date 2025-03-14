﻿using RepoDb.DbHelpers;
using RepoDb.DbValidators;
using RepoDb.SqLite.DbSettings;
using RepoDb.StatementBuilders;
using System.Data.SQLite;

namespace RepoDb.SqLite
{
    /// <summary>
    /// A class used to initialize necessary objects that is connected to <see cref="SQLiteConnection"/> object.
    /// </summary>
    public static class Bootstrap
    {
        #region Properties

        /// <summary>
        /// Gets the value indicating whether the initialization is completed.
        /// </summary>
        public static bool Initialized { get; private set; }

        #endregion

        #region Methods

        public static void Initialize()
        {
            // Skip if already initialized
            if (Initialized == true)
            {
                return;
            }

            // Map the DbSetting
            DbSettingMapper.Add(typeof(SQLiteConnection), new SqLiteDbSetting(), true);

            // Map the DbHelper
            DbHelperMapper.Add(typeof(SQLiteConnection), new SqLiteDbHelper(), true);

            // Map the DbValidator
            DbValidatorMapper.Add(typeof(SQLiteConnection), new SqLiteDbValidator(), true);

            // Map the Statement Builder
            StatementBuilderMapper.Add(typeof(SQLiteConnection), new SqLiteStatementBuilder(), true);

            // Set the flag
            Initialized = true;
        }

        #endregion
    }
}
