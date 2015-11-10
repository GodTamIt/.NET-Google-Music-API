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

    public class Result<T> : Result
    {

        #region Constructor
        /// <summary>
        /// Initializes a new instance of Result that represents the outcome of an operation.
        /// </summary>
        /// <param name="success">Required. The boolean value representing the operation's success.</param>
        /// <param name="result">Required. The value returned by the operation.</param>
        /// <param name="client">Required. The parent client of the operation executed.</param>
        /// <param name="innerException">Optional. The underlying exception thrown by the program on a failure. Value is null on success but maybe also be null on failure.</param>
        internal Result(bool success, T result, Clients.IClient client, Exception innerException = null) : base(success, client, innerException)
        {
            this.Value = result;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the value returned by the operation.
        /// </summary>
        public T Value { get; protected set; }

        #endregion
    }


    public class Result
    {
        /// <summary>
        /// Initializes a new instance of Result that represents the outcome of an operation.
        /// </summary>
        /// <param name="success">Required. The boolean value representing the operation's success.</param>
        /// <param name="client">Required. The parent client of the operation executed.</param>
        /// <param name="innerException">Optional. The underlying exception thrown by the program on a failure. Value is null on success but maybe also be null on failure.</param>
        internal Result(bool success, Clients.IClient client, Exception innerException = null)
        {
            this.Success = success;
            this.Client = client;
            this.InnerException = innerException;
        }

        #region Properties

        /// <summary>
        /// Gets the boolean value representing the operation's success.
        /// </summary>
        public bool Success { get; protected set; }

        /// <summary>
        /// The parent client of the operation executed.
        /// </summary>
        public Clients.IClient Client { get; protected set; }

        /// <summary>
        /// Optional. The underlying exception thrown by the program on a failure. Value is null on success but maybe also be null on failure.
        /// </summary>
        public Exception InnerException { get; protected set; }

        #endregion
    }
}
