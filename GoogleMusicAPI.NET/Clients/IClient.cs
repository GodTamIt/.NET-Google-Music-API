using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMusic.Clients
{
    public interface IClient
    {
        void Logout();

        /// <summary>
        /// Gets whether the client is logged in.
        /// </summary>
        bool IsLoggedIn { get; }
    }
}
