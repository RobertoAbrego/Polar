using IBM.Data.Db2;
using Polar.Models;

namespace Polar.Services
{
    public class EvidenciaService
    {
        private readonly Db2ConnectionFactory _factory;
        private readonly IWebHostEnvironment _env;

        public EvidenciaService(Db2ConnectionFactory factory, IWebHostEnvironment env)
        {
            _factory = factory;
            _env = env;
        }

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
        // CREAR EVIDENCIA + IMAGEN + DESCRIPCIÓN + PUNTOS
        // =========================
        public void Create(string email, int misionId, string descripcion, IFormFile imagen)
        {
            using var conn = _factory.Create();
            conn.Open();

            var userId = GetUserIdByEmail(conn, email);
            var puntosMision = GetMissionPoints(conn, misionId);

            // 1. insertar evidencia con descripción
            var insertEv = @"
                INSERT INTO DB2INST1.EVIDENCIA
                (USUARIOID, MISIONID, DESCRIPCION)
                VALUES (@u, @m, @d)";

            using var cmdEv = new DB2Command(insertEv, conn);
            cmdEv.Parameters.Add(new DB2Parameter("@u", userId));
            cmdEv.Parameters.Add(new DB2Parameter("@m", misionId));
            cmdEv.Parameters.Add(new DB2Parameter("@d", descripcion));
            cmdEv.ExecuteNonQuery();

            // 2. obtener último ID generado
            var getId = "SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1";
            using var cmdId = new DB2Command(getId, conn);
            var evidenciaId = Convert.ToInt32(cmdId.ExecuteScalar());

            // 3. guardar imagen en disco
            string? ruta = null;

            if (imagen != null && imagen.Length > 0)
            {
                var fileName = Guid.NewGuid() + Path.GetExtension(imagen.FileName);
                var path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                Directory.CreateDirectory(Path.Combine(_env.WebRootPath, "uploads"));

                using var stream = new FileStream(path, FileMode.Create);
                imagen.CopyTo(stream);

                ruta = "/uploads/" + fileName;
            }

            // 4. guardar ruta de imagen en DB
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

            // 5. sumar puntos al usuario y recalcular nivel
            ActualizarProgresoUsuario(conn, userId, puntosMision);
        }

        private int GetUserIdByEmail(DB2Connection conn, string email)
        {
            var sql = "SELECT ID FROM DB2INST1.USUARIO WHERE EMAIL = @email";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@email", email));

            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException("Usuario no encontrado.");

            return Convert.ToInt32(result);
        }

        private int GetMissionPoints(DB2Connection conn, int misionId)
        {
            var sql = "SELECT PUNTOS FROM DB2INST1.MISION WHERE ID = @id";
            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", misionId));

            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                return 0;

            return Convert.ToInt32(result);
        }

        private void ActualizarProgresoUsuario(DB2Connection conn, int userId, int puntosMision)
        {
            var sqlPuntos = @"
                SELECT COALESCE(PUNTOS_TOTALES, 0)
                FROM DB2INST1.USUARIO
                WHERE ID = @id";

            using var cmdGet = new DB2Command(sqlPuntos, conn);
            cmdGet.Parameters.Add(new DB2Parameter("@id", userId));

            var current = cmdGet.ExecuteScalar();
            var puntosActuales = current == null || current == DBNull.Value
                ? 0
                : Convert.ToInt32(current);

            var nuevosPuntos = puntosActuales + puntosMision;
            var nuevoNivel = (nuevosPuntos / 100) + 1;

            var sqlUpdate = @"
                UPDATE DB2INST1.USUARIO
                SET PUNTOS_TOTALES = @puntos,
                    NIVEL = @nivel
                WHERE ID = @id";

            using var cmdUpdate = new DB2Command(sqlUpdate, conn);
            cmdUpdate.Parameters.Add(new DB2Parameter("@puntos", nuevosPuntos));
            cmdUpdate.Parameters.Add(new DB2Parameter("@nivel", nuevoNivel));
            cmdUpdate.Parameters.Add(new DB2Parameter("@id", userId));

            cmdUpdate.ExecuteNonQuery();
        }

        // =========================
        // COMENTARIOS
        // =========================
        public void AddComment(int publicacionId, string email, string contenido)
        {
            using var conn = _factory.Create();
            conn.Open();

            var getUser = "SELECT ID FROM DB2INST1.USUARIO WHERE EMAIL = @e";

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

        private List<ComentarioModel> GetComentarios(int evidenciaId)
        {
            var comentarios = new List<ComentarioModel>();

            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                SELECT
                    C.ID,
                    C.CONTENIDO,
                    C.FECHA,
                    U.NOMBRE
                FROM DB2INST1.COMENTARIO C
                JOIN DB2INST1.USUARIO U
                    ON U.ID = C.USUARIOID
                WHERE C.PUBLICACIONID = @id
                ORDER BY C.FECHA ASC";

            using var cmd = new DB2Command(sql, conn);
            cmd.Parameters.Add(new DB2Parameter("@id", evidenciaId));

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                comentarios.Add(new ComentarioModel
                {
                    Id = reader.GetInt32(0),
                    Contenido = reader.GetString(1),
                    Fecha = reader.GetDateTime(2),
                    Nombre = reader.GetString(3)
                });
            }

            return comentarios;
        }

        public void DeletePost(int evidenciaId, string email)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                DELETE FROM DB2INST1.EVIDENCIA
                WHERE ID = @id
                AND USUARIOID = (
                    SELECT ID
                    FROM DB2INST1.USUARIO
                    WHERE EMAIL = @email
                )";

            using var cmd = new DB2Command(sql, conn);

            cmd.Parameters.Add(new DB2Parameter("@id", evidenciaId));
            cmd.Parameters.Add(new DB2Parameter("@email", email));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // FEED
        // =========================
        public List<FeedPost> GetFeed(string email)
        {
            var list = new List<FeedPost>();

            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
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
                JOIN DB2INST1.USUARIO U
                    ON U.ID = E.USUARIOID
                JOIN DB2INST1.MISION M
                    ON M.ID = E.MISIONID
                JOIN DB2INST1.EVIDENCIAIMAGEN EI
                    ON EI.EVIDENCIAID = E.ID
                ORDER BY E.FECHA DESC";

            using var cmd = new DB2Command(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var post = new FeedPost
                {
                    EvidenciaId = reader.GetInt32(0),
                    Nombre = reader.GetString(1),
                    Titulo = reader.GetString(2),
                    Descripcion = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    Tipo = reader.GetString(4),
                    Puntos = reader.GetInt32(5),
                    Imagen = reader.GetString(6),
                    Fecha = reader.GetDateTime(7),
                    EsMia = reader.GetString(8) == email
                };

                post.Comentarios = GetComentarios(post.EvidenciaId);
                list.Add(post);
            }

            return list;
        }
    }
}