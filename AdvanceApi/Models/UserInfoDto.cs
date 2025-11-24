namespace AdvanceApi.Models
{
    public class UserInfoDto
    {
        public int CredencialId { get; set; }
        public string? NombreCompleto { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public int Nivel { get; set; }
        public string? TipoUsuario { get; set; }
    }
}
