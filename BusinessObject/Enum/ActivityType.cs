using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObject.Enum
{
    /// <summary>
    /// Các loại hoạt động thực tế trong CLB
    /// Dựa trên bảng chấm điểm và hoạt động thực tế của sinh viên
    /// </summary>
    public enum ActivityType
    {
        // === HOẠT ĐỘNG CLB (Club Activities) ===
        /// <summary>Họp CLB thường kỳ - VD: Họp CLB Tin học mỗi thứ 3</summary>
        ClubMeeting = 0,
        
        /// <summary>Training nội bộ CLB - VD: Training ReactJS cho thành viên</summary>
        ClubTraining = 1,
        
        /// <summary>Workshop kỹ năng - VD: Workshop Photoshop, IELTS</summary>
        ClubWorkshop = 2,
        
        // === SỰ KIỆN (Events) ===
        /// <summary>Sự kiện lớn (100-200 người) - VD: Hội thảo AI 2024 (150 người)</summary>
        LargeEvent = 3,
        
        /// <summary>Sự kiện trung (50-100 người) - VD: Workshop Python (80 người)</summary>
        MediumEvent = 4,
        
        /// <summary>Sự kiện nhỏ (<50 người) - VD: Team building (30 người)</summary>
        SmallEvent = 5,
        
        // === CUỘC THI (Competitions) ===
        /// <summary>Cuộc thi cấp trường - VD: Cuộc thi Lập trình FPT</summary>
        SchoolCompetition = 6,
        
        /// <summary>Cuộc thi cấp tỉnh/TP - VD: Olympic Tin học TP.HCM</summary>
        ProvincialCompetition = 7,
        
        /// <summary>Cuộc thi cấp quốc gia - VD: Olympic Tin học VN, ACM/ICPC</summary>
        NationalCompetition = 8,
        
        // === TÌNH NGUYỆN (Volunteer) ===
        /// <summary>Hoạt động tình nguyện - VD: Quyên góp từ thiện, dọn bãi biển</summary>
        Volunteer = 9,
        
        // === PHỐI HỢP (Collaboration) ===
        /// <summary>Phối hợp với CLB khác - VD: CLB Tin + CLB Anh = Workshop lập trình bằng tiếng Anh</summary>
        ClubCollaboration = 10,
        
        /// <summary>Phối hợp với Nhà trường - VD: Hỗ trợ Ngày hội việc làm</summary>
        SchoolCollaboration = 11,
        
        /// <summary>Phối hợp với Doanh nghiệp - VD: Workshop với FPT Software</summary>
        EnterpriseCollaboration = 12,
        
        // === KHÁC ===
        /// <summary>Hoạt động khác không thuộc loại trên</summary>
        Other = 13
    }
}
