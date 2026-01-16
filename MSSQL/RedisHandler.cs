using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSSQL
{
    public static class RedisHelper
    {
        private static readonly Lazy<ConnectionMultiplexer> lazy =
            new Lazy<ConnectionMultiplexer>(() =>
                ConnectionMultiplexer.Connect("localhost:6379")
            );

        public static ConnectionMultiplexer Connection
        {
            get { return lazy.Value; }
        }

        public static IDatabase DB
        {
            get { return Connection.GetDatabase(); }
        }
    }
}
