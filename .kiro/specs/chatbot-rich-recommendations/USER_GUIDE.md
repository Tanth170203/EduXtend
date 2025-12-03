# User Guide - Chatbot Rich Recommendations

## Overview

The AI Chatbot now provides beautiful, interactive recommendation cards when you ask for club or activity suggestions. Instead of plain text, you'll see visually rich cards with icons, descriptions, and relevance scores to help you find the perfect match.

## Getting Started

### Accessing the Chatbot

1. Log in to your student account
2. Look for the chatbot icon (💬) in the bottom-right corner of any page
3. Click the icon to open the chat window

### Asking for Recommendations

To get recommendation cards, ask questions like:

**For Clubs:**
- "Tìm câu lạc bộ về công nghệ"
- "Gợi ý câu lạc bộ phù hợp với tôi"
- "Câu lạc bộ nào phù hợp với chuyên ngành IT?"
- "Đề xuất câu lạc bộ cho sinh viên kinh doanh"

**For Activities:**
- "Tìm hoạt động sắp diễn ra"
- "Gợi ý hoạt động cho sinh viên IT"
- "Hoạt động nào phù hợp với tôi?"

**General Questions:**
For general questions (not requesting recommendations), you'll still receive helpful plain text responses:
- "Câu lạc bộ là gì?"
- "Làm sao để tham gia câu lạc bộ?"
- "Xin chào"

## Understanding Recommendation Cards

### Card Layout

Each recommendation card displays:

```
┌─────────────────────────────────────────────┐
│ 👥 CÂU LẠC BỘ                               │ ← Type icon and label
│                                             │
│ Câu lạc bộ Lập trình                        │ ← Club/Activity name
│                                             │
│ Câu lạc bộ dành cho sinh viên yêu thích    │ ← Brief description
│ lập trình và phát triển phần mềm            │
│                                             │
│ ┌─────────────────────────────────────────┐ │
│ │ 💡 Phù hợp với chuyên ngành Công nghệ   │ │ ← Why it's recommended
│ │    thông tin của bạn và giúp phát triển │ │
│ │    kỹ năng lập trình thực tế            │ │
│ └─────────────────────────────────────────┘ │
│                                             │
│ ✨ Độ phù hợp: 95%                          │ ← Relevance score
└─────────────────────────────────────────────┘
```

### Card Elements

#### 1. Type Icon and Label
- **👥 CÂU LẠC BỘ** - For student clubs
- **🎯 HOẠT ĐỘNG** - For activities and events

#### 2. Name
The official name of the club or activity in large, bold blue text.

#### 3. Description
A brief 1-2 sentence description of what the club/activity is about.

#### 4. Reason (💡)
A personalized explanation of why this recommendation matches your profile, interests, or major.

#### 5. Relevance Score (✨)
A percentage (0-100%) showing how well this recommendation fits you:

- **90-100%** (Dark Green) - Excellent match! Highly recommended for you
- **70-89%** (Green) - Good match, worth exploring
- **50-69%** (Yellow) - Fair match, could be interesting
- **0-49%** (Orange) - Lower match, but might broaden your horizons

## Using Recommendation Cards

### Viewing Details

**Desktop:**
1. Hover over a card to see the lift animation
2. Click anywhere on the card to view full details
3. You'll be taken to the club or activity detail page

**Mobile:**
1. Tap on any card to view full details
2. The card will navigate to the detail page

**Keyboard Navigation:**
1. Press `Tab` to move between cards
2. Press `Enter` or `Space` to open the selected card
3. A blue outline shows which card is focused

### Comparing Recommendations

The AI typically shows 3-5 recommendations sorted by relevance score (highest first). Compare:

- **Relevance scores** - Higher scores mean better matches
- **Reasons** - See why each is recommended for you
- **Descriptions** - Understand what each club/activity offers

### Example Interaction

**You ask:**
> "Tìm câu lạc bộ về công nghệ cho sinh viên IT"

**AI responds with:**
> "Dựa trên chuyên ngành Công nghệ thông tin của bạn, tôi tìm thấy các câu lạc bộ phù hợp sau:"

**Then displays 3 cards:**

1. **Câu lạc bộ Lập trình** - 95% match
   - Reason: "Hoàn toàn phù hợp với chuyên ngành IT, giúp bạn nâng cao kỹ năng coding"

2. **Câu lạc bộ AI & Machine Learning** - 88% match
   - Reason: "Xu hướng công nghệ mới, phù hợp với sinh viên IT muốn học về AI"

