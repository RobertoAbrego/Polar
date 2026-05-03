using System.Data.Odbc;
using System.IO;

namespace Polar.Services
{
    public class AuthService
    {
        private readonly Db2ConnectionFactory _factory;

        public AuthService(Db2ConnectionFactory factory)
        {
            _factory = factory;
        }

        public bool Login(string email, string password)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = "SELECT PASSWORD_HASH FROM USUARIO WHERE EMAIL = ?";

            using var cmd = new OdbcCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read())
                return false;

            var encrypted = reader.GetString(0);
            var key = GetKey();

            var decrypted = CryptoService.Decrypt(encrypted, key);

            return decrypted == password;
        }

        public void Register(string email, string password)
        {
            using var conn = _factory.Create();
            conn.Open();

            var key = GetKey();
            var encrypted = CryptoService.Encrypt(password, key);

            var sql = "INSERT INTO USUARIO (EMAIL, PASSWORD_HASH) VALUES (?, ?)";

            using var cmd = new OdbcCommand(sql, conn);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@pass", encrypted);

            cmd.ExecuteNonQuery();
        }

        private string GetKey()
        {
            var path = Path.Combine("wwwroot", "keys", "key.png");
            return ImageKeyService.GetKeyFromImage(path);
        }
    }
}