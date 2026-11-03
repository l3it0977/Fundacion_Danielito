namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Datos del panel que ven la Directora y el Administrador.
/// Reemplaza las consultas con SQL crudo que estaban escritas dentro de routes/web.php.
/// </summary>
public class DashboardGeneralDTO
{
    public int Total_Ninos { get; set; }
    public int Total_Grupos { get; set; }
    public int Total_Actividades { get; set; }
    public int Total_Asistencias { get; set; }
    public int Ninos_Vulnerables { get; set; }

    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public int Justificados { get; set; }

    /// <summary>Porcentaje de presentes sobre el total de registros de asistencia.</summary>
    public double Porcentaje_Asistencia { get; set; }

    public List<AsistenciaPorGrupoDTO> Asistencia_Por_Grupo { get; set; } = [];
    public List<ActividadResumenDTO> Ultimas_Actividades { get; set; } = [];
    public List<NinoResumenDTO> Lista_Vulnerables { get; set; } = [];
}

/// <summary>Conteo de asistencias de un grupo, desglosado por estado.</summary>
public class AsistenciaPorGrupoDTO
{
    public string Grupo { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public int Justificados { get; set; }
}

/// <summary>Versión reducida de una actividad, para listarla en los paneles.</summary>
public class ActividadResumenDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateOnly? Fecha_Actividad { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public List<string> Grupos { get; set; } = [];
}

/// <summary>Versión reducida de un niño, para listarlo en los paneles.</summary>
public class NinoResumenDTO
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre_Completo { get; set; } = string.Empty;
    public string Grupo { get; set; } = string.Empty;
    public string? Motivo_Vulnerabilidad { get; set; }
}
