using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NickBuhro.Translit;
using Server.Models.User;
using Server.Services;
using System;
using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using BC = BCrypt.Net.BCrypt;

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
        public async Task<ActionResult<int>> DeleteAllUsers([FromBody]Guid preserveUserGuid)
        {
            _logger.LogInformation("Deleting all users but the first one");

            if (!hostingEnvironment.IsDevelopment()) { return NotFound(); };
            if (_userRepository.GetUserById(preserveUserGuid) == null) { return NotFound(); };

            return await _userRepository.DeleteAllUsers(preserveUserGuid);

        }

        [HttpPost]
        [Route("batch_insert")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        [EndpointSummary("Загрузка пользователей из файла")]
        [Consumes("multipart/form-data")]
        public async Task<int> BatchInsert([FromForm] int seed, IFormFile file)
        {
            _logger.LogInformation("Batch inserting users");
            var usersCreated = 0;

            if (_checkInputUsersCSVFile(file))
            {
                var usersToInsert = _getUsersFromFile(file);
                _logger.LogInformation("{userCount} users read from file. Inserting as binary from stdin...", usersToInsert.Count);
                usersCreated = await _userRepository.CreateUserBatch(usersToInsert, seed);
            }

            return usersCreated;
        }

        private List<User> _getUsersFromFile(IFormFile usersFile)
        {
            var result = new List<User>();

            if (usersFile != null)
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = false,
                    
                };

                try
                {
                    using var reader = new StreamReader(usersFile.OpenReadStream());
                    using var csv = new CsvReader(reader, config);

                    var anonymousTypeDefinition = new
                    {
                        LastAndFirstName = string.Empty,
                        BirthDate = string.Empty,
                        City = string.Empty
                    };

                    var records = csv.GetRecords(anonymousTypeDefinition).ToList();
                    
                    foreach (var userRecord in records)
                    {
                        var name = userRecord.LastAndFirstName.Split(' ');
                        var birthDate = DateOnly.ParseExact(userRecord.BirthDate, "yyyy-MM-dd");
                        var user = new User()
                        {
                            Id = Guid.NewGuid(),
                            FirstName = name[1],
                            LastName = name[0],
                            BirthDate = birthDate,
                            City = userRecord.City,
                            Email = Transliteration.CyrillicToLatin(userRecord.LastAndFirstName.Replace(' ', '_') + birthDate.Year.ToString()).Replace("'", string.Empty) + RandomString(6) + "@ppsn.ru",
                            Password = userRecord.City,
                        };
                        result.Add(user);
                    }
                }
                catch (Exception ex)
                {
                    // handle exception
                }
            }

            return result;
        }

        private static Random random = new Random();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private bool _checkInputUsersCSVFile(IFormFile usersFile)
        {
            return true;
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
