using IBM.Data.Db2;

namespace Polar.Services
{
    public class Db2ConnectionFactory
    {
        private readonly string _connectionString;

        public Db2ConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DB2")
                ?? throw new InvalidOperationException("Connection string DB2 no encontrada.");
        }

        public DB2Connection Create()
        {
            return new DB2Connection(_connectionString);
        }
    }
}