3. **Câu lạc bộ Cyber Security** - 82% match
   - Reason: "Kỹ năng quan trọng cho sinh viên IT, nhiều cơ hội việc làm"

## Screenshots

### Desktop View

**Screenshot 1: Chatbot with Recommendation Cards**
```
[Screenshot placeholder: Desktop view showing chatbot window with 3 recommendation cards displayed vertically. Each card shows the full layout with icon, name, description, reason, and score.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/desktop-recommendations.png
```

**Screenshot 2: Card Hover Effect**
```
[Screenshot placeholder: Desktop view showing a recommendation card with hover effect - card lifted up with enhanced shadow.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/desktop-hover.png
```

**Screenshot 3: Keyboard Focus**
```
[Screenshot placeholder: Desktop view showing a recommendation card with blue focus outline, demonstrating keyboard navigation.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/desktop-keyboard-focus.png
```

### Mobile View

**Screenshot 4: Mobile Recommendations**
```
[Screenshot placeholder: Mobile view (iPhone/Android) showing recommendation cards stacked vertically, fitting screen width.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/mobile-recommendations.png
```

**Screenshot 5: Mobile Card Detail**
```
[Screenshot placeholder: Mobile view showing a single recommendation card with all elements clearly visible and touch-friendly spacing.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/mobile-card-detail.png
```

### Different Recommendation Types

**Screenshot 6: Club Recommendations**
```
[Screenshot placeholder: Cards showing club recommendations with 👥 icon and "CÂU LẠC BỘ" label.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/club-recommendations.png
```

**Screenshot 7: Activity Recommendations**
```
[Screenshot placeholder: Cards showing activity recommendations with 🎯 icon and "HOẠT ĐỘNG" label.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/activity-recommendations.png
```

**Screenshot 8: Mixed Recommendations**
```
[Screenshot placeholder: Cards showing both clubs and activities in the same response.]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/mixed-recommendations.png
```

### Score Color Variations

**Screenshot 9: Different Relevance Scores**
```
[Screenshot placeholder: Multiple cards showing different relevance scores with their corresponding colors:
- 95% in dark green
- 78% in medium green
- 62% in yellow
- 45% in orange]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/score-colors.png
```

### Plain Text Responses

**Screenshot 10: Plain Text Response**
```
[Screenshot placeholder: Chatbot showing a plain text response for a general question like "Câu lạc bộ là gì?"]

Location: .kiro/specs/chatbot-rich-recommendations/screenshots/plain-text-response.png
```

## Tips for Best Results

### 1. Be Specific
Instead of: "Tìm câu lạc bộ"
Try: "Tìm câu lạc bộ về công nghệ cho sinh viên IT"

### 2. Mention Your Interests
Include what you're interested in:
- "Tìm câu lạc bộ về nhiếp ảnh và nghệ thuật"
- "Gợi ý hoạt động về khởi nghiệp"

### 3. Ask Follow-up Questions
After seeing recommendations, you can ask:
- "Cho tôi biết thêm về Câu lạc bộ Lập trình"
- "Có câu lạc bộ nào khác về công nghệ không?"

### 4. Explore Different Categories
Try asking about different areas:
- Technology clubs
- Business clubs
- Arts and culture clubs
- Sports and fitness clubs
- Social and volunteer clubs

## Accessibility Features

### For Screen Reader Users

The chatbot is fully accessible with screen readers:

1. **Card Announcements**: Each card is announced with full context:
   - "Câu lạc bộ Lập trình. Câu lạc bộ dành cho sinh viên yêu thích lập trình. Phù hợp với chuyên ngành Công nghệ thông tin của bạn. Độ phù hợp 95 phần trăm."

2. **Navigation**: Use standard screen reader commands:
   - Navigate by buttons (cards are marked as buttons)
   - Tab through interactive elements
   - Activate with Enter or Space

3. **Context**: All visual information is provided in text:
   - Type (club or activity) is announced
   - Relevance score includes "phần trăm" for clarity
   - Reasons are fully read out

### For Keyboard Users

1. **Tab Navigation**: Press Tab to move between cards
2. **Activation**: Press Enter or Space to open a card
3. **Focus Indicators**: Blue outline shows which card is focused
4. **Skip Links**: Use standard skip navigation if available

### For Users with Visual Impairments

