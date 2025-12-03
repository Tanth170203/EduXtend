# CSS Reference - Recommendation Cards

## Overview

This document provides a complete reference for all CSS classes and styles used in the recommendation cards feature. Use this guide to customize the appearance or troubleshoot styling issues.

## File Location

```
WebFE/wwwroot/css/recommendation-cards.css
```

## CSS Variables

The design system uses CSS custom properties for easy theming:

```css
:root {
    /* Card backgrounds */
    --card-bg-gradient: linear-gradient(135deg, #E8EAF6 0%, #C5CAE9 100%);
    --card-hover-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
    
    /* Text colors */
    --card-title-color: #1565C0;
    --card-text-color: #424242;
    --card-reason-color: #616161;
    
    /* Score colors */
    --score-excellent: #00A86B;  /* 90-100% */
    --score-good: #32CD32;       /* 70-89% */
    --score-fair: #FFD700;       /* 50-69% */
    --score-low: #FF8C00;        /* 0-49% */
    
    /* Spacing */
    --card-padding: 20px;
    --card-gap: 16px;
    --card-border-radius: 12px;
}
```

### Customizing Variables

To change the color scheme, override these variables in your custom CSS:

```css
:root {
    --card-bg-gradient: linear-gradient(135deg, #FFF3E0 0%, #FFE0B2 100%);
    --card-title-color: #E65100;
}
```

## Container Classes

### `.recommendations-container`

Container for all recommendation cards.

```css
.recommendations-container {
    display: flex;
    flex-direction: column;
    gap: var(--card-gap);
    margin-top: 12px;
    max-width: 100%;
}
```

**Usage:**
```html
<div class="recommendations-container">
    <!-- Cards go here -->
</div>
```

**Properties:**
- `display: flex` - Flexbox layout
- `flex-direction: column` - Stack cards vertically
- `gap: 16px` - Space between cards
- `margin-top: 12px` - Space above container
- `max-width: 100%` - Prevent overflow

## Card Classes

### `.recommendation-card`

Main card component.

```css
.recommendation-card {
    background: var(--card-bg-gradient);
    border-radius: var(--card-border-radius);
    padding: var(--card-padding);
    margin-bottom: var(--card-gap);
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    cursor: pointer;
    transition: all 0.3s ease;
    position: relative;
    overflow: hidden;
}
```

**Properties:**
- `background` - Gradient background (light blue to purple)
- `border-radius: 12px` - Rounded corners
- `padding: 20px` - Internal spacing
- `box-shadow` - Subtle shadow for depth
- `cursor: pointer` - Indicates clickability
- `transition` - Smooth animations
- `position: relative` - For pseudo-element positioning

**States:**

```css
.recommendation-card:hover {
    transform: translateY(-4px);
    box-shadow: var(--card-hover-shadow);
}

.recommendation-card:focus {
    outline: 2px solid #1565C0;
    outline-offset: 2px;
}
```

**Hover Effect:**
- Lifts card up by 4px
- Increases shadow for depth

**Focus Effect:**
- Blue outline for keyboard navigation
- 2px offset for visibility

### `.recommendation-card::before`

Left border accent.

```css
.recommendation-card::before {
    content: '';
    position: absolute;
    top: 0;
    left: 0;
    width: 4px;
    height: 100%;
    background: var(--card-title-color);
}
```

**Properties:**
- `width: 4px` - Thin vertical line
- `height: 100%` - Full card height
- `background` - Matches title color

## Header Classes

### `.card-header`

Container for type icon and label.

```css
.card-header {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 12px;
}
```

**Usage:**
```html
<div class="card-header">
    <span class="card-type-icon">👥</span>
    <span class="card-type-label">CÂU LẠC BỘ</span>
</div>
```

### `.card-type-icon`

Emoji icon for recommendation type.

```css
.card-type-icon {
    font-size: 20px;
}
```

**Icons:**
- 👥 for clubs
- 🎯 for activities

### `.card-type-label`

Text label for recommendation type.

```css
.card-type-label {
    font-size: 12px;
    font-weight: 600;
    color: #7E57C2;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}
```

**Properties:**
- `font-size: 12px` - Small text
- `font-weight: 600` - Semi-bold
- `color: #7E57C2` - Purple accent
- `text-transform: uppercase` - All caps
- `letter-spacing: 0.5px` - Slightly spaced

## Content Classes

### `.card-title`

Main heading for the recommendation.

