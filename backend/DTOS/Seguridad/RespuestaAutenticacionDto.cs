namespace SISTEMADANIELITO.DTOS;

public class RespuestaAutenticacionDto
{
    public string Token { get; set; } = string.Empty;
    public DateTime Expiracion { get; set; }
}
