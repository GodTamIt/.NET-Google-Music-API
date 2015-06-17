using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoogleMusic
{
    /// <summary>
    /// Formats exceptions into human-readable formats.
    /// </summary>
    internal static class ExceptionExtensions
    {
        public static string ToString(this System.Net.WebException ex, string message)
        {
            string details = (ex.Response == null ? ex.Message : "The server response was: " + ex.Response);
            return String.Format("{0}\n\n{1}", message, details);
        }

        public static string ToString(this Exception ex, string message)
        {
            return String.Format("{0}\n\n{1}", message, ex.Message);
        }
    }
}
