using IBM.Data.Db2;
using Polar.Models;
using Microsoft.AspNetCore.Http;

namespace Polar.Services
{
    public class EvidenciaService
    {
        private readonly Db2ConnectionFactory _factory;
        private readonly IWebHostEnvironment _env;

        public EvidenciaService(
            Db2ConnectionFactory factory,
            IWebHostEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

        // =========================
        // 🌱 MISIONES
        // =========================
        public List<dynamic> GetMisiones()
        {
            var list = new List<dynamic>();

            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                SELECT ID, TITULO, DESCRIPCION, TIPO, PUNTOS
                FROM DB2INST1.MISION
                ORDER BY ID DESC
                FETCH FIRST 3 ROWS ONLY";

            using var cmd = new DB2Command(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new
                {
                    Id = reader.GetInt32(0),
                    Titulo = reader.GetString(1),
                    Descripcion = reader.GetString(2),
                    Tipo = reader.GetString(3),
                    Puntos = reader.GetInt32(4)
                });
            }

            return list;
        }

        // =========================
        // 📸 CREAR EVIDENCIA
        // =========================
        public void Create(
            string email,
            int misionId,
            string descripcion,
            IFormFile imagen)
        {
            using var conn = _factory.Create();
            conn.Open();

            var userId = GetUserIdByEmail(conn, email);
            var puntosMision = GetMissionPoints(conn, misionId);

            var insertEv = @"
                INSERT INTO DB2INST1.EVIDENCIA
                (USUARIOID, MISIONID, DESCRIPCION)
                VALUES (@u, @m, @d)";

            using var cmdEv = new DB2Command(insertEv, conn);

            cmdEv.Parameters.Add(new DB2Parameter("@u", userId));
            cmdEv.Parameters.Add(new DB2Parameter("@m", misionId));
            cmdEv.Parameters.Add(new DB2Parameter("@d", descripcion ?? ""));

            cmdEv.ExecuteNonQuery();

            var getId = @"SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1";
            using var cmdId = new DB2Command(getId, conn);

            var evidenciaId = Convert.ToInt32(cmdId.ExecuteScalar());

            // =========================
            // 📸 IMAGEN
            // =========================
            string? ruta = null;

            if (imagen != null && imagen.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imagen.FileName);

                var uploads = Path.Combine(_env.WebRootPath, "uploads");
                Directory.CreateDirectory(uploads);

                var path = Path.Combine(uploads, fileName);

                using var stream = new FileStream(path, FileMode.Create);
                imagen.CopyTo(stream);

                ruta = "/uploads/" + fileName;
            }

            if (ruta != null)
            {
                var insertImg = @"
                    INSERT INTO DB2INST1.EVIDENCIAIMAGEN
                    (EVIDENCIAID, RUTAIMAGEN)
                    VALUES (@id, @ruta)";

                using var cmdImg = new DB2Command(insertImg, conn);

                cmdImg.Parameters.Add(new DB2Parameter("@id", evidenciaId));
                cmdImg.Parameters.Add(new DB2Parameter("@ruta", ruta));

                cmdImg.ExecuteNonQuery();
            }

            ActualizarProgresoUsuario(conn, userId, puntosMision);
        }

        // =========================
        // 👤 USER
        // =========================
        private int GetUserIdByEmail(DB2Connection conn, string email)
        {
            var sql = @"SELECT ID FROM DB2INST1.USUARIO WHERE EMAIL = @email";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@email", email));

            var result = cmd.ExecuteScalar();

            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Usuario no encontrado");

            return Convert.ToInt32(result);
        }

        // =========================
        // 🎯 PUNTOS
        // =========================
        private int GetMissionPoints(DB2Connection conn, int misionId)
        {
            var sql = @"SELECT PUNTOS FROM DB2INST1.MISION WHERE ID = @id";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", misionId));

            var result = cmd.ExecuteScalar();

            return result == null || result == DBNull.Value
                ? 0
                : Convert.ToInt32(result);
        }

