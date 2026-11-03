using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Data;

public class AppProyectoContext : DbContext
{
    public AppProyectoContext(DbContextOptions<AppProyectoContext> options) : base(options)
    {
    }

    public DbSet<Perfil> Perfiles { get; set; }
    public DbSet<Grupo> Grupos { get; set; }
    public DbSet<Grupo_Maestro> Grupo_Maestros { get; set; }
    public DbSet<Nino> Ninos { get; set; }
    public DbSet<Actividad> Actividades { get; set; }
    public DbSet<Asistencia> Asistencias { get; set; }
    public DbSet<Visita> Visitas { get; set; }
    public DbSet<Actividad_Nino> Actividad_Ninos { get; set; }
    public DbSet<Bitacora> Bitacoras { get; set; }

    //RELACIONES
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Perfil>()
            .HasIndex(x => x.UsuarioId)
            .IsUnique();

        //-----Bitacora-----
        // Se consulta de la más reciente a la más antigua, y por entidad.
        modelBuilder.Entity<Bitacora>()
            .HasIndex(x => x.Fecha);

        modelBuilder.Entity<Bitacora>()
            .HasIndex(x => new { x.Entidad, x.EntidadId });

        //-----Grupo_Maestro, Grupo y Perfil-----
        // Un grupo tiene varios maestros y un maestro varios grupos, así que la
        // relación vive en su propia tabla con clave compuesta.
        modelBuilder.Entity<Grupo_Maestro>()
            .HasKey(x => new { x.GrupoId, x.PerfilId });

        modelBuilder.Entity<Grupo_Maestro>()
            .HasOne(x => x.Grupo)
            .WithMany(x => x.Grupo_Maestros)
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict hacia Perfil: dar de baja a alguien no puede arrastrar grupos
        // ni el historial que registró.
        modelBuilder.Entity<Grupo_Maestro>()
            .HasOne(x => x.Perfil)
            .WithMany(x => x.Grupo_Maestros)
            .HasForeignKey(x => x.PerfilId)
            .OnDelete(DeleteBehavior.Restrict);

        //-----Nino y Grupo-----
        modelBuilder.Entity<Nino>()
            .HasOne(x => x.Grupo)
            .WithMany(x => x.Ninos)
            .HasForeignKey(x => x.GrupoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Nino>()
            .HasIndex(x => x.Codigo)
            .IsUnique();

        modelBuilder.Entity<Nino>()
            .Property(x => x.Latitud)
            .HasPrecision(10, 8);

        modelBuilder.Entity<Nino>()
            .Property(x => x.Longitud)
            .HasPrecision(11, 8);

        //-----Actividad y Perfil-----
        modelBuilder.Entity<Actividad>()
            .HasOne(x => x.CreadoPor)
            .WithMany(x => x.Actividades)
            .HasForeignKey(x => x.CreadoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Actividad>()
            .HasIndex(x => x.Fecha_Actividad);

        modelBuilder.Entity<Actividad>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Actividades_Tipo",
                "\"Tipo\" IN ('normal', 'intervencion')"));

        //-----Asistencia, Actividad, Nino y Perfil-----
        modelBuilder.Entity<Asistencia>()
            .HasOne(x => x.Actividad)
            .WithMany(x => x.Asistencias)
            .HasForeignKey(x => x.ActividadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict hacia Nino: borrar a un niño no puede arrastrar su historial. Se desactiva.
        modelBuilder.Entity<Asistencia>()
            .HasOne(x => x.Nino)
            .WithMany(x => x.Asistencias)
            .HasForeignKey(x => x.NinoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asistencia>()
            .HasOne(x => x.RegistradoPor)
            .WithMany(x => x.Asistencias_Registradas)
            .HasForeignKey(x => x.RegistradoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        // Un niño tiene una sola asistencia por actividad y fecha. Sin esta restricción,
        // dos envíos simultáneos del mismo formulario crearían registros duplicados.
        modelBuilder.Entity<Asistencia>()
            .HasIndex(x => new { x.ActividadId, x.NinoId, x.Fecha })
            .IsUnique();

        // Los reportes y el panel filtran por rango de fechas.
        modelBuilder.Entity<Asistencia>()
            .HasIndex(x => x.Fecha);

        // 'pendiente' es el estado con el que nace la lista al crear la actividad.
        modelBuilder.Entity<Asistencia>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_Asistencias_Estado",
                "\"Estado\" IN ('pendiente', 'presente', 'ausente', 'justificado')"));

        //-----Visita, Nino y Perfil-----
        modelBuilder.Entity<Visita>()
            .HasOne(x => x.Nino)
            .WithMany(x => x.Visitas)
            .HasForeignKey(x => x.NinoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Visita>()
            .HasOne(x => x.RealizadoPor)
            .WithMany(x => x.Visitas_Realizadas)
            .HasForeignKey(x => x.RealizadoPorId)
            .OnDelete(DeleteBehavior.Restrict);

        //-----Actividad_Nino, Actividad y Nino-----
        modelBuilder.Entity<Actividad_Nino>()
            .HasKey(x => new { x.ActividadId, x.NinoId });

        modelBuilder.Entity<Actividad_Nino>()
            .HasOne(x => x.Actividad)
            .WithMany(x => x.Actividad_Ninos)
            .HasForeignKey(x => x.ActividadId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Actividad_Nino>()
            .HasOne(x => x.Nino)
            .WithMany(x => x.Actividad_Ninos)
            .HasForeignKey(x => x.NinoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
