using BusinessObject.DTOs.MonthlyReport;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Services.MonthlyReports;

public class MonthlyReportPdfService : IMonthlyReportPdfService
{
    private readonly IMonthlyReportService _monthlyReportService;

    public MonthlyReportPdfService(IMonthlyReportService monthlyReportService)
    {
        _monthlyReportService = monthlyReportService;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportToPdfAsync(int reportId)
    {
        var report = await _monthlyReportService.GetReportWithFreshDataAsync(reportId);
        
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().Element(c => ComposeFooter(c, report.Footer));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader(IContainer container, MonthlyReportDto report)
    {
        container.Column(column =>
        {
            // Two-column header layout
            column.Item().Row(row =>
            {
                // Left column - PDP info
                row.RelativeItem(45).Column(leftCol =>
                {
                    leftCol.Item().AlignCenter().Text("PDP").Bold().FontSize(11);
                    leftCol.Item().AlignCenter().Text("PHÒNG HỢP TÁC QUỐC TẾ").Bold().FontSize(11);
                    leftCol.Item().AlignCenter().Text("& PHÁT TRIỂN CÁ NHÂN").Bold().FontSize(11);
                });
                
                // Right column - Report title
                row.RelativeItem(55).Column(rightCol =>
                {
                    rightCol.Item().AlignCenter().Text($"BÁO CÁO HOẠT ĐỘNG THÁNG {report.ReportMonth} VÀ KẾ HOẠCH HOẠT ĐỘNG THÁNG {report.NextMonth}")
                        .Bold().FontSize(13);
                    rightCol.Item().PaddingTop(5).AlignCenter().Text($"{report.Header.Location}, Ngày {report.Header.ReportDate.Day} tháng {report.Header.ReportDate.Month} năm {report.Header.ReportDate.Year}")
                        .Italic().FontSize(11);
                });
            });
            
            // Club name below
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem(45).Column(leftCol =>
                {
                    leftCol.Item().AlignCenter().Text(report.Header.ClubName).Bold().FontSize(12);
                });
                row.RelativeItem(55);
            });
            
            // Creator info
            column.Item().PaddingTop(10).Column(col =>
            {
                col.Item().Text($"Họ và tên: {report.Footer.CreatorName}").Bold();
                col.Item().Text($"Chức vụ: {report.Footer.CreatorPosition}").Bold();
                col.Item().Text($"Hôm nay, ngày {report.Header.ReportDate.Day} tháng {report.Header.ReportDate.Month} năm {report.Header.ReportDate.Year}, tôi đại diện CLB {report.Header.ClubName} xin báo cáo với phòng ICPDP (Hợp tác quốc tế & Phát triển cá nhân) như sau:");
            });
            
            column.Item().PaddingTop(10);
        });
    }

    private void ComposeContent(IContainer container, MonthlyReportDto report)
    {
        container.PaddingVertical(10).Column(column =>
        {
            // Part A: Current Month Activities
            column.Item().Element(c => ComposePartA(c, report));
            
            // Part B: Next Month Plans
            column.Item().PageBreak();
            column.Item().Element(c => ComposePartB(c, report));
        });
    }

    private void ComposePartA(IContainer container, MonthlyReportDto report)
    {
        container.Column(column =>
        {
            column.Item().Text($"A. HOẠT ĐỘNG THÁNG {report.ReportMonth}")
                .FontSize(14).Bold();
            
            column.Item().PaddingTop(10);
            
            // I. School Events
            column.Item().Text("I. Sự kiện cấp trường").FontSize(12).Bold();
            if (report.CurrentMonthActivities.SchoolEvents.Any())
            {
                int eventIndex = 1;
                foreach (var evt in report.CurrentMonthActivities.SchoolEvents)
                {
                    column.Item().PaddingTop(10).Element(c => ComposeSchoolEvent(c, evt));
                    eventIndex++;
                }
            }
            else
            {
                column.Item().PaddingTop(3).Text("Không có sự kiện cấp trường.").Italic();
            }
            
            // II. Support Activities
            column.Item().PaddingTop(10).Text("II. Các hoạt động hỗ trợ các phòng ban khác/bộ môn").FontSize(12).Bold();
            if (report.CurrentMonthActivities.SupportActivities.Any())
            {
                column.Item().PaddingTop(3).Element(c => ComposeSupportActivities(c, report.CurrentMonthActivities.SupportActivities));
            }
            else
            {
                column.Item().PaddingTop(3).Text("Không có hoạt động hỗ trợ.").Italic();
            }
            
            // III. Competitions
            column.Item().PaddingTop(10).Text("III. Cuộc thi cấp thành phố/ cấp vùng miền/ cấp quốc gia").FontSize(12).Bold();
            if (report.CurrentMonthActivities.Competitions.Any())
            {
                foreach (var comp in report.CurrentMonthActivities.Competitions)
                {
                    column.Item().PaddingTop(5).Element(c => ComposeCompetition(c, comp));
                }
            }
            else
            {
                column.Item().Text("1. Tên cuộc thi:");
                column.Item().Text("2. Đơn vị có thẩm quyền tổ chức:");
                column.Item().Text("3. Danh sách sinh viên tham gia: (Trống)");
            }
            
            // IV. Internal Meetings
            column.Item().PaddingTop(10).Text($"IV. Sinh hoạt nội bộ tháng {report.ReportMonth}").FontSize(12).Bold();
            column.Item().PaddingTop(3).Element(c => ComposeInternalMeetings(c, report.CurrentMonthActivities.InternalMeetings));
        });
    }

