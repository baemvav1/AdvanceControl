namespace Advance_Control.Utilities
{
    /// <summary>Qué tanto de las operaciones/facturas pendientes puede ver el usuario logueado.</summary>
    public enum AlcanceOperaciones
    {
        Todo,
        PorArea,
        PorTecnico,
        SinAcceso
    }

    /// <summary>
    /// Resuelve el alcance de visibilidad del dashboard ("Ops") a partir del nivel del usuario.
    /// Catálogo real verificado contra la tabla `niveles` (no versionada en migraciones, con drift
    /// respecto a comentarios viejos en el backend que decían "nivel IN (7,8) -- TecSup, Tecnico" --
    /// eso está mal, el Supervisor Tecnico real es nivel 5, no 7):
    ///   1 Senior Devs, 2 Junior Devs, 3 Director, 4 Director Tecnico, 6 Senior Admin, 7 Junior Admin -> Todo
    ///   5 Supervisor Tecnico                                                                          -> PorArea
    ///   8 Tecnico, 10 Junior Tecnico                                                                   -> PorTecnico
    ///   9 Senior Cliente (y cualquier nivel no reconocido)                                             -> SinAcceso
    /// </summary>
    public static class AlcanceOperacionesResolver
    {
        public static AlcanceOperaciones Resolver(int nivel) => nivel switch
        {
            1 or 2 or 3 or 4 or 6 or 7 => AlcanceOperaciones.Todo,
            5 => AlcanceOperaciones.PorArea,
            8 or 10 => AlcanceOperaciones.PorTecnico,
            _ => AlcanceOperaciones.SinAcceso
        };
    }
}
