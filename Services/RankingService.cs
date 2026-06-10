// ============================================================
// RUTA: Services/RankingService.cs        (ARCHIVO NUEVO)
// ============================================================
//
// Schema real usado:
//   USUARIO      → ID, NOMBRE, FOTO_PERFIL, NIVEL, PUNTOS_TOTALES
//   EVIDENCIA    → ID, USUARIOID, MISIONID, APROBADA (SMALLINT: 1=aprobada)
//
// PUNTOS_TOTALES y NIVEL ya están en USUARIO, no hay que calcularlos.
// Solo contamos las EVIDENCIA con APROBADA = 1 para "misiones completadas".
// ============================================================

using IBM.Data.Db2;
using Polar.Models;

namespace Polar.Services
{
    public class RankingService
    {
        private readonly Db2ConnectionFactory    _db;
        private readonly ILogger<RankingService> _log;

        public RankingService(Db2ConnectionFactory db, ILogger<RankingService> log)
        {
            _db  = db;
            _log = log;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Devuelve el TOP N de usuarios ordenados por PUNTOS_TOTALES DESC
        // ─────────────────────────────────────────────────────────────────────
        public async Task<List<RankingUsuario>> ObtenerTopAsync(int top = 20)
        {
            var lista = new List<RankingUsuario>();
            try
            {
                using var conn = _db.Create();
                await conn.OpenAsync();

                // Leemos PUNTOS_TOTALES y NIVEL directo de USUARIO (ya los tiene).
                // LEFT JOIN con EVIDENCIA solo para contar misiones aprobadas.
                // FETCH FIRST n ROWS ONLY es la sintaxis DB2 para limitar filas.
                string sql = $@"
                    SELECT
                        u.ID                            AS USRID,
                        u.NOMBRE                        AS USRNOMBRE,
                        u.FOTO_PERFIL                   AS USRFOTO,
                        u.NIVEL                         AS USRNIVEL,
                        u.PUNTOS_TOTALES                AS USRPUNTOS,
                        COUNT(e.ID)                     AS MISIONESOK
                    FROM USUARIO u
                    LEFT JOIN EVIDENCIA e
                        ON e.USUARIOID = u.ID
                       AND e.APROBADA  = 1
                    GROUP BY u.ID, u.NOMBRE, u.FOTO_PERFIL, u.NIVEL, u.PUNTOS_TOTALES
                    ORDER BY u.PUNTOS_TOTALES DESC
                    FETCH FIRST {top} ROWS ONLY";

                using var cmd   = conn.CreateCommand();
                cmd.CommandText = sql;

                using var reader = await cmd.ExecuteReaderAsync();
                int pos = 1;
                while (await reader.ReadAsync())
                {
                    lista.Add(new RankingUsuario
                    {
                        Posicion            = pos++,
                        UsuarioId           = Convert.ToInt32(reader["USRID"]),
                        NombreUsuario       = reader["USRNOMBRE"].ToString()!,
                        FotoPerfil          = reader["USRFOTO"] == DBNull.Value
                                                ? string.Empty
                                                : reader["USRFOTO"].ToString()!,
                        Nivel               = Convert.ToInt32(reader["USRNIVEL"]),
                        PuntosTotales       = Convert.ToInt32(reader["USRPUNTOS"]),
                        MisionesCompletadas = Convert.ToInt32(reader["MISIONESOK"])
                    });
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al obtener ranking global");
            }
            return lista;
        }

        // ─────────────────────────────────────────────────────────────────────
        // Posición exacta de un usuario específico en el ranking global.
        // Usa ROW_NUMBER() OVER (...) de DB2.
        // ─────────────────────────────────────────────────────────────────────
        public async Task<RankingUsuario?> ObtenerMiPosicionAsync(int usuarioId)
        {
            try
            {
                using var conn = _db.Create();
                await conn.OpenAsync();

                const string sql = @"
                    SELECT *
                    FROM (
                        SELECT
                            u.ID                            AS USRID,
                            u.NOMBRE                        AS USRNOMBRE,
                            u.FOTO_PERFIL                   AS USRFOTO,
                            u.NIVEL                         AS USRNIVEL,
                            u.PUNTOS_TOTALES                AS USRPUNTOS,
                            COUNT(e.ID)                     AS MISIONESOK,
                            ROW_NUMBER() OVER (
                                ORDER BY u.PUNTOS_TOTALES DESC
                            )                               AS POSICION
                        FROM USUARIO u
                        LEFT JOIN EVIDENCIA e
                            ON e.USUARIOID = u.ID
                           AND e.APROBADA  = 1
                        GROUP BY u.ID, u.NOMBRE, u.FOTO_PERFIL, u.NIVEL, u.PUNTOS_TOTALES
                    ) RK
                    WHERE USRID = @UID";

                using var cmd   = conn.CreateCommand();
                cmd.CommandText = sql;
                cmd.Parameters.Add(new DB2Parameter("@UID", usuarioId));

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    return new RankingUsuario
                    {
                        Posicion            = Convert.ToInt32(reader["POSICION"]),
                        UsuarioId           = Convert.ToInt32(reader["USRID"]),
                        NombreUsuario       = reader["USRNOMBRE"].ToString()!,
                        FotoPerfil          = reader["USRFOTO"] == DBNull.Value
                                                ? string.Empty
                                                : reader["USRFOTO"].ToString()!,
                        Nivel               = Convert.ToInt32(reader["USRNIVEL"]),
                        PuntosTotales       = Convert.ToInt32(reader["USRPUNTOS"]),
                        MisionesCompletadas = Convert.ToInt32(reader["MISIONESOK"])
                    };
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error al obtener posición del usuario {Id}", usuarioId);
            }
            return null;
        }
    }
}