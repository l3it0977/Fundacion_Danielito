using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

/// <summary>
/// Un renglón del registro de auditoría: quién cambió qué y cuándo. Lo llena automáticamente
/// AuditoriaInterceptor; nadie lo edita ni lo borra desde la aplicación.
/// </summary>
public class Bitacora
{
    public const string AccionCreado = "Creado";
    public const string AccionModificado = "Modificado";
    public const string AccionEliminado = "Eliminado";

    [Key]
    public int Id { get; set; }

    // Siempre en UTC: la pantalla lo muestra en la hora local de quien lo mira.
    public DateTime Fecha { get; set; }

    // Sin clave foránea a propósito: el registro debe sobrevivir aunque el perfil cambie.
    public int? PerfilId { get; set; }

    // Copia del nombre en ese momento, por si después la persona cambia de nombre.
    [Required]
    [StringLength(255)]
    public string Usuario { get; set; } = "Sistema";

    [Required]
    [StringLength(50)]
    public string Entidad { get; set; } = string.Empty;

    public int? EntidadId { get; set; }

    [Required]
    [StringLength(30)]
    public string Accion { get; set; } = string.Empty;

    // Texto para reconocer el registro sin abrirlo: el nombre del niño, del grupo, etc.
    [StringLength(500)]
    public string Descripcion { get; set; } = string.Empty;

    // JSON con los campos modificados: { "Campo": { "antes": ..., "despues": ... } }.
    public string? Cambios { get; set; }
}
