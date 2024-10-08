
using Microsoft.Extensions.Logging;
using Npgsql;
using NUnit.Framework.Internal;
using Server.Models.User;
using Server.Services;
using ServerTests.Utils;

namespace ServerTests
{
    public class UserRepositoryTests
    {
        public NpgsqlConnection connection { get; set; }
        public List<User> TestUsers { get; set; }

        public Microsoft.Extensions.Logging.ILogger logger { get; set; } = LoggerFactory.Create(options => options.AddConsole()).CreateLogger("UserRepositoryTest");

        [SetUp]
        public void Setup()
        {
            _fillInUsers();
            
        }

        private void _fillInUsers()
        {
            TestUsers = new List<User>()
            {
                new User
                {
                    FirstName = string.Empty
                    ,

                }
            };
        }

        [Test]
        public void CreateUserTest()
        {
            foreach (var testUser in TestUsers)
            {
                _˝reateUserInternalTest(testUser, logger);
            }
        }


        private void _˝reateUserInternalTest(User incomingUser, Microsoft.Extensions.Logging.ILogger logger)
        {
            IUserRepository userRepository = new UserRepository(connection, logger);
            
            var userGuid = Guid.NewGuid();
            userRepository.CreateUser(incomingUser, userGuid);
            incomingUser.Id = userGuid;

            var createdUser = userRepository.GetUserById(userGuid);

            TestExtensions.AreEqualByJson(incomingUser, createdUser);
        }
        
        [TearDown]
        public void TearDown()
        {
        }

    }
}