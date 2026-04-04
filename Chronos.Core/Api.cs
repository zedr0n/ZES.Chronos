using System;

#pragma warning disable SA1600

namespace Chronos.Core
{
    /// <summary>
    /// JSON Api static data
    /// </summary>
    public static class Api
    {
        /// <summary>
        /// Get the server url
        /// </summary>
        /// <param name="useRemote">Use remote (web) server</param>
        /// <param name="server">Server url</param>
        /// <returns>True if info is available</returns>
        public static bool TryGetServer(bool useRemote, out string server)
        {
            var envVariable = useRemote ? "REMOTESERVER" : "SERVER";
            server = Environment.GetEnvironmentVariable(envVariable);
            return server != null;
        }
    }
}