using NetArchTest.Rules;
using Xunit;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        [Fact]
        public void NetSdrClientApp_Should_Not_Depend_On_EchoTspServer()
        {
            var result = Types
                .InAssembly(typeof(NetSdrClientApp.Program).Assembly)
                .ShouldNot()
                .HaveDependencyOn("EchoTspServer")
                .GetResult();

            Assert.True(result.IsSuccessful, "NetSdrClientApp має залежність від EchoTspServer!");
        }

        [Fact]
        public void EchoTspServer_Should_Not_Depend_On_NetSdrClientApp()
        {
            var result = Types
                .InAssembly(typeof(EchoTspServer.Program).Assembly)
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp")
                .GetResult();

            Assert.True(result.IsSuccessful, "EchoTspServer не повинен залежати від NetSdrClientApp!");
        }
    }
}
