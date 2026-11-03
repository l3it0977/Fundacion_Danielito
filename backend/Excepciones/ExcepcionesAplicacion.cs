namespace SISTEMADANIELITO.Excepciones;

/// <summary>
/// Se lanza cuando se pide un registro que no existe en la base de datos.
/// El middleware la traduce a un HTTP 404 (No encontrado).
/// </summary>
public class RecursoNoEncontradoException : Exception
{
    public RecursoNoEncontradoException(string mensaje) : base(mensaje)
    {
    }
}

/// <summary>
/// Se lanza cuando los datos recibidos son válidos como formato, pero rompen una regla
/// del negocio (por ejemplo un código de niño repetido, o un estado de asistencia inventado).
/// El middleware la traduce a un HTTP 400 (Petición incorrecta).
/// </summary>
public class ReglaNegocioException : Exception
{
    public ReglaNegocioException(string mensaje) : base(mensaje)
    {
    }
}
