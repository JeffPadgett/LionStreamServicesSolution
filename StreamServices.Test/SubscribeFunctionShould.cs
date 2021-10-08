using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StreamServices.Core;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;


namespace StreamServices.Test
{

    public class SubscribeFunctionShould
    {
        private readonly ITestOutputHelper _output;
        readonly StreamServices _sut;

        public SubscribeFunctionShould(ITestOutputHelper output)
        {
            var startup = new Startup();
            var host = new HostBuilder()
                .ConfigureWebJobs(startup.Configure)
                .Build();

            _sut = new StreamServices(host.Services.GetRequiredService<IHttpClientFactory>(), host.Services.GetRequiredService<IConfiguration>());
            _output = output;
        }

        [Fact]
        public async Task GetChannelIdForUserNameAsync()
        {
            var channelId = await _sut.GetChannelIdForUserName("one1lion");
            Assert.Equal("48646924", channelId);
        }
    }
}
