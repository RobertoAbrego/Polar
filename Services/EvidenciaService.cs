using IBM.Data.Db2;

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

        // 🔥 CREAR EVIDENCIA + IMAGEN
        public void Create(string email, int misionId, IFormFile imagen)
        {
            using var conn = _factory.Create();
            conn.Open();

            // 1. obtener usuario
            var getUser = "SELECT ID FROM DB2INST1.USUARIO WHERE EMAIL = @email";
            using var cmdUser = new DB2Command(getUser, conn);
            cmdUser.Parameters.Add(new DB2Parameter("@email", email));

            var userId = (int)cmdUser.ExecuteScalar();

            // 2. insertar evidencia
            var insertEv = @"INSERT INTO DB2INST1.EVIDENCIA (USUARIOID, MISIONID)
                             VALUES (@u, @m)";

            using var cmdEv = new DB2Command(insertEv, conn);
            cmdEv.Parameters.Add(new DB2Parameter("@u", userId));
            cmdEv.Parameters.Add(new DB2Parameter("@m", misionId));
            cmdEv.ExecuteNonQuery();

            // 3. obtener último ID
            var getId = "SELECT IDENTITY_VAL_LOCAL() FROM SYSIBM.SYSDUMMY1";
            using var cmdId = new DB2Command(getId, conn);
            var evidenciaId = Convert.ToInt32(cmdId.ExecuteScalar());

            // 4. guardar imagen
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

            // 5. guardar imagen en DB
            if (ruta != null)
            {
                var insertImg = @"INSERT INTO DB2INST1.EVIDENCIAIMAGEN 
                                  (EVIDENCIAID, RUTAIMAGEN)
                                  VALUES (@id, @ruta)";

                using var cmdImg = new DB2Command(insertImg, conn);
                cmdImg.Parameters.Add(new DB2Parameter("@id", evidenciaId));
                cmdImg.Parameters.Add(new DB2Parameter("@ruta", ruta));

                cmdImg.ExecuteNonQuery();
            }
        }

        // 🔥 FEED (ESTO ES LO NUEVO)
        public List<dynamic> GetFeed()
        {
            var list = new List<dynamic>();

            using var conn = _factory.Create();
            conn.Open();

            var sql = @"
                SELECT U.EMAIL, EI.RUTAIMAGEN, E.FECHA
                FROM DB2INST1.EVIDENCIA E
                JOIN DB2INST1.USUARIO U ON U.ID = E.USUARIOID
                JOIN DB2INST1.EVIDENCIAIMAGEN EI ON EI.EVIDENCIAID = E.ID
                ORDER BY E.FECHA DESC";

            using var cmd = new DB2Command(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new
                {
                    Email = reader.GetString(0),
                    Imagen = reader.GetString(1),
                    Fecha = reader.GetDateTime(2)
                });
            }

            return list;
        }
    }
}