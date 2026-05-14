using IBM.Data.Db2;
using Polar.Models;
using System.Net;
using System.Net.Mail;

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

        // =========================
        // 🔑 GENERAR KEY
        // =========================

        private string GetKey()
        {
            var path = Path.Combine(
                _env.WebRootPath,
                "keys",
                "key.png");

            return ImageKeyService.GetKeyFromImage(path);
        }

        // =========================
        // 📧 ENVIAR CORREO
        // =========================

        public void SendVerificationEmail(
            string toEmail,
            string code)
        {
            var fromEmail = "polar.app.sv@gmail.com";

            var password = "jrddhtfapcsodstn";

            var client = new SmtpClient(
                "smtp.gmail.com",
                587)
            {
                Credentials = new NetworkCredential(
                    fromEmail,
                    password),

                EnableSsl = true,

                DeliveryMethod =
                    SmtpDeliveryMethod.Network,

                UseDefaultCredentials = false
            };

            var mail = new MailMessage(
                fromEmail,
                toEmail)
            {
                Subject = "Código de verificación",

                Body =
                    $"Tu código de verificación es: {code}"
            };

            client.Send(mail);
        }

        // =========================
        // 🔐 LOGIN
        // =========================

        public bool Login(
            string email,
            string password)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT PASSWORD_ENCRYPTED
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            using var reader =
                cmd.ExecuteReader();

            if (!reader.Read())
                return false;

            var encrypted =
                reader.GetString(0);

            var key = GetKey();

            var decrypted =
                CryptoService.Decrypt(
                    encrypted,
                    key);

            return decrypted == password;
        }

        // =========================
        // 📝 REGISTER
        // =========================

        public void Register(
            string nombre,
            string email,
            string password)
        {
            using var conn = _factory.Create();

            conn.Open();

            var key = GetKey();

            var encrypted =
                CryptoService.Encrypt(
                    password,
                    key);

            var sql = @"
                INSERT INTO DB2INST1.USUARIO
                (NOMBRE, EMAIL, PASSWORD_ENCRYPTED)
                VALUES (@nombre, @email, @pass)";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@nombre", nombre));

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            cmd.Parameters.Add(
                new DB2Parameter("@pass", encrypted));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 📧 EXISTE EMAIL
        // =========================

        public bool EmailExists(string email)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT COUNT(*)
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            var count = Convert.ToInt32(
                cmd.ExecuteScalar());

            return count > 0;
        }

        // =========================
        // 👤 GET USER
        // =========================

        public UsuarioModel? GetUserByEmail(
            string email)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT
                    NOMBRE,
                    EMAIL,
                    FOTO_PERFIL
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            using var reader =
                cmd.ExecuteReader();

            if (reader.Read())
            {
                return new UsuarioModel
                {
                    Nombre = reader.GetString(0),
                    Email = reader.GetString(1),
                    FotoPerfil = reader.IsDBNull(2)
                        ? null
                        : reader.GetString(2)
                };
            }

            return null;
        }

        // =========================
        // 👤 GET NOMBRE
        // =========================

        public string? GetNombreByEmail(
            string email)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT NOMBRE
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            var result = cmd.ExecuteScalar();

            return result?.ToString();
        }

        // =========================
        // ✏️ UPDATE PROFILE
        // =========================

        public void UpdateProfile(
            string email,
            string nombre,
            string? foto)
        {
            using var conn = _factory.Create();

            conn.Open();

            string sql;

            if (foto != null)
            {
                sql = @"
                    UPDATE DB2INST1.USUARIO
                    SET
                        NOMBRE = @n,
                        FOTO_PERFIL = @f
                    WHERE EMAIL = @e";
            }
            else
            {
                sql = @"
                    UPDATE DB2INST1.USUARIO
                    SET
                        NOMBRE = @n
                    WHERE EMAIL = @e";
            }

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@n", nombre));

            cmd.Parameters.Add(
                new DB2Parameter("@e", email));

            if (foto != null)
            {
                cmd.Parameters.Add(
                    new DB2Parameter("@f", foto));
            }

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 🔑 CHANGE PASSWORD
        // =========================

        public void ChangePassword(
            string email,
            string newPassword)
        {
            using var conn = _factory.Create();

            conn.Open();

            var key = GetKey();

            var encrypted =
                CryptoService.Encrypt(
                    newPassword,
                    key);

            var sql = @"
                UPDATE DB2INST1.USUARIO
                SET PASSWORD_ENCRYPTED = @p
                WHERE EMAIL = @e";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@p", encrypted));

            cmd.Parameters.Add(
                new DB2Parameter("@e", email));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // ⭐ PUNTOS
        // =========================

        public int GetPuntosByEmail(
            string email)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT COALESCE(PUNTOS_TOTALES, 0)
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            var result = cmd.ExecuteScalar();

            if (result == null ||
                result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        // =========================
        // 🏆 NIVEL
        // =========================

        public int GetNivelByEmail(
            string email)
        {
            using var conn = _factory.Create();

            conn.Open();

            var sql = @"
                SELECT COALESCE(NIVEL, 1)
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            var result = cmd.ExecuteScalar();

            if (result == null ||
                result == DBNull.Value)
                return 1;

            return Convert.ToInt32(result);
        }

        // =========================
        // 📈 PROGRESO
        // =========================

        public int GetProgresoAlSiguienteNivel(
            string email)
        {
            var puntos =
                GetPuntosByEmail(email);

            return puntos % 100;
        }

        // =========================
        // 🚀 ACTUALIZAR PROGRESO
        // =========================

        public void ActualizarProgresoUsuario(
            string email,
            int puntosGanados)
        {
            using var conn = _factory.Create();

            conn.Open();

            var puntosActuales =
                GetPuntosByEmail(email);

            var nuevosPuntos =
                puntosActuales + puntosGanados;

            var nuevoNivel =
                (nuevosPuntos / 100) + 1;

            var sql = @"
                UPDATE DB2INST1.USUARIO
                SET
                    PUNTOS_TOTALES = @puntos,
                    NIVEL = @nivel
                WHERE EMAIL = @email";

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter(
                    "@puntos",
                    nuevosPuntos));

            cmd.Parameters.Add(
                new DB2Parameter(
                    "@nivel",
                    nuevoNivel));

            cmd.Parameters.Add(
                new DB2Parameter(
                    "@email",
                    email));

            cmd.ExecuteNonQuery();
        }
    }
}