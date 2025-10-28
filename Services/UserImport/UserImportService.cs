using BusinessObject.DTOs.ImportFile;
using BusinessObject.Models;
using BusinessObject.Enum;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Repositories.Users;
using Repositories.Students;
using Repositories.Majors;

namespace Services.UserImport
{
    public class UserImportService : IUserImportService
    {
        private readonly IUserRepository _userRepo;
        private readonly IStudentRepository _studentRepo;
        private readonly IMajorRepository _majorRepo;

        public UserImportService(
            IUserRepository userRepo,
            IStudentRepository studentRepo,
            IMajorRepository majorRepo)
        {
            _userRepo = userRepo;
            _studentRepo = studentRepo;
            _majorRepo = majorRepo;
        }

        public async Task<ImportUsersResponse> ImportUsersFromExcelAsync(IFormFile file)
        {
            var response = new ImportUsersResponse();

            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid or empty file.");
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLower();
            if (extension != ".xlsx" && extension != ".xls")
            {
                throw new ArgumentException("Only Excel files (.xlsx, .xls) are accepted");
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            var userRows = new List<UserImportRow>();
            
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets.FirstOrDefault();
                    
                    if (worksheet == null)
                    {
                        throw new ArgumentException("Excel file has no worksheets.");
                    }

                    var rowCount = worksheet.Dimension?.Rows ?? 0;
                    
                    if (rowCount < 2) // Header + at least 1 data row
                    {
                        throw new ArgumentException("Excel file has no data.");
                    }

                    // Read data starting from row 2 (assuming row 1 is header)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        var email = worksheet.Cells[row, 1].Value?.ToString()?.Trim();
                        var fullName = worksheet.Cells[row, 2].Value?.ToString()?.Trim();
                        var phoneNumber = worksheet.Cells[row, 3].Value?.ToString()?.Trim();
                        var roles = worksheet.Cells[row, 4].Value?.ToString()?.Trim();
                        var isActiveStr = worksheet.Cells[row, 5].Value?.ToString()?.Trim();
                        
                        // Student-specific fields
                        var studentCode = worksheet.Cells[row, 6].Value?.ToString()?.Trim();
                        var cohort = worksheet.Cells[row, 7].Value?.ToString()?.Trim();
                        var dateOfBirthStr = worksheet.Cells[row, 8].Value?.ToString()?.Trim();
                        var gender = worksheet.Cells[row, 9].Value?.ToString()?.Trim();
                        var enrollmentDateStr = worksheet.Cells[row, 10].Value?.ToString()?.Trim();
                        var majorCode = worksheet.Cells[row, 11].Value?.ToString()?.Trim();
                        var studentStatus = worksheet.Cells[row, 12].Value?.ToString()?.Trim();

                        // Skip empty rows
                        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(fullName))
                        {
                            continue;
                        }

                        // Validate required fields
                        if (string.IsNullOrWhiteSpace(email))
                        {
                            response.Errors.Add(new ImportUserError
                            {
                                RowNumber = row,
                                Email = email ?? "",
                                ErrorMessage = "Email cannot be empty"
                            });
                            response.FailureCount++;
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            response.Errors.Add(new ImportUserError
                            {
                                RowNumber = row,
                                Email = email,
                                ErrorMessage = "Full name cannot be empty"
                            });
                            response.FailureCount++;
                            continue;
                        }

                        // Parse IsActive (default to true if not specified)
                        bool isActive = true;
                        if (!string.IsNullOrWhiteSpace(isActiveStr))
                        {
                            if (isActiveStr.Equals("true", StringComparison.OrdinalIgnoreCase) || 
                                isActiveStr == "1" || 
                                isActiveStr.Equals("yes", StringComparison.OrdinalIgnoreCase))
                            {
                                isActive = true;
                            }
                            else if (isActiveStr.Equals("false", StringComparison.OrdinalIgnoreCase) || 
                                     isActiveStr == "0" || 
                                     isActiveStr.Equals("no", StringComparison.OrdinalIgnoreCase))
                            {
                                isActive = false;
                            }
                        }

                        // Parse DateOfBirth
                        DateTime? dateOfBirth = null;
                        if (!string.IsNullOrWhiteSpace(dateOfBirthStr))
                        {
                            if (DateTime.TryParse(dateOfBirthStr, out DateTime parsedDate))
                            {
                                dateOfBirth = parsedDate;
                            }
                        }

                        // Parse EnrollmentDate
                        DateTime? enrollmentDate = null;
                        if (!string.IsNullOrWhiteSpace(enrollmentDateStr))
                        {
                            if (DateTime.TryParse(enrollmentDateStr, out DateTime parsedDate))
                            {
                                enrollmentDate = parsedDate;
                            }
                        }

                        userRows.Add(new UserImportRow
                        {
                            Email = email,
                            FullName = fullName,
                            PhoneNumber = phoneNumber,
                            Roles = roles,
                            IsActive = isActive,
                            StudentCode = studentCode,
                            Cohort = cohort,
                            DateOfBirth = dateOfBirth,
                            Gender = gender,
                            EnrollmentDate = enrollmentDate,
                            MajorCode = majorCode,
                            StudentStatus = studentStatus
                        });
                        response.TotalRows++;
                    }
                }
            }

            if (userRows.Count == 0)
            {
                throw new ArgumentException("No valid data found in file.");
            }

            // Check for existing users
            var emails = userRows.Select(u => u.Email).ToList();
            var existingUsers = await _userRepo.GetUsersByEmailsAsync(emails);
            var existingEmails = existingUsers.Select(u => u.Email.ToLower()).ToHashSet();

            // Get all unique role names from import
            var allRoleNames = userRows
                .Where(u => !string.IsNullOrWhiteSpace(u.Roles))
                .SelectMany(u => u.Roles!.Split(',', StringSplitOptions.RemoveEmptyEntries))
                .Select(r => r.Trim())
                .Distinct()
                .ToList();

            // Get role IDs from database
            var roleMapping = await _userRepo.GetRoleIdsByNamesAsync(allRoleNames);

            // Get major codes for students
            var studentRows = userRows.Where(u => !string.IsNullOrWhiteSpace(u.Roles) && 
                                                 u.Roles.Contains("Student", StringComparison.OrdinalIgnoreCase) &&
                                                 !string.IsNullOrWhiteSpace(u.MajorCode)).ToList();
            var majorCodes = studentRows.Select(s => s.MajorCode!).Distinct().ToList();
            var majorMapping = await _majorRepo.GetMajorIdsByCodesAsync(majorCodes);

            // Check for existing students
            var studentCodes = studentRows.Where(s => !string.IsNullOrWhiteSpace(s.StudentCode))
                                         .Select(s => s.StudentCode!).ToList();
            var existingStudents = await _studentRepo.GetByStudentCodesAsync(studentCodes);
            var existingStudentCodes = existingStudents.Select(s => s.StudentCode).ToHashSet();

            // Process users
            var usersToAdd = new List<User>();
            var studentsToAdd = new List<Student>();
            int rowNumber = 2; // Starting from row 2 in Excel

            foreach (var userRow in userRows)
            {
                // Check if user already exists
                if (existingEmails.Contains(userRow.Email.ToLower()))
                {
                    response.Errors.Add(new ImportUserError
                    {
                        RowNumber = rowNumber,
                        Email = userRow.Email,
                        ErrorMessage = "Email already exists in the system"
                    });
                    response.FailureCount++;
                    rowNumber++;
                    continue;
                }

                var newUser = new User
                {
                    Email = userRow.Email,
                    FullName = userRow.FullName,
                    PhoneNumber = userRow.PhoneNumber,
                    IsActive = userRow.IsActive,
                    CreatedAt = DateTime.UtcNow
                };

                // Add roles if specified
                if (!string.IsNullOrWhiteSpace(userRow.Roles))
                {
                    var roleNames = userRow.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(r => r.Trim())
                        .ToList();

                    // Take only the first role (since user can have only one role now)
                    var firstRoleName = roleNames.FirstOrDefault();
                    if (firstRoleName != null)
                    {
                        if (roleMapping.TryGetValue(firstRoleName, out int roleId))
                        {
                            newUser.RoleId = roleId;
                        }
                        else
                        {
                            response.Errors.Add(new ImportUserError
                            {
                                RowNumber = rowNumber,
                                Email = userRow.Email,
                                ErrorMessage = $"Role '{firstRoleName}' does not exist in the system"
                            });
                            continue; // Skip this user
                        }
                    }
                }
                else
                {
                    // Assign default Student role if no role specified
                    if (roleMapping.TryGetValue("Student", out int studentRoleId))
                    {
                        newUser.RoleId = studentRoleId;
                    }
                }

                usersToAdd.Add(newUser);
                response.SuccessMessages.Add($"Row {rowNumber}: {userRow.Email} - {userRow.FullName}");
                response.SuccessCount++;
                rowNumber++;
            }

            // Save all users to database first
            if (usersToAdd.Count > 0)
            {
                await _userRepo.AddRangeAsync(usersToAdd);
                
                // Now create Student records for users with Student role
                foreach (var userRow in userRows)
                {
                    if (!string.IsNullOrWhiteSpace(userRow.Roles) && 
                        userRow.Roles.Contains("Student", StringComparison.OrdinalIgnoreCase))
                    {
                        // Find the created user
                        var createdUser = usersToAdd.FirstOrDefault(u => u.Email == userRow.Email);
                        if (createdUser != null)
                        {
                            // Validate student data
                            if (string.IsNullOrWhiteSpace(userRow.StudentCode))
                            {
                                response.Errors.Add(new ImportUserError
                                {
                                    RowNumber = userRows.IndexOf(userRow) + 2,
                                    Email = userRow.Email,
                                    ErrorMessage = "Student code is required for Student role"
                                });
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(userRow.Cohort))
                            {
                                response.Errors.Add(new ImportUserError
                                {
                                    RowNumber = userRows.IndexOf(userRow) + 2,
                                    Email = userRow.Email,
                                    ErrorMessage = "Cohort is required for Student role"
                                });
                                continue;
                            }

                            if (string.IsNullOrWhiteSpace(userRow.MajorCode))
                            {
                                response.Errors.Add(new ImportUserError
                                {
                                    RowNumber = userRows.IndexOf(userRow) + 2,
                                    Email = userRow.Email,
                                    ErrorMessage = "Major code is required for Student role"
                                });
                                continue;
                            }

                            // Check if student code already exists
                            if (existingStudentCodes.Contains(userRow.StudentCode))
                            {
                                response.Errors.Add(new ImportUserError
                                {
                                    RowNumber = userRows.IndexOf(userRow) + 2,
                                    Email = userRow.Email,
                                    ErrorMessage = $"Student code '{userRow.StudentCode}' already exists"
                                });
                                continue;
                            }

                            // Get major ID
                            if (!majorMapping.TryGetValue(userRow.MajorCode, out int majorId))
                            {
                                response.Errors.Add(new ImportUserError
                                {
                                    RowNumber = userRows.IndexOf(userRow) + 2,
                                    Email = userRow.Email,
                                    ErrorMessage = $"Major code '{userRow.MajorCode}' does not exist"
                                });
                                continue;
                            }

                            // Parse gender
                            if (!Enum.TryParse<Gender>(userRow.Gender, true, out Gender gender))
                            {
                                gender = Gender.Other; // Default value
                            }

                            // Parse student status
                            if (!Enum.TryParse<StudentStatus>(userRow.StudentStatus, true, out StudentStatus status))
                            {
                                status = StudentStatus.Active; // Default value
                            }

                            var newStudent = new Student
                            {
                                StudentCode = userRow.StudentCode,
                                Cohort = userRow.Cohort,
                                FullName = userRow.FullName,
                                Email = userRow.Email,
                                Phone = userRow.PhoneNumber,
                                DateOfBirth = userRow.DateOfBirth ?? DateTime.Now.AddYears(-20), // Default age
                                Gender = gender,
                                EnrollmentDate = userRow.EnrollmentDate ?? DateTime.Now,
                                Status = status,
                                UserId = createdUser.Id,
                                MajorId = majorId
                            };

                            studentsToAdd.Add(newStudent);
                        }
                    }
                }

                // Save all students to database
                if (studentsToAdd.Count > 0)
                {
                    await _studentRepo.AddRangeAsync(studentsToAdd);
                }
            }

            return response;
        }
    }
}

