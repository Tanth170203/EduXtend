using System.ComponentModel.DataAnnotations;

namespace BusinessObject.DTOs.MovementCriteria;

/// <summary>
/// DTO để trả về thông tin MovementCriterionGroup
/// </summary>
public class MovementCriterionGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public string TargetType { get; set; } = "Student"; // "Student" hoặc "Club"
    public int CriteriaCount { get; set; } // Số lượng tiêu chí trong nhóm
}

/// <summary>
/// DTO để tạo mới MovementCriterionGroup
/// </summary>
public class CreateMovementCriterionGroupDto
{
    [Required(ErrorMessage = "Tên nhóm tiêu chí là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Điểm tối đa phải từ 0 đến 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Loại đối tượng là bắt buộc")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Loại đối tượng phải là 'Student' hoặc 'Club'")]
    public string TargetType { get; set; } = "Student";
}

/// <summary>
/// DTO để cập nhật MovementCriterionGroup
/// </summary>
public class UpdateMovementCriterionGroupDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên nhóm tiêu chí là bắt buộc")]
    [MaxLength(100, ErrorMessage = "Tên không được vượt quá 100 ký tự")]
    public string Name { get; set; } = null!;

    [MaxLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự")]
    public string? Description { get; set; }

    [Range(0, 1000, ErrorMessage = "Điểm tối đa phải từ 0 đến 1000")]
    public int MaxScore { get; set; }

    [Required(ErrorMessage = "Loại đối tượng là bắt buộc")]
    [RegularExpression("^(Student|Club)$", ErrorMessage = "Loại đối tượng phải là 'Student' hoặc 'Club'")]
    public string TargetType { get; set; } = "Student";
}

/// <summary>
/// DTO chi tiết của MovementCriterionGroup bao gồm các tiêu chí con
/// </summary>
public class MovementCriterionGroupDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxScore { get; set; }
    public string TargetType { get; set; } = "Student";
    public List<MovementCriterionDto> Criteria { get; set; } = new();
}