        // =========================
        // 📈 PROGRESO
        // =========================
        private void ActualizarProgresoUsuario(
            DB2Connection conn,
            int userId,
            int puntosMision)
        {
            var sql = @"
                SELECT COALESCE(PUNTOS_TOTALES,0)
                FROM DB2INST1.USUARIO
                WHERE ID=@id";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", userId));

            var actual = cmd.ExecuteScalar();
            var puntosActuales = actual == null ? 0 : Convert.ToInt32(actual);

            var nuevos = puntosActuales + puntosMision;
            var nivel = (nuevos / 100) + 1;

            var update = @"
                UPDATE DB2INST1.USUARIO
                SET PUNTOS_TOTALES=@p,
                    NIVEL=@n
                WHERE ID=@id";

            using var cmdUp = new DB2Command(update, conn);

            cmdUp.Parameters.Add(new DB2Parameter("@p", nuevos));
            cmdUp.Parameters.Add(new DB2Parameter("@n", nivel));
            cmdUp.Parameters.Add(new DB2Parameter("@id", userId));

            cmdUp.ExecuteNonQuery();
        }

        // =========================
        // 💬 COMENTARIOS
        // =========================
        private List<ComentarioModel> GetComentarios(int evidenciaId)
        {
            var list = new List<ComentarioModel>();

            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                SELECT C.ID, C.CONTENIDO, C.FECHA, U.NOMBRE
                FROM DB2INST1.COMENTARIO C
                JOIN DB2INST1.USUARIO U ON U.ID=C.USUARIOID
                WHERE C.PUBLICACIONID=@id
                ORDER BY C.FECHA ASC";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", evidenciaId));

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ComentarioModel
                {
                    Id = reader.GetInt32(0),
                    Contenido = reader.GetString(1),
                    Fecha = reader.IsDBNull(2) ? DateTime.Now : reader.GetDateTime(2),
                    Nombre = reader.GetString(3)
                });
            }

            return list;
        }

        // =========================
        // 🗑 DELETE
        // =========================
        public void DeletePost(int id, string email)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                DELETE FROM DB2INST1.EVIDENCIA
                WHERE ID=@id
                AND USUARIOID=(SELECT ID FROM DB2INST1.USUARIO WHERE EMAIL=@e)";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", id));
            cmd.Parameters.Add(new DB2Parameter("@e", email));

            cmd.ExecuteNonQuery();
        }

        public void AddComment(int publicacionId, string email, string contenido)
        {
            using var conn = _factory.Create();
            conn.Open();

            var getUser = @"
                SELECT ID
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @e";

            using var cmdUser = new DB2Command(getUser, conn);
            cmdUser.Parameters.Add(new DB2Parameter("@e", email));

            var userId = Convert.ToInt32(cmdUser.ExecuteScalar());

            var sql = @"
                INSERT INTO DB2INST1.COMENTARIO
                (PUBLICACIONID, USUARIOID, CONTENIDO)
                VALUES (@p, @u, @c)";

            using var cmd = new DB2Command(sql, conn);

            cmd.Parameters.Add(new DB2Parameter("@p", publicacionId));
            cmd.Parameters.Add(new DB2Parameter("@u", userId));
            cmd.Parameters.Add(new DB2Parameter("@c", contenido));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 🌍 FEED (FIX IMÁGENES AQUÍ)
        // =========================
        public List<FeedPost> GetFeed(string email)
        {
            var feed = new List<FeedPost>();

            using var conn = _factory.Create();
            conn.Open();

            var query = @"
                SELECT
                    E.ID,
                    U.NOMBRE,
                    M.TITULO,
                    E.DESCRIPCION,
                    M.TIPO,
                    M.PUNTOS,
                    EI.RUTAIMAGEN,
                    E.FECHA,
                    U.EMAIL
                FROM DB2INST1.EVIDENCIA E
                JOIN DB2INST1.USUARIO U ON U.ID = E.USUARIOID
                JOIN DB2INST1.MISION M ON M.ID = E.MISIONID
                LEFT JOIN DB2INST1.EVIDENCIAIMAGEN EI ON EI.EVIDENCIAID = E.ID
                ORDER BY E.FECHA DESC";

            using var cmd = new DB2Command(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                string imagen = "/img/default-post.jpg";

                if (reader["RUTAIMAGEN"] != DBNull.Value)
                {
                    var r = reader["RUTAIMAGEN"].ToString();

                    if (!string.IsNullOrWhiteSpace(r))
                        imagen = r.StartsWith("/")
                            ? r
                            : "/uploads/" + r;
                }

                feed.Add(new FeedPost
                {
                    EvidenciaId = Convert.ToInt32(reader["ID"]),
                    Nombre = reader["NOMBRE"]?.ToString() ?? "",
                    Titulo = reader["TITULO"]?.ToString() ?? "",
                    Descripcion = reader["DESCRIPCION"]?.ToString() ?? "",
                    Tipo = reader["TIPO"]?.ToString() ?? "",
                    Puntos = reader["PUNTOS"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PUNTOS"]),
                    Imagen = imagen,
                    Fecha = reader["FECHA"] == DBNull.Value ? DateTime.Now : Convert.ToDateTime(reader["FECHA"]),
                    EsMia = reader["EMAIL"]?.ToString() == email
                });
            }

            return feed;
        }
    }
}