namespace SISTEMADANIELITO.Models;

/// <summary>
/// Maestros a cargo de un grupo. Un grupo puede tener varios y un maestro puede
/// estar en varios grupos, por eso la relación necesita su propia tabla.
/// La Directora y el Administrador son quienes asignan estos maestros.
/// </summary>
public class Grupo_Maestro
{
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;

    public int PerfilId { get; set; }
    public Perfil Perfil { get; set; } = null!;
}
