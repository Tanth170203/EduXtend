# Requirements Document

## Introduction

Fix the CV viewer functionality on the Student's "My Applications" page where students cannot view their own CV but Club Managers can view student CVs without issues.

## Glossary

- **CV Viewer**: A modal component that displays PDF files (CVs) using PDF.js library
- **Student**: A user with Student role who can apply to clubs
- **Club Manager**: A user who manages club applications and can view applicant CVs
- **CORS**: Cross-Origin Resource Sharing - browser security feature
- **PDF.js**: JavaScript library for rendering PDF files in browsers

## Requirements

### Requirement 1

**User Story:** As a student, I want to view my own CV from the My Applications page, so that I can verify what CV I submitted to clubs.

#### Acceptance Criteria

1. WHEN a student clicks the "View CV" button on their application THEN the system SHALL display the CV in a modal viewer
2. WHEN the CV URL is from an external source (Cloudinary) THEN the system SHALL handle CORS properly to load the PDF
3. WHEN the CV viewer modal opens THEN the system SHALL show a loading indicator while the PDF is being loaded
4. IF the CV fails to load THEN the system SHALL display a clear error message with an option to open the CV in a new tab
5. WHEN the CV loads successfully THEN the system SHALL render the PDF with zoom and page navigation controls

### Requirement 2

**User Story:** As a developer, I want consistent CV viewing behavior across all user roles, so that the feature works reliably for everyone.

#### Acceptance Criteria

1. WHEN any user (Student or Club Manager) views a CV THEN the system SHALL use the same CV viewer component
2. WHEN the CV viewer is initialized THEN the system SHALL ensure PDF.js library is loaded before attempting to render
3. WHEN handling CV URLs THEN the system SHALL properly configure PDF.js to handle external URLs with appropriate CORS settings
4. WHEN the page loads THEN the system SHALL ensure the CV viewer script is fully initialized before attaching event listeners
5. IF PDF.js fails to load THEN the system SHALL provide a fallback option to open the CV in a new tab
