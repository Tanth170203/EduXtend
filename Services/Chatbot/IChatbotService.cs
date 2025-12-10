using BusinessObject.DTOs.Chatbot;

namespace Services.Chatbot
{
    public interface IChatbotService
    {
        Task<string> ProcessChatMessageAsync(
            int userId,
            string userMessage,
            List<ChatMessageDto>? conversationHistory);

        void InvalidateStudentContext(int userId);
        void InvalidateActiveClubs();
        void InvalidateUpcomingActivities();
    }
}
