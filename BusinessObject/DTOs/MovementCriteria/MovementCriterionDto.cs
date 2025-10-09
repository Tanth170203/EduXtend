using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementCriteria;

/// <summary>
/// DTO để trả về thông tin MovementCriterion
/// </summary>
public class MovementCriterionDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string? GroupName { get; set; } // Tên nhóm tiêu chí
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public string TargetType { get; set; } = "Student"; // "Student" hoặc "Club"
    public string? DataSource { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO để tạo mới MovementCriterion
/// </summary>
public class CreateMovementCriterionDto
{
    [Required(ErrorMessage = "ID nhóm tiêu chí là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "ID nhóm tiêu chí phải lớn hơn 0")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Điểm tối đa phải từ 0 đến 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Loại đối tượng là bắt buộc")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Loại đối tượng phải là 'Student' hoặc 'Club'")]
    public string TargetType { get; set; } = "Student";

    [MaxLength(200, ErrorMessage = "Nguồn dữ liệu không được vượt quá 200 ký tự")]
    public string? DataSource { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO để cập nhật MovementCriterion
/// </summary>
public class UpdateMovementCriterionDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "ID nhóm tiêu chí là bắt buộc")]
    [Range(1, int.MaxValue, ErrorMessage = "ID nhóm tiêu chí phải lớn hơn 0")]
    public int GroupId { get; set; }

    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [MaxLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự")]
    public string Title { get; set; } = null!;

    [MaxLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Điểm tối đa phải từ 0 đến 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Loại đối tượng là bắt buộc")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Loại đối tượng phải là 'Student' hoặc 'Club'")]
    public string TargetType { get; set; } = "Student";

    [MaxLength(200, ErrorMessage = "Nguồn dữ liệu không được vượt quá 200 ký tự")]
    public string? DataSource { get; set; }

    public bool IsActive { get; set; } = true;
}


