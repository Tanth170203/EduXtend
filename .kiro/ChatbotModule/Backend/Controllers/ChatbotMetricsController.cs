using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Chatbot;

namespace WebAPI.Controllers
{
    /// <summary>
    /// Controller for monitoring chatbot metrics and health
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotMetricsController : ControllerBase
    {
        private readonly IChatbotMetricsService _metricsService;
        private readonly ILogger<ChatbotMetricsController> _logger;

        public ChatbotMetricsController(
            IChatbotMetricsService metricsService,
            ILogger<ChatbotMetricsController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Get current chatbot metrics
        /// </summary>
        /// <remarks>
        /// Returns operational metrics for the chatbot including:
        /// - Total requests and success/failure rates
        /// - Rate limit hits
        /// - Intent distribution (club, activity, general)
        /// - Average response time
        /// - Error counts
        /// 
        /// **Note:** Requires Admin role for access
        /// </remarks>
        /// <returns>Current chatbot metrics</returns>
        /// <response code="200">Successfully retrieved metrics</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ChatbotMetrics), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        public IActionResult GetMetrics()
        {
            try
            {
                var metrics = _metricsService.GetMetrics();
                _logger.LogInformation("Chatbot metrics retrieved");
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chatbot metrics");
                return StatusCode(500, new { message = "Unable to retrieve metrics" });
            }
        }

        /// <summary>
        /// Reset chatbot metrics
        /// </summary>
        /// <remarks>
        /// Resets all chatbot metrics to zero. Use this for testing or after reviewing metrics.
        /// 
        /// **Note:** Requires Admin role for access
        /// </remarks>
        /// <returns>Success message</returns>
        /// <response code="200">Successfully reset metrics</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="403">User does not have Admin role</response>
        [HttpPost("reset")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status403Forbidden)]
        public IActionResult ResetMetrics()
        {
            try
            {
                _metricsService.ResetMetrics();
                _logger.LogWarning("Chatbot metrics reset by admin");
                return Ok(new { message = "Metrics reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting chatbot metrics");
                return StatusCode(500, new { message = "Unable to reset metrics" });
            }
        }

        /// <summary>
        /// Get chatbot health status
        /// </summary>
        /// <remarks>
        /// Returns a simple health check for the chatbot system.
        /// Useful for monitoring and alerting systems.
        /// </remarks>
        /// <returns>Health status</returns>
        /// <response code="200">Chatbot is healthy</response>
        /// <response code="503">Chatbot is unhealthy</response>
        [HttpGet("health")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
        public IActionResult GetHealth()
        {
            try
            {
                var metrics = _metricsService.GetMetrics();
                
                // Calculate error rate
                var errorRate = metrics.TotalRequests > 0 
                    ? (double)metrics.FailedRequests / metrics.TotalRequests 
                    : 0;

                // Consider unhealthy if error rate > 50% and we have at least 10 requests
                var isHealthy = metrics.TotalRequests < 10 || errorRate < 0.5;

                if (isHealthy)
                {
                    return Ok(new
                    {
                        status = "healthy",
                        totalRequests = metrics.TotalRequests,
                        errorRate = Math.Round(errorRate * 100, 2),
                        averageResponseTimeMs = Math.Round(metrics.AverageResponseTimeMs, 2),
                        lastUpdated = metrics.LastUpdated
                    });
                }
                else
                {
                    _logger.LogWarning("Chatbot health check failed: high error rate {ErrorRate}%", errorRate * 100);
                    return StatusCode(503, new
                    {
                        status = "unhealthy",
                        reason = "High error rate",
                        totalRequests = metrics.TotalRequests,
                        errorRate = Math.Round(errorRate * 100, 2),
                        lastUpdated = metrics.LastUpdated
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking chatbot health");
                return StatusCode(503, new
                {
                    status = "unhealthy",
                    reason = "Unable to retrieve metrics"
                });
            }
        }
    }
}
