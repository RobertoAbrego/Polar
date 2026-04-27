using System.Data.Odbc;

namespace Polar.Services
{
    public class Db2Service
    {
        private readonly string _connString;

        public Db2Service(IConfiguration config)
        {
            _connString = "Driver=/opt/ibm/clidriver;" +
                          "Database=POLAR;" +
                          "Hostname=db2;" +
                          "Port=50000;" +
                          "Protocol=TCPIP;" +
                          "Uid=db2inst1;" +
                          "Pwd=Password123;";
        }

        public void TestConnection()
        {
            using var conn = new OdbcConnection(_connString);
            conn.Open();

            using var cmd = new OdbcCommand("SELECT CURRENT DATE FROM SYSIBM.SYSDUMMY1", conn);
            var result = cmd.ExecuteScalar();

            Console.WriteLine($"DB2 OK: {result}");
        }
    }
}