```css
.card-title {
    font-size: 20px;
    font-weight: 700;
    color: var(--card-title-color);
    margin: 0 0 12px 0;
    line-height: 1.3;
}
```

**Properties:**
- `font-size: 20px` - Large, prominent
- `font-weight: 700` - Bold
- `color: #1565C0` - Blue
- `line-height: 1.3` - Comfortable reading

**Mobile:**
```css
@media (max-width: 768px) {
    .card-title {
        font-size: 18px;
    }
}
```

### `.card-description`

Brief description text.

```css
.card-description {
    font-size: 13px;
    color: var(--card-text-color);
    margin: 0 0 12px 0;
    line-height: 1.5;
}
```

**Properties:**
- `font-size: 13px` - Smaller than title
- `color: #424242` - Dark gray
- `line-height: 1.5` - Readable spacing

### `.card-reason`

Container for reason explanation.

```css
.card-reason {
    display: flex;
    gap: 8px;
    margin-bottom: 16px;
    padding: 12px;
    background: rgba(255, 255, 255, 0.6);
    border-radius: 8px;
}
```

**Properties:**
- `display: flex` - Horizontal layout
- `gap: 8px` - Space between icon and text
- `padding: 12px` - Internal spacing
- `background` - Semi-transparent white
- `border-radius: 8px` - Rounded corners

**Mobile:**
```css
@media (max-width: 768px) {
    .card-reason {
        padding: 10px;
    }
}
```

### `.reason-icon`

Icon for reason section.

```css
.reason-icon {
    font-size: 18px;
    flex-shrink: 0;
}
```

**Properties:**
- `font-size: 18px` - Medium size
- `flex-shrink: 0` - Prevents icon from shrinking

**Icon:**
- 💡 (light bulb) for all reasons

### `.reason-text`

Text explaining why the recommendation fits.

```css
.reason-text {
    font-size: 14px;
    color: var(--card-reason-color);
    margin: 0;
    line-height: 1.6;
}
```

**Properties:**
- `font-size: 14px` - Medium text
- `color: #616161` - Medium gray
- `line-height: 1.6` - Comfortable reading

## Score Classes

### `.card-score`

Container for relevance score.

```css
.card-score {
    display: flex;
    align-items: center;
    gap: 6px;
}
```

**Usage:**
```html
<div class="card-score">
    <span class="score-icon">✨</span>
    <span class="score-text" style="color: #00A86B">
        Độ phù hợp: 95%
    </span>
</div>
```

### `.score-icon`

Icon for score section.

```css
.score-icon {
    font-size: 16px;
}
```

**Icon:**
- ✨ (sparkles) for all scores

### `.score-text`

Text displaying the relevance percentage.

```css
.score-text {
    font-size: 14px;
    font-weight: 600;
}
```

**Properties:**
- `font-size: 14px` - Medium text
- `font-weight: 600` - Semi-bold
- `color` - Dynamic (set inline based on score)

**Color Mapping:**
```javascript
function getScoreColor(score) {
    if (score >= 90) return '#00A86B'; // Dark green
    if (score >= 70) return '#32CD32'; // Medium green
    if (score >= 50) return '#FFD700'; // Yellow
    return '#FF8C00'; // Orange
}
```

## Accessibility Classes

### `.sr-only`

Screen reader only text (visually hidden).

```css
.sr-only {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}
```

**Usage:**
```html
<span class="sr-only">Độ phù hợp: 95 phần trăm</span>
```

**Purpose:**
- Provides context for screen readers
- Completely hidden visually
- Accessible to assistive technologies

## Responsive Design

### Mobile Breakpoint

All mobile styles use the same breakpoint:

```css
@media (max-width: 768px) {
    /* Mobile styles */
}
```

### Mobile Adjustments

```css
@media (max-width: 768px) {
    .recommendation-card {
        padding: 16px;
    }
    
    .card-title {
        font-size: 18px;
    }
    
    .card-reason {
        padding: 10px;
    }
}
```

**Changes:**
- Reduced padding (20px → 16px)
- Smaller title (20px → 18px)
- Smaller reason padding (12px → 10px)

## Animation Details

### Hover Animation

```css
.recommendation-card {
    transition: all 0.3s ease;
}

.recommendation-card:hover {
    transform: translateY(-4px);
    box-shadow: 0 8px 16px rgba(0, 0, 0, 0.15);
}
```

**Effect:**
- Duration: 300ms
- Easing: ease (smooth acceleration/deceleration)
- Transform: Moves up 4px
- Shadow: Increases for depth perception

