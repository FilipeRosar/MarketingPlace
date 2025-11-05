using System.Security.Claims;

namespace MarketplaceArtesanato.API.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            if (user.Identity?.IsAuthenticated != true)
                throw new UnauthorizedAccessException("Usuário não autenticado");

            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)
                       ?? user.FindFirst("sub")
                       ?? user.FindFirst("id");

            if (idClaim == null || !Guid.TryParse(idClaim.Value, out _))
                throw new UnauthorizedAccessException("ID do usuário inválido no token");

            return Guid.Parse(idClaim.Value);
        }
    }
}
