using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Server;

namespace ServerTests.Utils
{
    public static class TestExtensions
    {
        public static void AreEqualByJson(object expected, object actual)
        {
            var expectedJson = JsonSerializer.Serialize(expected);
            var actualJson = JsonSerializer.Serialize(actual);
            
            Assert.That(actualJson, Is.EqualTo(expectedJson));
        }
    }
}
