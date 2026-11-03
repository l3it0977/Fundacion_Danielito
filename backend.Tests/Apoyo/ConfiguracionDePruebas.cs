using System.Runtime.CompilerServices;

namespace SISTEMADANIELITO.Tests.Apoyo;

internal static class ConfiguracionDePruebas
{
    /// <summary>QuestPDF exige declarar la licencia antes de generar cualquier PDF.</summary>
    [ModuleInitializer]
    internal static void Iniciar()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    }
}
