using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Server.Models.User;
using Server.Services;

namespace Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController(ILogger<UserController> _logger, IUserRepository _userRepository, IWebHostEnvironment hostingEnvironment) : ControllerBase
    {
        [HttpGet]
        [Route("get")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [EndpointSummary("Try get user by its id")]
        [Consumes("application/json")]
        public async Task<ActionResult<UserGetResponse>> Get([SwaggerTryItOutDefaulValue("36f01d62-e8cb-4d98-9a2c-65f12d5c61fc")] Guid id)
        {
            return Ok((UserGetResponse)_userRepository.GetUserById(id).Result);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [EndpointSummary("Registers user in the system")]
        [Consumes("application/x-www-form-urlencoded")]
        public async Task<ActionResult<UserRegisterResponse>> Register([FromForm] UserRegisterQuery userRegisterQuery)
        {
            _logger.LogInformation("User creation process started");
            try
            {
                var user = new User(userRegisterQuery);
                _logger.LogInformation("User entity object created from query");
                var errorMessage = string.Empty;
                if (userRegisterQuery.Validate(out errorMessage))
                {
                    var newUserGuid = Guid.NewGuid();
                    var userCreationResult = await _userRepository.CreateUser(user, newUserGuid);
                    _logger.LogInformation("User successully saved to db");

                    return new UserRegisterResponse(newUserGuid);
                }
                else return BadRequest(errorMessage);
            }
            catch (Npgsql.PostgresException dbException)
            {
                if (dbException.SqlState == "23505")
                {
                    _logger.LogWarning("Email dublicate found. Saving cancelled!");
                    return BadRequest("Выберите другой email адрес");
                }
                _logger.LogError(dbException.Message + dbException.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message + ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpDelete]
        [Route("delete_all")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [EndpointSummary("Delete ALL Users in the app. ONLY IN DEVELOPMENT MODE")]
        [Consumes("application/json")]
        public async Task<ActionResult<int>> DeleteAllUsers()
        {
            if (hostingEnvironment.IsDevelopment()) { return NotFound(); };

            return await _userRepository.DeleteAllUsers();

        }

        private bool _checkUserRegisterQuery(UserRegisterQuery userRegisterQuery)
        {
            throw new NotImplementedException();
        }
    }
}
