using IBM.Data.Db2;

namespace Polar.Services
{
    public class AuthService
    {
        private readonly Db2ConnectionFactory _factory;
        private readonly IWebHostEnvironment _env;

        public AuthService(
            Db2ConnectionFactory factory,
            IWebHostEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

        public bool Login(string email, string password)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = @"SELECT PASSWORD_ENCRYPTED
                        FROM DB2INST1.USUARIO
                        WHERE EMAIL = @email";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@email", email));

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

            var sql = @"INSERT INTO DB2INST1.USUARIO
                        (EMAIL, PASSWORD_ENCRYPTED)
                        VALUES (@email, @pass)";

            using var cmd = new DB2Command(sql, conn);

            cmd.Parameters.Add(new DB2Parameter("@email", email));
            cmd.Parameters.Add(new DB2Parameter("@pass", encrypted));

            cmd.ExecuteNonQuery();
        }

        private string GetKey()
        {
            var path = Path.Combine(
                _env.WebRootPath,
                "keys",
                "key.png");

            return ImageKeyService.GetKeyFromImage(path);
        }
    }
}