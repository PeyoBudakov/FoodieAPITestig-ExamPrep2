using FoodieExamPrep2.Models;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;

namespace FoodieExamPrep2
{
    public class Tests
    {
        private RestClient client;
        private const string BASEURL = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";
        private const string USERNAME = "Budakov";
        private const string PASSWORD = "123456";


        private static string lastFoodId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(USERNAME, PASSWORD);

            var options = new RestClientOptions(BASEURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            RestClient authClient = new RestClient(BASEURL);
            var request = new RestRequest("/api/User/Authentication");
            request.AddJsonBody(new
            {
                username,
                password
            });

            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Access Token is null or empty");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response type {response.StatusCode} with data {response.Content}");
            }
        }



        [Test, Order(1)]
        public void CreateNewIdea_WithCorrectData_ShouldSucceed()
        {

            var newFood = new FoodDTO
            {
                Name = "New Test Food",
                Description = "Description",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(newFood);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            var data = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            lastFoodId = data.foodId;

            Console.WriteLine(lastFoodId);
        }

        [Test, Order(2)]
        public void EditFoodTitle_WithCorrectData_ShouldSucceed()
        {
            
            var request = new RestRequest($"/api/Food/Edit/{lastFoodId}", Method.Patch);
            request.AddJsonBody(new[]
            {
                new {
                    path = "/name",
                    op = "replace",
                    value = "Edited Food Title"

                },
            });

            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            
            Assert.That(responseData.Message, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllFoods_ShouldSucceed()
        {
            var request = new RestRequest("/api/Food/All");

            var response = client.Execute(request, Method.Get);
            var responseDataArray = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(responseDataArray.Length, Is.GreaterThan(0));
        }

        [Test, Order(4)]
        public void DeleteFoodThatYouCreated_ShouldSucceed()
        {
            var request = new RestRequest($"/api/Food/Delete/{lastFoodId}");
            

            var response = client.Execute(request, Method.Delete);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("Deleted successfully!"));
        }
        [Test, Order(5)]
        public void CreateFoodWithoutRequoredFields_ShouldFail()
        {
            var newFood = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(newFood);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingFood_ShouldFail()
        {
            var request = new RestRequest($"/api/Food/Edit/666", Method.Patch);
            request.AddJsonBody(new[]
            {
                new {
                    path = "/name",
                    op = "replace",
                    value = "Edited Food Title Test 6"

                },
            });

            var response = this.client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(responseData.Message, Is.EqualTo("No food revues..."));
        }

        [Test, Order(7)]
        public void DeleteNonExistentFood_ShouldFail()
        {
            var request = new RestRequest($"/api/Food/Delete/888");

            var response = client.Execute(request, Method.Delete);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));

            Assert.That(response.Content, Does.Contain("Unable to delete this food revue!"));
        }
    }
}