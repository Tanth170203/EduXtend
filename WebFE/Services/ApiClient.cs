using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace WebFE.Services
{
    public interface IApiClient
    {
        Task<T?> GetAsync<T>(string endpoint);
        Task<T?> PostAsync<T>(string endpoint, object data);
        Task<T?> PutAsync<T>(string endpoint, object data);
        Task<bool> DeleteAsync(string endpoint);
    }

    public class ApiClient : IApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<T?> PostAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Nếu không success, check xem có phải warning hay error
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("POST {Endpoint} failed with status {StatusCode}: {Error}", 
                        endpoint, response.StatusCode, responseContent);
                    
                    // Try to extract error/warning message from response
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        try
                        {
                            var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
                            if (errorObj != null && errorObj.ContainsKey("message"))
                            {
                                throw new HttpRequestException(errorObj["message"].ToString());
                            }
                        }
                        catch (JsonException)
                        {
                            // If can't parse as JSON, throw the raw content
                            throw new HttpRequestException(responseContent);
                        }
                    }
                    
                    response.EnsureSuccessStatusCode(); // This will throw if still not handled
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw to preserve error message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<T?> PutAsync<T>(string endpoint, object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync(endpoint, content);
                
                var responseContent = await response.Content.ReadAsStringAsync();
                
                // Nếu không success, check xem có phải warning hay error
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("PUT {Endpoint} failed with status {StatusCode}: {Error}", 
                        endpoint, response.StatusCode, responseContent);
                    
                    // Try to extract error/warning message from response
                    if (!string.IsNullOrEmpty(responseContent))
                    {
                        try
                        {
                            var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, _jsonOptions);
                            if (errorObj != null && errorObj.ContainsKey("message"))
                            {
                                throw new HttpRequestException(errorObj["message"].ToString());
                            }
                        }
                        catch (JsonException)
                        {
                            // If can't parse as JSON, throw the raw content
                            throw new HttpRequestException(responseContent);
                        }
                    }
                    
                    response.EnsureSuccessStatusCode(); // This will throw if still not handled
                }

                return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw to preserve error message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling PUT {Endpoint}", endpoint);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.DeleteAsync(endpoint);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("DELETE {Endpoint} failed with status {StatusCode}: {Error}", 
                        endpoint, response.StatusCode, errorContent);
                    
                    // Try to extract error message from response
                    if (!string.IsNullOrEmpty(errorContent))
                    {
                        try
                        {
                            var errorObj = JsonSerializer.Deserialize<Dictionary<string, object>>(errorContent, _jsonOptions);
                            if (errorObj != null && errorObj.ContainsKey("message"))
                            {
                                throw new HttpRequestException(errorObj["message"].ToString());
                            }
                        }
                        catch (JsonException)
                        {
                            // If can't parse as JSON, throw the raw content
                            throw new HttpRequestException(errorContent);
                        }
                    }
                    
                    return false;
                }
                
                return true;
            }
            catch (HttpRequestException)
            {
                throw; // Re-throw to preserve error message
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling DELETE {Endpoint}", endpoint);
                throw;
            }
        }
    }
}

