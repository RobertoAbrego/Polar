using System.Data.Odbc;
using System.Security.Cryptography;
using System.Text;

namespace Polar.Services
{
    public class Db2Service
    {
        private readonly Db2ConnectionFactory _factory;

        public Db2Service(Db2ConnectionFactory factory)
        {
            _factory = factory;
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public bool Login(string email, string password, string imageKey)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = @"SELECT PASSWORD_HASH, IMAGE_KEY
                        FROM USUARIO
                        WHERE EMAIL = ?";

            using var cmd = new OdbcCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return false;

            var dbPassword = reader.GetString(0);
            var dbImage = reader.GetString(1);

            var inputHash = Hash(password);

            return dbPassword == inputHash && dbImage == imageKey;
        }
    }
}