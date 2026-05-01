using System.Data.Odbc;
using System.Security.Cryptography;
using System.Text;

namespace Polar.Services
{
    public class Db2Service
    {
        private readonly string _connString;

        public Db2Service()
        {
            // 🔥 CORREGIDO: asignar al campo de la clase (_connString)
            _connString = "Driver=/app/clidriver/lib/libdb2.so;" +
              "Database=POLAR;" +
              "Hostname=db2;" +
              "Port=50000;" +
              "Protocol=TCPIP;" +
              "Uid=db2inst1;" +
              "Pwd=Password123;";
        }

        private string Hash(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        public bool Login(string email, string password, string imageKey)
        {
            using var conn = new OdbcConnection(_connString);
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