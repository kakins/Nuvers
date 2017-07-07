using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Nuvers.Test
{
    public class SomeCommandTests
    {
        [Theory]
        [InlineData("")]
        [InlineData("-myoption hi")]
        [InlineData("-otheroption b")]
        public void SomeCommand_OptionMessage(string command)
        {
            var nuversexe = Util.GetNuversExePath();

            // Act
            CommandRunnerResult result = CommandRunner.Run(
                nuversexe,
                Directory.GetCurrentDirectory(),
                "somecmd " + command,
                waitForExit: true);

            // Assert
            Assert.True(0 == result.Item1, result.Item2 + Environment.NewLine + result.Item3);
        }
    }
}
