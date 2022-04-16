using IdentityModel.Client;
using Majority.RemittanceProvider.IntegrationTest.Entities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Majority.RemittanceProvider.IntegrationTest
{
    public class BaseIntegrationTest
    {
        private readonly HttpClient _client;
        private readonly ApiTokenInMemoryClient _tokenService;
        private readonly IConfigurationRoot _configurationRoot;


        public BaseIntegrationTest()
        {
            _configurationRoot = GetIConfigurationRoot();
            _client = new HttpClient
            {
                BaseAddress = new System.Uri(_configurationRoot["ApiUrl"])
            };

            _tokenService = new ApiTokenInMemoryClient(_configurationRoot["identityServerUrl"], new HttpClient());
        }

        [Fact]
        public async Task Get401ForIncorrectToken()
        {
            var access_token = await _tokenService.GetApiToken(
                    "438872944666-eemptnnlep2k3e2slicf71dh3fk80q3h",
                    "422A3574-5272-4345-9672-211F2C940",
                    "TestAPI.write",
                    "client_credentials"
                );

            _client.SetBearerToken(access_token);

            // Act
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            ExchangeRateDetailsRequest request = new ExchangeRateDetailsRequest();
            request.From = "US";
            request.To = "SE";
            string json = JsonConvert.SerializeObject(request);
            StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("transaction/get-exchange-rate", httpContent);

            // Assert
            Assert.Equal("Unauthorized", response.StatusCode.ToString());
        }

        [Fact]
        public async Task Get401ForNoToken()
        {
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/x-protobuf"));
            ExchangeRateDetailsRequest request = new ExchangeRateDetailsRequest();
            request.From = "US";
            request.To = "SE";
            string json = JsonConvert.SerializeObject(request);
            StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("transaction/get-exchange-rate", httpContent);

            // Assert
            Assert.Equal("Unauthorized", response.StatusCode.ToString());
        }

        public static IConfigurationRoot GetIConfigurationRoot()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
