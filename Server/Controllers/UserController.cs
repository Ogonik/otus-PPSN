using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Server.Models.User;
using Server.Services;
using System.Reflection.Metadata;
using System.Security.Claims;

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
        public async Task<ActionResult<UserGetResponse>> Get([SwaggerTryItOutDefaulValue("36f01d62-e8cb-4d98-9a2c-65f12d5c61fc")] Guid id)
        {
            _logger.LogInformation("Getting user by id: {0}", id);

            var userId = (User.Identity as ClaimsIdentity)?.FindFirst("Id")?.Value;
            _logger.LogInformation("Context is for user with id: {user_id}", userId);
            var foundUser = await _userRepository.GetUserById(id);

            if (foundUser != null)
                return Ok((UserGetResponse)foundUser);
            else
            {
                _logger.LogInformation("User not found by id: {user_id}", userId);
                return NotFound();
            }
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
            _logger.LogInformation("User registration process started");
            try
            {
                var user = new User(userRegisterQuery);
                _logger.LogInformation("User entity object created from form-data");
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


        [HttpGet]
        [Route("search")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [EndpointSummary("Поиск анкет")]
        public async Task<ActionResult<List<UserGetResponse>>> Search([FromQuery] UserSearchQuery userSearchQuery)
        {
            _logger.LogInformation("Searching user by: first_name '{firstName}' and last_name '{lastName}'", userSearchQuery.FirstName, userSearchQuery.LastName);

            if (_validateUserSearchQuery(userSearchQuery, out var failMessage))
            {
                _logger.LogInformation("Start searching users...");

                var result = await _userRepository.SearchUser(userSearchQuery.FirstName, userSearchQuery.LastName);
                _logger.LogInformation("Found {count} users by search query", result.ToList().Count);

                if (result.Any())
                    return Ok(result.Select(x => (UserGetResponse)x));
                else
                    return NotFound();
            }
            else
            {
                _logger.LogInformation("Search query validation failed: {failMessage}", failMessage);
                return BadRequest(failMessage);
            }
        }

        private static bool _validateUserSearchQuery(UserSearchQuery userSearchQuery, out string failMessage)
        {
            const string AllClauseSign = "*";
            failMessage = string.Empty;

            if (userSearchQuery.FirstName == string.Empty && userSearchQuery.LastName == string.Empty)
            {
                failMessage = "Both First Name and Last Name search params can not be empty";
                return false;
            }

            if (userSearchQuery.FirstName == AllClauseSign && userSearchQuery.LastName == AllClauseSign)
            {
                failMessage = "Both First Name and Last Name search params can not be set to *";
                return false;
            }

            return true;
        }
    }
}
