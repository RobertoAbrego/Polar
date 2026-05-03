using System.Data.Odbc;

namespace Polar.Services
{
    public class Db2ConnectionFactory
    {
        private readonly string _connectionString =
            "Driver=/opt/ibm/clidriver/lib/libdb2.so;" +
            "Database=POLAR;" +
            "Hostname=db2;" +
            "Port=50000;" +
            "Protocol=TCPIP;" +
            "Uid=db2inst1;" +
            "Pwd=Password123;";

        public OdbcConnection Create()
        {
            return new OdbcConnection(_connectionString);
        }
    }
}