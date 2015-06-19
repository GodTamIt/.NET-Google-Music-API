using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using System.Net.Cache;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Web;
using System.Collections;
using System.Threading.Tasks;

namespace GoogleMusic
{
    /// <summary>
    /// The delegate type invoked when an asynchronous GoogleMusic function makes progress.
    /// </summary>
    /// <param name="progress">The new percentage progress to report, in the range 0.0 to 1.0.</param>
    public delegate void TaskProgressEventHandler(double progress);
    /// <summary>
    /// The delegate type invoked when an asynchronous GoogleMusic function finishes a task.
    /// </summary>
    public delegate void TaskCompleteEventHandler();

    public class Result<T>
    {
        #region Members
        private bool _Success;
        private T _ResultValue;
        private Clients.IClient _Client;
        private string _ErrorMessage;
        private Exception _InnerException;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of Result that represents the outcome of an operation.
        /// </summary>
        /// <param name="success">Required. The boolean value representing the operation's success.</param>
        /// <param name="resultValue">Required. The value returned by the operation.</param>
        /// <param name="client">Required. The parent client of the operation executed.</param>
        /// <param name="errorMessage">Optional. A human-readable error message describing a failure. Value should be null if operation was successful.</param>
        /// <param name="innerException">Optional. The underlying exception thrown by the program on a failure. Value should be null if operation was successful.</param>
        internal Result(bool success, T resultValue, Clients.IClient client, string errorMessage = null, Exception innerException = null)
        {
            _Success = success;
            _ResultValue = resultValue;
            _Client = client;
            _ErrorMessage = errorMessage;
            _InnerException = innerException;
        }
        #endregion

        #region Properties

        /// <summary>
        /// Gets the boolean value representing the operation's success.
        /// </summary>
        public bool Success
        {
            get { return _Success; }
        }

        /// <summary>
        /// Gets the value returned by the operation.
        /// </summary>
        public T ResultValue
        {
            get { return _ResultValue; }
            set { _ResultValue = value; }
        }

        /// <summary>
        /// The parent client of the operation executed.
        /// </summary>
        public Clients.IClient Client
        {
            get { return _Client; }
        }

        /// <summary>
        /// A human-readable error message describing a failure. Value is null if operation was successful.
        /// </summary>
        public string ErrorMessage
        {
            get { return _ErrorMessage; }
        }

        /// <summary>
        /// The underlying exception thrown by the program on a failure. Value is null if operation was successful.
        /// </summary>
        public Exception InnerException
        {
            get { return _InnerException; }
        }

        #endregion
    }
}
