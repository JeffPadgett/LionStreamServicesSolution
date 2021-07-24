using LionStreamServices;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;


[assembly: FunctionsStartup(typeof(Startup))]
namespace LionStreamServices
{
    public sealed class Startup : FunctionsStartup
    {

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddHttpClient("SubClient", client =>
            {
                client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("TwitchSubUri"));
            });

        }

    }

}