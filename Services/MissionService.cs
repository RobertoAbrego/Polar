using IBM.Data.Db2;
using Polar.Models;

namespace Polar.Services
{
    public class MissionService
    {
        private readonly string _connectionString =
            "Server=db2:50000;" +
            "Database=POLAR;" +
            "UID=db2inst1;" +
            "PWD=Password123;";

        public List<Mision> GetAll()
        {
            var lista = new List<Mision>();

            using var conn =
                new DB2Connection(_connectionString);

            conn.Open();

            var query = @"
                SELECT
                    ID,
                    TITULO,
                    DESCRIPCION,
                    TIPO,
                    PUNTOS
                FROM DB2INST1.MISION
                ORDER BY ID DESC";

            using var cmd =
                new DB2Command(query, conn);

            using var reader =
                cmd.ExecuteReader();

            while (reader.Read())
            {
                lista.Add(new Mision
                {
                    Id =
                        Convert.ToInt32(reader["ID"]),

                    Titulo =
                        reader["TITULO"].ToString(),

                    Descripcion =
                        reader["DESCRIPCION"].ToString(),

                    Tipo =
                        reader["TIPO"].ToString(),

                    Puntos =
                        Convert.ToInt32(reader["PUNTOS"])
                });
            }

            return lista;
        }
    }
}