using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using System.Diagnostics.Eventing.Reader;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Server.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly NpgsqlConnection _connection;

        public AuthController(NpgsqlConnection connection, ILogger<UserController> logger)
        {
            _connection = connection;
            _logger = logger;
        }

        [HttpPost("auth")]
        [EndpointSummary("General ordinary auth method")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Consumes("application/x-www-form-urlencoded")]
        /// summary
        /// Упрощенный процесс аутентификации путем передачи идентификатор пользователя и получения токена для дальнейшего прохождения авторизации
        public IResult Login([FromForm] string username, [FromForm] string password)
        {
            try
            {
                if (!_checkInputData(username, password))
                    return Results.BadRequest();

                var checkUserExists = _checkUserExists(username);

                if (checkUserExists)
                {
                    var passwordCheckIsOk = _checkUserPassword(username, password);

                    if (passwordCheckIsOk)
                    {
                        var claimsIdentity = new ClaimsIdentity(BearerTokenDefaults.AuthenticationScheme);
                        claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, username));

                        return Results.SignIn(new ClaimsPrincipal(claimsIdentity));
                    }
                }
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                return Results.StatusCode(500);
            }
        }

        [HttpPost("login")]
        [EndpointSummary("Otus specification auth method")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IResult Login(Guid id, string password)
        {
            var claimsIdentity = new ClaimsIdentity(BearerTokenDefaults.AuthenticationScheme);
            claimsIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

            
            return Results.SignIn(new ClaimsPrincipal(claimsIdentity));
        }

        private bool _checkInputData(string username, string password)
        {
            const int maxUserNameLength = 50;
            const int maxPasswordLength = 255;
            var alphanumericRegex = "^[a-zA-Z]+$";

            if (username.Length > maxUserNameLength) return false;
            if (!Regex.IsMatch(alphanumericRegex, username)) return false;

            if (password.Length > maxPasswordLength) return false;
            if (!Regex.IsMatch(alphanumericRegex, password)) return false;

            return true;
        }

        private bool _checkUserPassword(string username, string password)
        {
            return true;
        }

        private bool _checkUserExists(string username)
        {
            return true;
        }
    }
}
