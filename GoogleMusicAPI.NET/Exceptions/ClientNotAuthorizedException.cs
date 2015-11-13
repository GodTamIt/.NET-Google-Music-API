using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GoogleMusic
{
    /// <summary>
    /// The exception is thrown when a client is not authorized or logged in to perform an operation.
    /// </summary>
    public class ClientNotAuthorizedException : ApplicationException
    {
        internal ClientNotAuthorizedException(Clients.IClient client)
            : base(String.Format("Cannot perform operation while {0} is not logged in.", client.GetType().Name))
        {
            this.Client = client;
        }

        /// <summary>
        /// Gets the instance of the client that threw the exception.
        /// </summary>
        public Clients.IClient Client { get; protected set; }

    }
}
