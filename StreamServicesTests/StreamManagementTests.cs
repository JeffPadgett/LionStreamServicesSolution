using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using StreamServices;
using StreamServices.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StreamServices.Tests
{
    [TestClass()]
    public class StreamManagementTests
    {
        public StreamManagementTests()
        {
            // Setup for all unit tests
            /*Environment.SetEnvironmentVariable("OAuthToken", "foo:bar:baz");
            Environment.SetEnvironmentVariable("ClientSecret", "foo:bar:baz");*/
        }

        [TestMethod()]
        public async void MakeSureWeObtainAccessTokenTest()
        {
            // arrange
            var clientId = Environment.GetEnvironmentVariable("OAuthToken");
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret");

            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    if (request.RequestUri.ToString().StartsWith("https://id.twitch.tv/oauth2/token"))
                        return new HttpResponseMessage
                        {
                            StatusCode = System.Net.HttpStatusCode.OK,
                            Content = new StringContent(JsonConvert.SerializeObject(new AppAccessToken
                            {
                                AccessToken = "Passed The Test!"
                            }))
                        };

                    throw new Exception("Derp");
                });

            // The factory has to return a "real" HttpClient, but using our mocked message handler
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup<HttpClient>(x=>x.CreateClient(It.IsAny<string>()))
                       .Returns((string clientName) => new HttpClient(mockHttpMessageHandler.Object));

            // v-- this. Shorthand for IDisposable using scopes. Since StreamManagement isn't IDisposable, you can't do a "using" statement
            /*using*/ var client = new StreamManagement(mockFactory.Object, null); // config is null for now - Last one... THis is complaining Same issue that was below


            //act
            // using (var client = BaseFunction.GetHttpClient("https://id.twitch.tv"))
            //{
            var token = await client.GetAccessToken();


            //assert
            Assert.IsNotNull(token);

            Assert.IsNotNull(token.AccessToken);
            // Assert.Fail();
        }

        [TestMethod]
        public async Task EnsureTwitchOAuthTokenSuccessful()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            mockFactory.Setup<HttpClient>(x=>x.CreateClient(It.IsAny<string>()))
                       .Returns((string clientName) => new HttpClient());

            // StreamManagement and BaseFunction are not disposable (pulled it from up there ^^^)
            var client = new StreamManagement(mockFactory.Object, null); // DefaultFactory might be in a separate lib, I forget off the top of my head...
            
            var token = await client.GetAccessToken();
            
            //assert
            Assert.IsNotNull(token);

            Assert.IsNotNull(token.AccessToken);
        }
    }

}