    private void ComposeSchoolEvent(IContainer container, SchoolEventDto evt)
    {
        container.Column(column =>
        {
            column.Item().Text($"Sự kiện: {evt.EventName}").Bold();
            column.Item().Text($"1. Thời gian diễn ra sự kiện: {evt.EventDate:dd/MM/yyyy HH:mm}");
            column.Item().Text($"2. Số lượng người tham gia sự kiện: {evt.ActualParticipants}");
            
            // Participants table
            if (evt.Participants.Any())
            {
                column.Item().PaddingTop(5).AlignCenter().Text("BẢNG THỐNG KÊ SỐ LƯỢNG NGƯỜI THAM DỰ CHƯƠNG TRÌNH").Bold().FontSize(11);
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Họ & tên").Bold();
                            col.Item().Text("MSSV").Bold();
                        });
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("SĐT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Điểm đánh giá").Bold();
                            col.Item().Text("(thang 3-5 điểm)").Bold().FontSize(9);
                        });
                    });

                    int index = 1;
                    foreach (var participant in evt.Participants)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Border(1).Padding(5).Column(col =>
                        {
                            col.Item().Text(participant.FullName);
                            col.Item().Text(participant.StudentCode);
                        });
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(participant.PhoneNumber);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(participant.Rating?.ToString("0.0") ?? "");
                        index++;
                    }
                });
            }
            
            // Evaluation
            column.Item().PaddingTop(5).Text("3. Đánh giá sự kiện");
            if (evt.Evaluation != null)
            {
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Số lượng dự kiến (A)").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Số lượng thực tế (B)").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Nguyên nhân nếu B < A").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Truyền thông (thang 10)").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Tổ chức (thang 10)").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("MC/ Host").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Diễn giả/ Biểu diễn").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Thành công").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Hạn chế").Bold().FontSize(9);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Biện pháp").Bold().FontSize(9);
                    });

                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.Evaluation.ExpectedCount.ToString()).FontSize(9);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.Evaluation.ActualCount.ToString()).FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.ReasonIfLess ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.Evaluation.CommunicationScore?.ToString("0.0") ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.Evaluation.OrganizationScore?.ToString("0.0") ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.McHostEvaluation ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.SpeakerPerformerEvaluation ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.Achievements ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.Limitations ?? "").FontSize(9);
                    table.Cell().Border(1).Padding(3).Text(evt.Evaluation.ProposedSolutions ?? "").FontSize(9);
                });
            }
            
            // Support Members
            column.Item().PaddingTop(5).Text("4. Tình hình hoạt động của CLB trước, trong và sau sự kiện");
            column.Item().Text("    a. Danh sách các thành viên tham gia hỗ trợ sự kiện");
            if (evt.SupportMembers.Any())
            {
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Họ & tên").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("MSSV").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("SĐT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Vị trí công việc").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                        {
                            col.Item().Text("Điểm đánh giá").Bold();
                            col.Item().Text("(thang 5-10 điểm)").Bold().FontSize(9);
                        });
                    });

                    int index = 1;
                    foreach (var member in evt.SupportMembers)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Border(1).Padding(5).Text(member.FullName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(member.StudentCode);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(member.PhoneNumber);
                        table.Cell().Border(1).Padding(5).Text(member.Position);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(member.Rating?.ToString("0.0") ?? "");
                        index++;
                    }
                });
            }
            
            // Timeline
            if (!string.IsNullOrWhiteSpace(evt.Timeline))
            {
                column.Item().PaddingTop(5).Text("    b. Timeline chi tiết sự kiện:");
                column.Item().Border(1).Padding(5).Text(evt.Timeline).FontSize(10);
            }
            
            column.Item().PaddingTop(5).Text($"5. Feedback sau sự kiện: {(string.IsNullOrEmpty(evt.FeedbackUrl) ? "(Đính kèm link feedback)" : evt.FeedbackUrl)}");
            column.Item().Text($"6. Hình ảnh, video của sự kiện: {(string.IsNullOrEmpty(evt.MediaUrls) ? "(Đính kèm link drive)" : evt.MediaUrls)}");
        });
    }

    private void ComposeSupportActivities(IContainer container, List<SupportActivityDto> activities)
    {
        container.Column(column =>
        {
            column.Item().Text("1. Tên sự kiện");
            column.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Nội dung sự kiện").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Phòng ban/bộ môn").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Địa điểm").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                    {
                        col.Item().Text("Hình ảnh hoạt động").Bold();
                        col.Item().Text("(Link hình ảnh)").Bold().FontSize(9);
                    });
                });

                int index = 1;
                foreach (var activity in activities)
                {
                    table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                    table.Cell().Border(1).Padding(5).Text(activity.EventContent);
                    table.Cell().Border(1).Padding(5).Text(activity.DepartmentName);
                    table.Cell().Border(1).Padding(5).AlignCenter().Text(activity.EventTime.ToString("dd/MM/yyyy"));
                    table.Cell().Border(1).Padding(5).Text(activity.Location);
                    table.Cell().Border(1).Padding(5).Text(activity.ImageUrl ?? "");
                    index++;
                }
            });

            column.Item().PaddingTop(5).Text("2. Danh sách sinh viên tham gia hỗ trợ");
            column.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Họ và tên").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Mã số sinh viên").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Tên sự kiện").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Điểm đánh giá").Bold();
                });

                int supCount = 1;
                foreach (var sa in activities)
                {
                    foreach (var s in sa.SupportStudents)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(supCount.ToString());
                        table.Cell().Border(1).Padding(5).Text(s.FullName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(s.StudentCode);
                        table.Cell().Border(1).Padding(5).Text(s.EventName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(s.EventTime.ToString("dd/MM/yyyy"));
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(s.Rating?.ToString() ?? "");
                        supCount++;
                    }
                }
            });
        });
    }

    private void ComposeCompetition(IContainer container, CompetitionDto competition)
    {
        container.Column(column =>
        {
            column.Item().Text($"1. Tên cuộc thi: {competition.CompetitionName}");
            column.Item().Text($"2. Đơn vị có thẩm quyền tổ chức: {competition.OrganizingUnit}");
            column.Item().Text("3. Danh sách sinh viên tham gia");
            
            if (competition.Participants.Any())
            {
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Họ và tên").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Mã số sinh viên").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Email").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Thành tích").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Ghi chú").Bold();
                    });

                    int index = 1;
                    foreach (var participant in competition.Participants)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Border(1).Padding(5).Text(participant.FullName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(participant.StudentCode);
                        table.Cell().Border(1).Padding(5).Text(participant.Email);
                        table.Cell().Border(1).Padding(5).Text(participant.Achievement ?? "");
                        table.Cell().Border(1).Padding(5).Text(participant.Note ?? "");
                        index++;
                    }
                });
            }
        });
    }

    private void ComposeInternalMeetings(IContainer container, List<InternalMeetingDto> meetings)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(1);
                columns.RelativeColumn(3);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Địa điểm").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Số người tham gia").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Nội dung sinh hoạt chuyên môn").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Hình ảnh (đính kèm link)").Bold();
            });

            int index = 1;
            foreach (var meeting in meetings)
            {
                table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                table.Cell().Border(1).Padding(5).AlignCenter().Text(meeting.MeetingTime.ToString("dd/MM/yyyy"));
                table.Cell().Border(1).Padding(5).Text(meeting.Location);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(meeting.ParticipantCount.ToString());
                table.Cell().Border(1).Padding(5).Text(meeting.Content);
                table.Cell().Border(1).Padding(5).Text(meeting.ImageUrl ?? "");
                index++;
            }
        });
    }


    private void ComposePartB(IContainer container, MonthlyReportDto report)
    {
        container.Column(column =>
        {
            column.Item().Text($"B. KẾ HOẠCH THÁNG {report.NextMonth}")
                .FontSize(14).Bold();
            
            column.Item().PaddingTop(10);
            
            // I. Purpose and Significance
            column.Item().Text("I. Mục đích và ý nghĩa:").FontSize(12).Bold();
            column.Item().PaddingTop(3).Text(report.NextMonthPlans.Purpose.Purpose);
            column.Item().Text(report.NextMonthPlans.Purpose.Significance);
            
            // II. School Events
            column.Item().PaddingTop(10).Text("II. Sự kiện cấp trường").FontSize(12).Bold();
            if (report.NextMonthPlans.PlannedEvents.Any())
            {
                int planIndex = 1;
                foreach (var evt in report.NextMonthPlans.PlannedEvents)
                {
                    column.Item().PaddingTop(5).Element(c => ComposePlannedEvent(c, evt, planIndex));
                    planIndex++;
                }
            }
            else
            {
                column.Item().Text("1. Tên sự kiện: (Chưa có)");
            }
            
            // III. Competitions
            column.Item().PaddingTop(10).Text("III. Cuộc thi cấp thành phố/ vùng miền/ quốc gia").FontSize(12).Bold();
            if (report.NextMonthPlans.PlannedCompetitions.Any())
            {
                foreach (var comp in report.NextMonthPlans.PlannedCompetitions)
                {
                    column.Item().PaddingTop(5).Element(c => ComposePlannedCompetition(c, comp));
                }
            }
            else
            {
                column.Item().Text("1. Tên cuộc thi: (Chưa có)");
            }
            
            // IV. Communication Plan
            column.Item().PaddingTop(10).Text("IV. Kế hoạch truyền thông").FontSize(12).Bold();
            column.Item().PaddingTop(3).AlignCenter().Text("BẢNG KẾ HOẠCH TRUYỀN THÔNG").Bold().FontSize(11);
            column.Item().PaddingTop(3).Element(c => ComposeCommunicationPlan(c, report.NextMonthPlans.CommunicationPlan));
            
            // V. Budget Support
            column.Item().PaddingTop(10).Text("V. Hỗ trợ kinh phí").FontSize(12).Bold();
            column.Item().PaddingTop(3).Element(c => ComposeBudget(c, report.NextMonthPlans.Budget));
            
            // VI. Facility Usage
            column.Item().PaddingTop(10).Text("VI. Kế hoạch sử dụng cơ sở vật chất cho sự kiện/ sinh hoạt nội bộ").FontSize(12).Bold();
            column.Item().PaddingTop(3).Element(c => ComposeFacility(c, report.NextMonthPlans.Facility));
            
            // VII. Club Responsibilities
            column.Item().PaddingTop(10).Text("VII. Trách nhiệm của CLB:").FontSize(12).Bold();
            column.Item().PaddingTop(3).Element(c => ComposeResponsibilities(c, report.NextMonthPlans.Responsibilities));
        });
    }

    private void ComposePlannedEvent(IContainer container, PlannedEventDto evt, int eventIndex)
    {
        container.Column(column =>
        {
            column.Item().Text($"{eventIndex}. Tên sự kiện: {evt.EventName}");
            column.Item().Text($"2. Nội dung chi tiết về sự kiện: {evt.EventContent}");
            
            column.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian tổ chức").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Địa điểm tổ chức").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Số lượng SV dự kiến").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Link đăng ký (nếu có)").Bold();
                });

                table.Cell().Border(1).Padding(5).AlignCenter().Text(evt.OrganizationTime.ToString("dd/MM/yyyy HH:mm"));
                table.Cell().Border(1).Padding(5).Text(evt.Location);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(evt.ExpectedStudents.ToString());
                table.Cell().Border(1).Padding(5).Text(evt.RegistrationUrl ?? "");
            });

            column.Item().PaddingTop(5).Text("3. Timeline chi tiết sự kiện:");
            column.Item().Border(1).Padding(5).Text(evt.Timeline).FontSize(10);
            
            column.Item().PaddingTop(5).Text("4. Khách mời/ Diễn giả sự kiện:");
            if (evt.Guests.Any())
            {
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Họ & tên khách mời/diễn giả").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Ngày tham gia").Bold();
                    });

                    int index = 1;
                    foreach (var guest in evt.Guests)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Border(1).Padding(5).Text(guest.FullName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(guest.ParticipationDate.ToString("dd/MM/yyyy"));
                        index++;
                    }
                });
            }
        });
    }

    private void ComposePlannedCompetition(IContainer container, PlannedCompetitionDto competition)
    {
        container.Column(column =>
        {
            column.Item().Text($"1. Tên cuộc thi: {competition.CompetitionName}");
            column.Item().Text($"2. Đơn vị có thẩm quyền tổ chức: {competition.AuthorizedUnit}");
            column.Item().Text($"3. Thời gian: {competition.CompetitionTime:dd/MM/yyyy}");
            column.Item().Text($"4. Địa điểm: {competition.Location}");
            column.Item().Text("5. Danh sách sinh viên tham gia dự thi");
            
            if (competition.Participants.Any())
            {
                column.Item().PaddingTop(3).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Họ & tên sinh viên").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("MSSV").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Email FPT").Bold();
                        header.Cell().Border(1).Padding(5).AlignCenter().Text("Số điện thoại").Bold();
                    });

                    int index = 1;
                    foreach (var participant in competition.Participants)
                    {
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                        table.Cell().Border(1).Padding(5).Text(participant.FullName);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(participant.StudentCode);
                        table.Cell().Border(1).Padding(5).Text(participant.Email);
                        table.Cell().Border(1).Padding(5).AlignCenter().Text(""); // Phone number not in DTO
                        index++;
                    }
                });
            }
        });
    }


    private void ComposeCommunicationPlan(IContainer container, List<CommunicationItemDto> items)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(4);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                {
                    col.Item().Text("Nội dung truyền thông").Bold();
                    col.Item().Text("(Bao gồm cả poster/hình ảnh và link đăng ký)").Bold().FontSize(9);
                });
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian truyền thông").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Phụ trách").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                {
                    col.Item().Text("Ghi chú").Bold();
                    col.Item().Text("(Tick vào nội dung cần gửi phòng IC-PDP hỗ trợ)").Bold().FontSize(9);
                });
            });

            // Add "Truyền thông trước sự kiện" row
            table.Cell().ColumnSpan(5).Border(1).Padding(5).Text("Truyền thông trước sự kiện").Bold().Italic();

            int index = 1;
            foreach (var item in items)
            {
                table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                table.Cell().Border(1).Padding(5).Text(item.Content);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Time.ToString("dd/MM/yyyy"));
                table.Cell().Border(1).Padding(5).Text(item.ResponsiblePerson);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.NeedSupport ? "✓" : "");
                index++;
            }
        });
    }

    private void ComposeBudget(IContainer container, BudgetDto budget)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(30);
                columns.RelativeColumn(3);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
            });

            table.Header(header =>
            {
                header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Hạng mục").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Số lượng").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Text("Đơn vị tính").Bold();
                header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                {
                    col.Item().Text("Đơn giá").Bold();
                    col.Item().Text("(Đã bao gồm VAT)").Bold().FontSize(9);
                });
                header.Cell().Border(1).Padding(5).AlignCenter().Column(col =>
                {
                    col.Item().Text("Thành tiền").Bold();
                    col.Item().Text("(Đã bao gồm VAT)").Bold().FontSize(9);
                });
            });

            // School Funding
            table.Cell().ColumnSpan(6).Border(1).Padding(5).Text("Chi phí hỗ trợ từ phía Nhà trường").Bold().Italic();
            
            int index = 1;
            foreach (var item in budget.SchoolFunding)
            {
                table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                table.Cell().Border(1).Padding(5).Text(item.Category);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Quantity.ToString());
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Unit);
                table.Cell().Border(1).Padding(5).AlignRight().Text(item.UnitPrice.ToString("N0"));
                table.Cell().Border(1).Padding(5).AlignRight().Text(item.TotalPrice.ToString("N0"));
                index++;
            }
            
            table.Cell().ColumnSpan(5).Border(1).Padding(5).AlignRight().Text("Tổng:").Bold();
            table.Cell().Border(1).Padding(5).AlignRight().Text($"{budget.SchoolTotal:N0} VNĐ").Bold();
            
            table.Cell().ColumnSpan(6).Border(1).Padding(5).Text($"Bằng chữ: {budget.SchoolTotalInWords}").Italic();

            // Club Funding
            table.Cell().ColumnSpan(6).Border(1).Padding(5).Text("Chi phí từ phía CLB").Bold().Italic();
            
            index = 1;
            foreach (var item in budget.ClubFunding)
            {
                table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                table.Cell().Border(1).Padding(5).Text(item.Category);
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Quantity.ToString());
                table.Cell().Border(1).Padding(5).AlignCenter().Text(item.Unit);
                table.Cell().Border(1).Padding(5).AlignRight().Text(item.UnitPrice.ToString("N0"));
                table.Cell().Border(1).Padding(5).AlignRight().Text(item.TotalPrice.ToString("N0"));
                index++;
            }
            
            table.Cell().ColumnSpan(5).Border(1).Padding(5).AlignRight().Text("Tổng:").Bold();
            table.Cell().Border(1).Padding(5).AlignRight().Text($"{budget.ClubTotal:N0} VNĐ").Bold();
            
            table.Cell().ColumnSpan(6).Border(1).Padding(5).Text($"Bằng chữ: {budget.ClubTotalInWords}").Italic();
        });
    }

    private void ComposeFacility(IContainer container, FacilityDto facility)
    {
        container.Column(column =>
        {
            if (facility.ElectionTime.HasValue)
            {
                column.Item().Text($"Thời gian bầu BCN nhiệm kỳ: {facility.ElectionTime:dd/MM/yyyy}");
            }
            
            column.Item().PaddingTop(3).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("STT").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Cơ sở vật chất").Bold();
                    header.Cell().Border(1).Padding(5).AlignCenter().Text("Thời gian sinh hoạt").Bold();
                });

                int index = 1;
                foreach (var item in facility.Items)
                {
                    table.Cell().Border(1).Padding(5).AlignCenter().Text(index.ToString());
                    table.Cell().Border(1).Padding(5).Text(item.FacilityName);
                    table.Cell().Border(1).Padding(5).Text(item.UsageTime.ToString("dd/MM/yyyy HH:mm"));
                    index++;
                }
            });
        });
    }


    private void ComposeResponsibilities(IContainer container, ClubResponsibilitiesDto responsibilities)
    {
        container.Column(column =>
        {
            // Display CustomText if available, otherwise show checkboxes
            if (!string.IsNullOrWhiteSpace(responsibilities.CustomText))
            {
                column.Item().Text(responsibilities.CustomText);
            }
            else
            {
                column.Item().Row(row =>
                {
                    row.AutoItem().Text(responsibilities.Planning ? "☑" : "☐");
                    row.AutoItem().PaddingLeft(5).Text("Lên kế hoạch");
                });
                
                column.Item().Row(row =>
                {
                    row.AutoItem().Text(responsibilities.Implementation ? "☑" : "☐");
                    row.AutoItem().PaddingLeft(5).Text("Triển khai kế hoạch");
                });
                
                column.Item().Row(row =>
                {
                    row.AutoItem().Text(responsibilities.StaffAssignment ? "☑" : "☐");
                    row.AutoItem().PaddingLeft(5).Text("Phân công nhân sự");
                });
                
                column.Item().Row(row =>
                {
                    row.AutoItem().Text(responsibilities.SecurityOrder ? "☑" : "☐");
                    row.AutoItem().PaddingLeft(5).Text("Đảm bảo an ninh trật tự");
                });
                
                column.Item().Row(row =>
                {
                    row.AutoItem().Text(responsibilities.HygieneAssetMaintenance ? "☑" : "☐");
                    row.AutoItem().PaddingLeft(5).Text("Giữ gìn vệ sinh và tài sản");
                });
            }
        });
    }

    private void ComposeFooter(IContainer container, FooterDto footer)
    {
        container.PaddingTop(30).Row(row =>
        {
            // Approver
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text("Người phê duyệt").Bold();
                column.Item().PaddingTop(60).AlignCenter().Text(footer.ApproverName ?? "").Bold();
            });
            
            // Reviewer (if exists)
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text("Người xem xét").Bold();
                column.Item().PaddingTop(60).AlignCenter().Text(footer.ReviewerName ?? "").Bold();
            });
            
            // Creator
            row.RelativeItem().Column(column =>
            {
                column.Item().AlignCenter().Text("Người lập bảng").Bold();
                column.Item().PaddingTop(60).AlignCenter().Text(footer.CreatorName).Bold();
            });
        });
    }
}
