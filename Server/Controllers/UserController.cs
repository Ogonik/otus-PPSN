using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Server.Models.User;
using Server.Services;

namespace Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IUserRepository _userRepository;

        public UserController(ILogger<UserController> logger, UserRepository userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;               
        }

        [HttpGet(Name = "get")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<User>> Get(Guid id)
        {
            return await _userRepository.GetUserById(id);
        }

        [HttpGet(Name = "register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        
        public async Task<ActionResult<UserRegisterResponse>> Register(UserRegisterQuery userRegisterQuery)
        {
            _logger.LogInformation("UserController: Register started");
            try
            {
                var user = new User(userRegisterQuery);
                _logger.LogInformation("UserController: user created from query");
                var errorMessage = string.Empty;
                if (userRegisterQuery.Validate(out errorMessage))
                {
                    var newUserGuid = Guid.NewGuid();
                    var userId = await _userRepository.CreateUser(user, newUserGuid);
                    _logger.LogInformation("UserController: User saved");
                    return new UserRegisterResponse(newUserGuid);
                }
                else return BadRequest(errorMessage);
            }
            catch (Exception ex) {
                _logger.LogError(ex.Message + ex.StackTrace);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private bool _checkUserRegisterQuery(UserRegisterQuery userRegisterQuery)
        {
            throw new NotImplementedException();
        }
    }
}
