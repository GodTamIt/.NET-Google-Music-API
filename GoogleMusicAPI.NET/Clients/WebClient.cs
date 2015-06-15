using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GoogleMusic.Net;

namespace GoogleMusic.Clients
{
    public class WebClient : IClient
    {

        #region Members
        private Http http;
        #endregion

        #region Constructor

        public WebClient(bool validate = true, bool verifySSL = true)
        {
            http = new Http();
        }

        #endregion


        #region Login

        public bool Login(string email, string password)
        {
            return true;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {

            return true;
        }

        public void Logout()
        {
            http = new Http();
        }

        #endregion
    }
}