1. **High Contrast**: Cards use sufficient color contrast
2. **Text Labels**: All information is text-based, not just color
3. **Scalable Text**: Text can be zoomed without breaking layout
4. **Clear Hierarchy**: Logical heading structure

## Troubleshooting

### Cards Not Showing

**Problem**: You asked for recommendations but see plain text instead.

**Solutions:**
1. Try rephrasing your question with keywords like "tìm câu lạc bộ" or "gợi ý hoạt động"
2. Refresh the page and try again
3. Clear your browser cache (Ctrl+Shift+Delete)
4. Try a different browser

### Cards Look Broken

**Problem**: Cards display but styling is incorrect.

**Solutions:**
1. Hard refresh the page (Ctrl+F5)
2. Clear browser cache
3. Check if you're using a supported browser (Chrome, Firefox, Safari, Edge)

### Can't Click Cards

**Problem**: Clicking cards doesn't navigate to detail pages.

**Solutions:**
1. Check if JavaScript is enabled in your browser
2. Try using keyboard navigation (Tab + Enter)
3. Refresh the page and try again

### Slow Response

**Problem**: AI takes a long time to respond.

**Solutions:**
1. Wait up to 30 seconds for response
2. Check your internet connection
3. Try asking a simpler question
4. If timeout occurs, try again

### Mobile Display Issues

**Problem**: Cards don't display properly on mobile.

**Solutions:**
1. Rotate device to portrait orientation
2. Zoom out if cards are too large
3. Try refreshing the page
4. Update your mobile browser

## Frequently Asked Questions

### Q: How does the AI calculate relevance scores?

A: The AI analyzes your profile (major, cohort, interests) and matches it against club/activity characteristics. Higher scores mean better alignment with your profile.

### Q: Can I save recommendations for later?

A: Currently, recommendations are shown in the chat. You can click on cards to view details and bookmark those pages. A save feature may be added in the future.

### Q: Why do I sometimes get plain text instead of cards?

A: The AI uses cards specifically for club/activity recommendations. General questions receive plain text responses. Try using keywords like "tìm câu lạc bộ" to get cards.

### Q: How many recommendations will I get?

A: Typically 3-5 recommendations per request, sorted by relevance. This provides a good balance between choice and decision-making.

### Q: Can I request more recommendations?

A: Yes! Ask follow-up questions like "Có câu lạc bộ nào khác không?" or "Gợi ý thêm hoạt động khác"

### Q: Are recommendations personalized?

A: Yes! The AI considers your major, cohort, and the context of your question to provide personalized recommendations with explanations.

### Q: What if I don't like any recommendations?

A: Try:
1. Asking with different keywords or interests
2. Being more specific about what you're looking for
3. Asking for a different category of clubs/activities

### Q: Can I use the chatbot on mobile?

A: Yes! The recommendation cards are fully responsive and work great on mobile devices.

### Q: Is the chatbot accessible?

A: Yes! The chatbot supports screen readers, keyboard navigation, and follows accessibility best practices.

### Q: How often is club/activity data updated?

A: The AI uses current data from the system. New clubs and activities appear in recommendations as soon as they're added to the system.

## Getting Help

If you encounter issues or have questions:

1. **Check this guide** for common solutions
2. **Contact support** through the help desk
3. **Report bugs** to the IT department
4. **Provide feedback** to help us improve

## Privacy and Data

### What Information Does the AI Use?

The AI uses:
- Your name and major (from your profile)
- Your cohort/year
- Available clubs and activities
- Your chat messages

### What Information is Stored?

- Chat messages are stored for improving the service
- No sensitive personal information is shared with external services
- Your data is protected according to university privacy policies

### Can I Delete My Chat History?

Contact the IT department to request chat history deletion.

## Updates and New Features

This feature is continuously being improved. Future updates may include:

- Save recommendations for later
- Share recommendations with friends
- More detailed filtering options
- Integration with club registration
- Personalized notifications

Check back regularly for new features!

## Additional Resources

- **Technical Documentation**: For developers and administrators
  - [API Documentation](./API_DOCUMENTATION.md)
  - [Developer Guide](./DEVELOPER_GUIDE.md)
  - [CSS Reference](./CSS_REFERENCE.md)
  - [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md)

- **System Documentation**:
  - [Requirements Document](./requirements.md)
  - [Design Document](./design.md)
  - [Implementation Tasks](./tasks.md)

---

**Version**: 1.0  
**Last Updated**: December 2, 2024  
**Feedback**: Please send feedback to support@eduxted.edu.vn
