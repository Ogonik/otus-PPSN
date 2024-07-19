using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Server.Models.User;
using Server.Services;
using System.Security.Claims;
using System.Text.RegularExpressions;
using BC = BCrypt.Net.BCrypt;

namespace Server.Controllers
{
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserRepository _userRepository;

        public AuthController(ILogger<UserController> logger, IUserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        [HttpPost("auth")]
        [AllowAnonymous]
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

                //var checkUserExists = _checkUserExists(username);

                /*if (checkUserExists)
                {
                    var passwordCheckIsOk = _checkUserPassword(username, password);

                    if (passwordCheckIsOk)
                    {
                        var claimsIdentity = _getClaimsIdentity(userIfExists);
                        var authProperties = new AuthenticationProperties();

                        return Results.SignIn(new ClaimsPrincipal(claimsIdentity), authProperties, CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }*/
                return Results.Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                return Results.StatusCode(500);
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EndpointSummary("Otus specification auth method")]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(AccessToken), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IResult Login(
            [FromForm][SwaggerTryItOutDefaulValue("36f01d62-e8cb-4d98-9a2c-65f12d5c61fc")] Guid id,
            [FromForm][SwaggerTryItOutDefaulValue("string")] string password)
        {
            _logger.LogInformation("Login started");
            if (!_checkInputData(id, password))
                return Results.BadRequest();

            var userIfExists = _userRepository.GetUserById(id).Result;

            if (userIfExists is not null)
            {
                _logger.LogInformation("User found by id");
                if (BC.Verify(password, userIfExists.Password))
                {
                    var claimsIdentity = _getClaimsIdentity(userIfExists);
                    var authProperties = new AuthenticationProperties();
                   
                    return Results.SignIn(new ClaimsPrincipal(claimsIdentity), authProperties, BearerTokenDefaults.AuthenticationScheme);
                }
            }
            _logger.LogInformation("Password check failed or user not found");
            return Results.Unauthorized();
        }

        private ClaimsIdentity _getClaimsIdentity(User userIfExists)
        {
            var claims = new List<Claim>()
                    {
                        new(ClaimTypes.Name, userIfExists.Email),
                        new("FullName", userIfExists.LastName),
                        new(ClaimTypes.Role, "User"),
                    };

            _logger.LogInformation("Password ok");
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            return claimsIdentity;
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

        private bool _checkInputData(Guid id, string password)
        {
            const int maxPasswordLength = 40;

            if (id == Guid.Empty) return false;

            if (password.Length > maxPasswordLength) return false;

            return true;
        }
    }
}
