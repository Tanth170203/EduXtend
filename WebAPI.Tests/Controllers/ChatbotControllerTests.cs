using System.Security.Claims;
using BusinessObject.DTOs.Chatbot;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Chatbot;
using WebAPI.Constants;
using WebAPI.Controllers;
using Xunit;

namespace WebAPI.Tests.Controllers
{
    public class ChatbotControllerTests
    {
        private readonly Mock<IChatbotService> _chatbotServiceMock;
        private readonly Mock<ILogger<ChatbotController>> _loggerMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly ChatbotController _controller;

        public ChatbotControllerTests()
        {
            _chatbotServiceMock = new Mock<IChatbotService>();
            _loggerMock = new Mock<ILogger<ChatbotController>>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            _controller = new ChatbotController(
                _chatbotServiceMock.Object,
                _loggerMock.Object,
                _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task SendMessage_WithAuthenticatedUser_Returns200OkWithAIResponse()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Hello, what clubs are available?",
                ConversationHistory = null
            };

            var expectedResponse = "Here are some clubs for you";

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ChatMessageResponseDto>(okResult.Value);
            Assert.True(response.Success);
            Assert.Equal(expectedResponse, response.Message);
            Assert.Null(response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithUnauthenticatedUser_Returns401Unauthorized()
        {
            // Arrange
            var request = new ChatMessageRequestDto
            {
                Message = "Hello",
                ConversationHistory = null
            };

            SetupUnauthenticatedUser();

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result.Result);
            var response = Assert.IsType<ChatMessageResponseDto>(unauthorizedResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.Unauthorized, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithInvalidRequest_Returns400BadRequest()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "", // Empty message
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);
            _controller.ModelState.AddModelError("Message", "The Message field is required.");

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<ChatMessageResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.InvalidMessage, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithServiceError_Returns500InternalServerError()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Test message",
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatMessageResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.GenericError, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithGeminiApiError_Returns502BadGateway()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Test message",
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ThrowsAsync(new HttpRequestException("Network error"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(502, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatMessageResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.NetworkError, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithTimeout_Returns503ServiceUnavailable()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Test message",
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ThrowsAsync(new TimeoutException("Request timeout"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatMessageResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.Timeout, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithApiKeyError_Returns503ServiceUnavailable()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Test message",
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ThrowsAsync(new InvalidOperationException("Invalid API key configuration"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(503, statusCodeResult.StatusCode);
            var response = Assert.IsType<ChatMessageResponseDto>(statusCodeResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.InvalidApiKey, response.ErrorMessage);
        }

        [Fact]
        public async Task SendMessage_WithStudentNotFound_Returns404NotFound()
        {
            // Arrange
            var studentId = 1;
            var request = new ChatMessageRequestDto
            {
                Message = "Test message",
                ConversationHistory = null
            };

            SetupAuthenticatedUser(studentId);

            _chatbotServiceMock
                .Setup(s => s.ProcessChatMessageAsync(studentId, request.Message, request.ConversationHistory))
                .ThrowsAsync(new KeyNotFoundException("Student not found"));

            // Act
            var result = await _controller.SendMessage(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
            var response = Assert.IsType<ChatMessageResponseDto>(notFoundResult.Value);
            Assert.False(response.Success);
            Assert.Equal(ChatbotErrorMessages.StudentNotFound, response.ErrorMessage);
        }

        private void SetupAuthenticatedUser(int studentId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, studentId.ToString()),
                new Claim(ClaimTypes.Name, "Test Student")
            };

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }

        private void SetupUnauthenticatedUser()
        {
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = claimsPrincipal
                }
            };
        }
    }
}
