using BackgroundService.Data;
using BackgroundService.DTOs;
using BackgroundService.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace BackgroundService.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        BackgroundServiceContext context)
        : ControllerBase
    {
        [HttpPost]
        public async Task<ActionResult> Register(RegisterDTO register)
        {
            if (register.Password != register.PasswordConfirm)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { Message = "Les deux mots de passe spécifiés sont différents." });
            }
            IdentityUser user = new IdentityUser()
            {
                UserName = register.Username,
                Email = register.Email
            };
            IdentityResult identityResult = await userManager.CreateAsync(user, register.Password);
            if (!identityResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Message = "La création de l'utilisateur a échoué." });
            }

            Player player = new()
            {
                Id = 0,
                User = user,
                UserId = user.Id,
            };

            context.Player.Add(player);
            await context.SaveChangesAsync();

            return Ok(new { Message = "Inscription réussie ! 🥳" });
        }

        [HttpPost]
        public async Task<ActionResult> Login(LoginDTO login)
        {
            SignInResult result = await signInManager.PasswordSignInAsync(login.Username, login.Password, true, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                Claim? nameIdentifierClaim = User.Claims?.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

                // Note: On ajoute simplement le NameIdentifier dans les claims. Il n'y aura pas de rôle pour les utilisateurs du WebAPI.
                List<Claim> authClaims =
                [
                    nameIdentifierClaim
                ];

                SymmetricSecurityKey signingKey = new("C'est tellement la meilleure cle qui a jamais ete cree dans l'histoire de l'humanite (doit etre longue)"u8.ToArray());

                string issuer = Request.Scheme + "://" + Request.Host;

                DateTime expirationTime = DateTime.Now.AddMinutes(30);

                JwtSecurityToken token = new(
                    issuer: issuer,
                    audience: null,
                    claims: authClaims,
                    expires: expirationTime,
                    signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256Signature)
                );

                string tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                return Ok(new { Token = tokenString });
            }

            return NotFound(new { Error = "L'utilisateur est introuvable ou le mot de passe de concorde pas" });
        }
    }
}