### Focus Animation

```css
.recommendation-card:focus {
    outline: 2px solid #1565C0;
    outline-offset: 2px;
}
```

**Effect:**
- Blue outline for visibility
- 2px offset prevents overlap with card

## Browser Compatibility

### Supported Browsers

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

### CSS Features Used

- CSS Custom Properties (variables)
- Flexbox
- CSS Grid (for container)
- Transform
- Transition
- Box Shadow
- Linear Gradient
- Pseudo-elements (::before)

### Fallbacks

For older browsers, consider:

```css
.recommendation-card {
    background: #E8EAF6; /* Fallback solid color */
    background: linear-gradient(135deg, #E8EAF6 0%, #C5CAE9 100%);
}
```

## Customization Examples

### Example 1: Dark Theme

```css
:root {
    --card-bg-gradient: linear-gradient(135deg, #263238 0%, #37474F 100%);
    --card-title-color: #64B5F6;
    --card-text-color: #ECEFF1;
    --card-reason-color: #B0BEC5;
}

.card-reason {
    background: rgba(0, 0, 0, 0.3);
}
```

### Example 2: Larger Cards

```css
:root {
    --card-padding: 24px;
    --card-gap: 20px;
}

.card-title {
    font-size: 24px;
}

.card-description {
    font-size: 15px;
}
```

### Example 3: Compact Cards

```css
:root {
    --card-padding: 16px;
    --card-gap: 12px;
}

.card-title {
    font-size: 18px;
    margin-bottom: 8px;
}

.card-reason {
    padding: 8px;
    margin-bottom: 12px;
}
```

### Example 4: Custom Colors

```css
:root {
    /* Orange theme */
    --card-bg-gradient: linear-gradient(135deg, #FFF3E0 0%, #FFE0B2 100%);
    --card-title-color: #E65100;
    --score-excellent: #2E7D32;
    --score-good: #558B2F;
    --score-fair: #F57C00;
    --score-low: #D84315;
}

.card-type-label {
    color: #F57C00;
}
```

## Performance Considerations

### GPU Acceleration

The transform property triggers GPU acceleration:

```css
.recommendation-card:hover {
    transform: translateY(-4px);
    /* Uses GPU, smooth animation */
}
```

### Avoiding Repaints

Properties that don't trigger layout recalculation:
- `transform`
- `opacity`
- `box-shadow` (with caution)

Avoid animating:
- `width`, `height`
- `padding`, `margin`
- `top`, `left`

### CSS Optimization

```css
/* Good: Uses transform */
.recommendation-card:hover {
    transform: translateY(-4px);
}

/* Avoid: Triggers layout */
.recommendation-card:hover {
    margin-top: -4px;
}
```

## Debugging Tips

### Inspect Card Structure

Use browser DevTools to inspect:

```
.recommendation-card
├── ::before (left border)
├── .card-header
│   ├── .card-type-icon
│   └── .card-type-label
├── .card-title
├── .card-description
├── .card-reason
│   ├── .reason-icon
│   └── .reason-text
└── .card-score
    ├── .score-icon
    ├── .score-text
    └── .sr-only
```

### Common Issues

**Cards not displaying:**
- Check if CSS file is loaded
- Verify `.recommendations-container` exists
- Check for JavaScript errors

**Hover not working:**
- Verify `cursor: pointer` is applied
- Check if `transition` property is set
- Ensure no overlapping elements

**Colors not showing:**
- Check CSS variable definitions
- Verify inline styles for score colors
- Check browser compatibility

**Mobile layout broken:**
- Verify media query syntax
- Check viewport meta tag
- Test on actual devices

## Testing Checklist

- [ ] Cards display correctly on desktop
- [ ] Cards display correctly on mobile
- [ ] Hover animation works smoothly
- [ ] Focus outline visible for keyboard navigation
- [ ] Colors match design specifications
- [ ] Text is readable on all backgrounds
- [ ] Score colors update based on value
- [ ] Cards are clickable
- [ ] Responsive breakpoints work
- [ ] No layout shifts or jumps
- [ ] Animations are smooth (60fps)
- [ ] Works in all supported browsers

## Additional Resources

- [API Documentation](./API_DOCUMENTATION.md)
- [Developer Guide](./DEVELOPER_GUIDE.md)
- [Troubleshooting Guide](./TROUBLESHOOTING_GUIDE.md)
