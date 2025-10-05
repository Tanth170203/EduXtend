# Academic Year Management API

## Overview
This API provides CRUD operations for managing academic years in the EduXtend system.

## Endpoints

### 1. Get All Academic Years
- **GET** `/api/academic-years`
- **Description**: Retrieve all academic years ordered by start date (newest first)
- **Authentication**: Required (JWT Bearer token)
- **Response**: Array of AcademicYearDto objects

### 2. Get Academic Year by ID
- **GET** `/api/academic-years/{id}`
- **Description**: Retrieve a specific academic year by ID
- **Authentication**: Required (JWT Bearer token)
- **Response**: AcademicYearDto object or 404 if not found

### 3. Get Active Academic Year
- **GET** `/api/academic-years/active`
- **Description**: Retrieve the currently active academic year
- **Authentication**: Required (JWT Bearer token)
- **Response**: AcademicYearDto object or 404 if no active year

### 4. Create Academic Year
- **POST** `/api/academic-years`
- **Description**: Create a new academic year
- **Authentication**: Required (JWT Bearer token)
- **Request Body**: CreateAcademicYearDto
- **Response**: Created AcademicYearDto (201 Created)

### 5. Update Academic Year
- **PUT** `/api/academic-years/{id}`
- **Description**: Update an existing academic year
- **Authentication**: Required (JWT Bearer token)
- **Request Body**: UpdateAcademicYearDto
- **Response**: Updated AcademicYearDto (200 OK)

### 6. Delete Academic Year
- **DELETE** `/api/academic-years/{id}`
- **Description**: Delete an academic year
- **Authentication**: Required (JWT Bearer token)
- **Response**: 204 No Content if successful, 404 if not found

## Data Models

### AcademicYearDto
```json
{
  "id": 1,
  "name": "Academic Year 2024-2025",
  "startDate": "2024-09-01T00:00:00Z",
  "endDate": "2025-08-31T23:59:59Z",
  "isActive": true
}
```

### CreateAcademicYearDto
```json
{
  "name": "Academic Year 2024-2025",
  "startDate": "2024-09-01T00:00:00Z",
  "endDate": "2025-08-31T23:59:59Z",
  "isActive": false
}
```

### UpdateAcademicYearDto
```json
{
  "name": "Academic Year 2024-2025 (Updated)",
  "startDate": "2024-09-01T00:00:00Z",
  "endDate": "2025-08-31T23:59:59Z",
  "isActive": true
}
```

## Business Rules

1. **Start Date Validation**: Start date must be before end date
2. **Active Year Management**: Only one academic year can be active at a time
3. **Automatic Deactivation**: When setting a year as active, all other years are automatically deactivated

## Error Handling

- **400 Bad Request**: Invalid input data or business rule violations
- **401 Unauthorized**: Missing or invalid JWT token
- **404 Not Found**: Academic year not found
- **500 Internal Server Error**: Server-side errors

## Testing

Use the provided `AcademicYear.http` file to test the endpoints. Make sure to:
1. First authenticate and get a JWT token
2. Replace `{{token}}` with your actual JWT token
3. Run the requests in order

## Security

- All endpoints require JWT authentication
- Input validation is performed on all requests
- Business rules are enforced to maintain data integrity
