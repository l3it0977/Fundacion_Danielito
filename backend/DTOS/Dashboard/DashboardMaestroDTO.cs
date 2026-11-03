namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Datos del panel de un Maestro: solo lo referente a los niños de los grupos que tiene a cargo.
/// </summary>
public class DashboardMaestroDTO
{
    public int Total_Mis_Ninos { get; set; }
    public int Total_Mis_Actividades { get; set; }
    public int Total_Mis_Asistencias { get; set; }

    /// <summary>Niños que cumplen años dentro de los próximos 30 días.</summary>
    public List<CumpleanosDTO> Cumpleanos_Cercanos { get; set; } = [];

    /// <summary>Los 5 niños con más asistencias marcadas como presente.</summary>
    public List<TopAsistenteDTO> Mis_Mas_Asistentes { get; set; } = [];

    /// <summary>Resumen de asistencia del mes en curso.</summary>
    public ResumenAsistenciaDTO Asistencia_Del_Mes { get; set; } = new();
}

public class CumpleanosDTO
{
    public int NinoId { get; set; }
    public string Nombre_Completo { get; set; } = string.Empty;
    public DateTime Fecha_Nacimiento { get; set; }
    public int Edad_Que_Cumple { get; set; }
    public int Dias_Restantes { get; set; }
}

public class TopAsistenteDTO
{
    public int NinoId { get; set; }
    public string Nombre_Completo { get; set; } = string.Empty;
    public int Total_Asistencias { get; set; }
}

public class ResumenAsistenciaDTO
{
    public int Total { get; set; }
    public int Presentes { get; set; }
    public int Ausentes { get; set; }
    public int Justificados { get; set; }
    public double Porcentaje_Asistencia { get; set; }
}
