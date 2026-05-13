using IBM.Data.Db2;
using Polar.Models;

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
                SELECT
                    ID,
                    TITULO,
                    DESCRIPCION,
                    TIPO,
                    PUNTOS
                FROM DB2INST1.MISION";

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
            IFormFile imagen,
            string descripcion)
        {
            using var conn = _factory.Create();
            conn.Open();

            // Obtener usuario

            var getUser = @"
                SELECT ID
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @email";

            using var cmdUser = new DB2Command(getUser, conn);

            cmdUser.Parameters.Add(
                new DB2Parameter("@email", email));

            var userId = Convert.ToInt32(
                cmdUser.ExecuteScalar());

            // Insertar evidencia

            var insertEv = @"
                INSERT INTO DB2INST1.EVIDENCIA
                (
                    USUARIOID,
                    MISIONID,
                    DESCRIPCION
                )
                VALUES
                (
                    @u,
                    @m,
                    @d
                )";

            using var cmdEv = new DB2Command(insertEv, conn);

            cmdEv.Parameters.Add(
                new DB2Parameter("@u", userId));

            cmdEv.Parameters.Add(
                new DB2Parameter("@m", misionId));

            cmdEv.Parameters.Add(
                new DB2Parameter("@d", descripcion ?? ""));

            cmdEv.ExecuteNonQuery();

            // Obtener ID generado

            var getId = @"
                SELECT IDENTITY_VAL_LOCAL()
                FROM SYSIBM.SYSDUMMY1";

            using var cmdId = new DB2Command(getId, conn);

            var evidenciaId = Convert.ToInt32(
                cmdId.ExecuteScalar());

            // Guardar imagen

            string? ruta = null;

            if (imagen != null && imagen.Length > 0)
            {
                var fileName =
                    Guid.NewGuid() +
                    Path.GetExtension(imagen.FileName);

                var uploads =
                    Path.Combine(_env.WebRootPath, "uploads");

                Directory.CreateDirectory(uploads);

                var path =
                    Path.Combine(uploads, fileName);

                using var stream =
                    new FileStream(path, FileMode.Create);

                imagen.CopyTo(stream);

                ruta = "/uploads/" + fileName;
            }

            // Guardar ruta imagen

            if (ruta != null)
            {
                var insertImg = @"
                    INSERT INTO DB2INST1.EVIDENCIAIMAGEN
                    (
                        EVIDENCIAID,
                        RUTAIMAGEN
                    )
                    VALUES
                    (
                        @id,
                        @ruta
                    )";

                using var cmdImg =
                    new DB2Command(insertImg, conn);

                cmdImg.Parameters.Add(
                    new DB2Parameter("@id", evidenciaId));

                cmdImg.Parameters.Add(
                    new DB2Parameter("@ruta", ruta));

                cmdImg.ExecuteNonQuery();
            }
        }

        // =========================
        // 💬 AGREGAR COMENTARIO
        // =========================

        public void AddComment(
            int publicacionId,
            string email,
            string contenido)
        {
            using var conn = _factory.Create();
            conn.Open();

            var getUser = @"
                SELECT ID
                FROM DB2INST1.USUARIO
                WHERE EMAIL = @e";

            using var cmdUser =
                new DB2Command(getUser, conn);

            cmdUser.Parameters.Add(
                new DB2Parameter("@e", email));

            var userId = Convert.ToInt32(
                cmdUser.ExecuteScalar());

            var sql = @"
                INSERT INTO DB2INST1.COMENTARIO
                (
                    PUBLICACIONID,
                    USUARIOID,
                    CONTENIDO
                )
                VALUES
                (
                    @p,
                    @u,
                    @c
                )";

            using var cmd = new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@p", publicacionId));

            cmd.Parameters.Add(
                new DB2Parameter("@u", userId));

            cmd.Parameters.Add(
                new DB2Parameter("@c", contenido));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 📖 OBTENER COMENTARIOS
        // =========================

        private List<ComentarioModel> GetComentarios(
            int evidenciaId)
        {
            var comentarios =
                new List<ComentarioModel>();

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

            using var cmd =
                new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@id", evidenciaId));

            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                comentarios.Add(new ComentarioModel
                {
                    Id = reader.GetInt32(0),

                    Contenido = reader.GetString(1),

                    Fecha = reader.IsDBNull(2)
                        ? DateTime.Now
                        : reader.GetDateTime(2),

                    Nombre = reader.GetString(3)
                });
            }

            return comentarios;
        }

        // =========================
        // 🗑 ELIMINAR POST
        // =========================

        public void DeletePost(
            int evidenciaId,
            string email)
        {
            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                DELETE FROM DB2INST1.EVIDENCIA
                WHERE ID = @id
                AND USUARIOID =
                (
                    SELECT ID
                    FROM DB2INST1.USUARIO
                    WHERE EMAIL = @email
                )";

            using var cmd = new DB2Command(sql, conn);

            cmd.Parameters.Add(
                new DB2Parameter("@id", evidenciaId));

            cmd.Parameters.Add(
                new DB2Parameter("@email", email));

            cmd.ExecuteNonQuery();
        }

        // =========================
        // 🌍 FEED
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
                    M.TIPO,
                    M.PUNTOS,
                    EI.RUTAIMAGEN,
                    E.FECHA,
                    U.EMAIL,
                    E.DESCRIPCION
                FROM DB2INST1.EVIDENCIA E

                JOIN DB2INST1.USUARIO U
                    ON U.ID = E.USUARIOID

                JOIN DB2INST1.MISION M
                    ON M.ID = E.MISIONID

                JOIN DB2INST1.EVIDENCIAIMAGEN EI
                    ON EI.EVIDENCIAID = E.ID

                ORDER BY E.FECHA DESC";

            using var cmd =
                new DB2Command(sql, conn);

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                var post = new FeedPost
                {
                    EvidenciaId = reader.GetInt32(0),

                    Nombre = reader.GetString(1),

                    Titulo = reader.GetString(2),

                    Tipo = reader.GetString(3),

                    Puntos = reader.GetInt32(4),

                    Imagen = reader.GetString(5),

                    Fecha = reader.GetDateTime(6),

                    EsMia = reader.GetString(7) == email,

                    Descripcion = reader.IsDBNull(8)
                        ? ""
                        : reader.GetString(8)
                };

                post.Comentarios =
                    GetComentarios(post.EvidenciaId);

                list.Add(post);
            }

            return list;
        }
    }
}