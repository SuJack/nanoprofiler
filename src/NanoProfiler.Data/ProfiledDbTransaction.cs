/*
    The MIT License (MIT)
    Copyright © 2015 Englishtown <opensource@englishtown.com>

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System.Data;
using System.Data.Common;

namespace EF.Diagnostics.Profiling.Data
{
    /// <summary>
    /// A wrapper of <see cref="IDbTransaction"/> which supports DB profiling.
    /// </summary>
    internal class ProfiledDbTransaction : DbTransaction
    {
        private readonly IDbTransaction _transaction;
        private readonly DbTransaction _dbTransaction;
        private DbConnection _dbConnection;

        /// <summary>
        /// Gets the wrapped <see cref="DbTransaction"/>
        /// </summary>
        public DbTransaction WrappedTransaction { get { return _dbTransaction; } }

        #region Constructors

        /// <summary>
        /// Initializes a <see cref="ProfiledDbTransaction"/>.
        /// </summary>
        /// <param name="transaction">The <see cref="IDbTransaction"/> to be profiled.</param>
        /// <param name="connection">The <see cref="DbConnection"/>.</param>
        public ProfiledDbTransaction(IDbTransaction transaction, DbConnection connection)
        {
            _transaction = transaction;
            _dbTransaction = _transaction as DbTransaction;
            _dbConnection = connection ?? transaction.Connection as DbConnection;
        }

        #endregion

        #region DbTransaction Members

        /// <summary>
        /// Commits the database transaction. 
        /// </summary>
        public override void Commit()
        {
            _transaction.Commit();
        }

        /// <summary>
        /// Returns the <see cref="DbConnection"/> object associated with the transaction. 
        /// </summary>
        protected override DbConnection DbConnection
        {
            get { return _dbConnection; }
        }

        /// <summary>
        /// Returns <see cref="IsolationLevel"/> for this transaction. 
        /// </summary>
        public override IsolationLevel IsolationLevel
        {
            get { return _transaction.IsolationLevel; }
        }

        /// <summary>
        /// Rolls back a transaction from a pending state. 
        /// </summary>
        public override void Rollback()
        {
            _transaction.Rollback();
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="ProfiledDbTransaction"/> and optionally releases the managed resources. 
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion
    }

}
