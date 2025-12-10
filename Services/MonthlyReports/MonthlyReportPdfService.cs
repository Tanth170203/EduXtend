using BusinessObject.DTOs.MonthlyReport;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Repositories.MonthlyReports;

namespace Services.MonthlyReports;

public class MonthlyReportPdfService : IMonthlyReportPdfService
{
    private readonly IMonthlyReportRepository _reportRepo;
    private readonly IMonthlyReportDataAggregator _dataAggregator;
    private readonly EduXtendContext _context;

    public MonthlyReportPdfService(
        IMonthlyReportRepository reportRepo,
        IMonthlyReportDataAggregator dataAggregator,
        EduXtendContext context)
    {
        _reportRepo = reportRepo;
        _dataAggregator = dataAggregator;
        _context = context;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<byte[]> ExportToPdfAsync(int reportId)
    {
        var report = await BuildMonthlyReportDtoAsync(reportId);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(50);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Times New Roman"));

                page.Content().Element(c => ComposeDocument(c, report));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeDocument(IContainer container, MonthlyReportDto report)
    {
        container.Column(column =>
        {
            // Header
            ComposeHeader(column, report);

            // Part A: Current Month Activities
            ComposePartA(column, report);

            // Part B: Next Month Plans
            ComposePartB(column, report);

            // Signature Section
            ComposeSignature(column, report);
        });
    }


    private void ComposeHeader(ColumnDescriptor column, MonthlyReportDto report)
    {
        // Header table layout
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(45);
                columns.RelativeColumn(55);
            });

            // Row 1: PDP info and Report title
            table.Cell().Column(col =>
            {
                col.Item().AlignCenter().Text("PDP").Bold().FontSize(11);
                col.Item().AlignCenter().Text("PHÒNG HỢP TÁC QUỐC TẾ").Bold().FontSize(11);
                col.Item().AlignCenter().Text("& PHÁT TRIỂN CÁ NHÂN").Bold().FontSize(11);
            });

            table.Cell().Column(col =>
            {
                col.Item().AlignCenter().Text($"BÁO CÁO HOẠT ĐỘNG THÁNG {report.ReportMonth} VÀ KẾ HOẠCH HOẠT ĐỘNG THÁNG {report.NextMonth}")
                    .Bold().FontSize(12);
                col.Item().PaddingTop(5).AlignCenter()
                    .Text($"{report.Header.Location}, Ngày {report.Header.ReportDate.Day} tháng {report.Header.ReportDate.Month} năm {report.Header.ReportDate.Year}")
                    .Italic().FontSize(11);
            });

            // Row 2: Club name
            table.Cell().PaddingTop(10).AlignCenter().Text(report.Header.ClubName).Bold().FontSize(12);
            table.Cell();
        });

        // Creator info
        column.Item().PaddingTop(15).Column(col =>
        {
            col.Item().Text(text =>
            {
                text.Span("Họ và tên: ").Bold();
                text.Span(report.Footer.CreatorName);
            });
            col.Item().Text(text =>
            {
                text.Span("Chức vụ: ").Bold();
                text.Span(report.Footer.CreatorPosition);
            });
            col.Item().PaddingTop(5).Text($"Hôm nay, ngày {report.Header.ReportDate.Day} tháng {report.Header.ReportDate.Month} năm {report.Header.ReportDate.Year}, tôi đại diện CLB {report.Header.ClubName} xin báo cáo với phòng ICPDP (Hợp tác quốc tế & Phát triển cá nhân) như sau:");
        });
    }

    private void ComposePartA(ColumnDescriptor column, MonthlyReportDto report)
    {
        column.Item().PaddingTop(15).Text($"A. HOẠT ĐỘNG THÁNG {report.ReportMonth}").Bold().FontSize(12);

        // I. School Events
        column.Item().PaddingTop(10).Text("I. Sự kiện cấp trường").Bold();

        if (report.CurrentMonthActivities.SchoolEvents.Any())
        {
            int eventIndex = 1;
            foreach (var evt in report.CurrentMonthActivities.SchoolEvents)
            {
                ComposeSchoolEvent(column, evt, eventIndex);
                eventIndex++;
            }
        }
        else
        {
            column.Item().PaddingTop(3).Text("Không có sự kiện cấp trường.").Italic();
        }

        // II. Support Activities
        column.Item().PaddingTop(10).Text("II. Các hoạt động hỗ trợ các phòng ban khác/bộ môn").Bold();
        ComposeSupportActivities(column, report.CurrentMonthActivities.SupportActivities);

        // III. Competitions
        column.Item().PaddingTop(10).Text("III. Cuộc thi cấp thành phố/ cấp vùng miền/ cấp quốc gia").Bold();
        ComposeCompetitions(column, report.CurrentMonthActivities.Competitions);

        // IV. Internal Meetings
        column.Item().PaddingTop(10).Text($"IV. Sinh hoạt nội bộ tháng {report.ReportMonth}").Bold();
        ComposeInternalMeetings(column, report.CurrentMonthActivities.InternalMeetings);
    }

    private void ComposeSchoolEvent(ColumnDescriptor column, SchoolEventDto evt, int eventIndex)
    {
        column.Item().PaddingTop(8).Text($"{eventIndex}. Tên sự kiện: {evt.EventName}").Bold();
        column.Item().Text($"1. Thời gian diễn ra sự kiện: {evt.EventDate:dd/MM/yyyy HH:mm}");
        column.Item().Text($"2. Số lượng người tham gia sự kiện: {evt.ActualParticipants}");

        // Participants table
        if (evt.Participants.Any())
        {
            column.Item().PaddingTop(5).AlignCenter().Text("BẢNG THỐNG KÊ SỐ LƯỢNG NGƯỜI THAM DỰ CHƯƠNG TRÌNH").Bold();
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
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ & tên\nMSSV").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("SĐT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Điểm đánh giá\n(thang 3-5 điểm)").Bold().FontSize(10);
                });

                for (int i = 0; i < evt.Participants.Count; i++)
                {
                    var p = evt.Participants[i];
                    table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text($"{p.FullName}\n{p.StudentCode}").FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(p.PhoneNumber).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(p.Rating?.ToString("0.0") ?? "").FontSize(10);
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
                    for (int i = 0; i < 10; i++) columns.RelativeColumn(1);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Số lượng dự kiến (A)").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Số lượng thực tế (B)").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Nguyên nhân nếu B < A").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Truyền thông (thang 10)").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Tổ chức (thang 10)").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("MC/ Host").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Diễn giả/ Biểu diễn").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Thành công").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Hạn chế").Bold().FontSize(8);
                    header.Cell().Border(1).Padding(2).AlignCenter().Text("Biện pháp").Bold().FontSize(8);
                });

                table.Cell().Border(1).Padding(2).AlignCenter().Text(evt.Evaluation.ExpectedCount.ToString()).FontSize(8);
                table.Cell().Border(1).Padding(2).AlignCenter().Text(evt.Evaluation.ActualCount.ToString()).FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.ReasonIfLess ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).AlignCenter().Text(evt.Evaluation.CommunicationScore?.ToString("0.0") ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).AlignCenter().Text(evt.Evaluation.OrganizationScore?.ToString("0.0") ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.McHostEvaluation ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.SpeakerPerformerEvaluation ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.Achievements ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.Limitations ?? "").FontSize(8);
                table.Cell().Border(1).Padding(2).Text(evt.Evaluation.ProposedSolutions ?? "").FontSize(8);
            });
        }

        // Support Members
        column.Item().PaddingTop(5).Text("4. Tình hình hoạt động của CLB trước, trong và sau sự kiện");
        column.Item().PaddingLeft(15).Text("a. Danh sách các thành viên tham gia hỗ trợ sự kiện");

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
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ & tên").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("MSSV").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("SĐT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Vị trí công việc").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Điểm đánh giá\n(thang 5-10 điểm)").Bold().FontSize(10);
                });

                for (int i = 0; i < evt.SupportMembers.Count; i++)
                {
                    var m = evt.SupportMembers[i];
                    table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(m.FullName).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(m.StudentCode).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(m.PhoneNumber).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(m.Position).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(m.Rating?.ToString("0.0") ?? "").FontSize(10);
                }
            });
        }

        column.Item().PaddingTop(5).Text($"5. Feedback sau sự kiện: {(string.IsNullOrEmpty(evt.FeedbackUrl) ? "(Đính kèm link feedback)" : evt.FeedbackUrl)}");
        column.Item().Text($"6. Hình ảnh, video của sự kiện: {(string.IsNullOrEmpty(evt.MediaUrls) ? "(Đính kèm link drive)" : evt.MediaUrls)}");
    }


    private void ComposeSupportActivities(ColumnDescriptor column, List<SupportActivityDto> activities)
    {
        column.Item().Text("1. Tên sự kiện");

        if (activities.Any())
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
                    columns.RelativeColumn(2);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Nội dung sự kiện").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Phòng ban/bộ môn").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Địa điểm").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Hình ảnh hoạt động\n(Link hình ảnh)").Bold().FontSize(10);
                });

                for (int i = 0; i < activities.Count; i++)
                {
                    var sa = activities[i];
                    table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(sa.EventContent).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(sa.DepartmentName).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(sa.EventTime.ToString("dd/MM/yyyy")).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(sa.Location).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(sa.ImageUrl ?? "").FontSize(10);
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
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ và tên").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Mã số sinh viên").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Tên sự kiện").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian").Bold().FontSize(10);
                    header.Cell().Border(1).Padding(3).AlignCenter().Text("Điểm đánh giá").Bold().FontSize(10);
                });

                int supCount = 1;
                foreach (var sa in activities)
                {
                    foreach (var s in sa.SupportStudents)
                    {
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(supCount.ToString()).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(s.FullName).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(s.StudentCode).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(s.EventName).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(s.EventTime.ToString("dd/MM/yyyy")).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(s.Rating?.ToString() ?? "").FontSize(10);
                        supCount++;
                    }
                }
            });
        }
        else
        {
            column.Item().PaddingTop(3).Text("Không có hoạt động hỗ trợ.").Italic();
        }
    }

    private void ComposeCompetitions(ColumnDescriptor column, List<CompetitionDto> competitions)
    {
        if (competitions.Any())
        {
            foreach (var comp in competitions)
            {
                column.Item().PaddingTop(5).Text($"1. Tên cuộc thi: {comp.CompetitionName}");
                column.Item().Text($"2. Đơn vị có thẩm quyền tổ chức: {comp.OrganizingUnit}");
                column.Item().Text("3. Danh sách sinh viên tham gia");

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
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ và tên").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Mã số sinh viên").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Email").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Thành tích").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Ghi chú").Bold().FontSize(10);
                    });

                    for (int i = 0; i < comp.Participants.Count; i++)
                    {
                        var p = comp.Participants[i];
                        table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.FullName).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(p.StudentCode).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.Email).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.Achievement ?? "").FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.Note ?? "").FontSize(10);
                    }
                });
            }
        }
        else
        {
            column.Item().Text("1. Tên cuộc thi:");
            column.Item().Text("2. Đơn vị có thẩm quyền tổ chức:");
            column.Item().Text("3. Danh sách sinh viên tham gia: (Trống)");
        }
    }

    private void ComposeInternalMeetings(ColumnDescriptor column, List<InternalMeetingDto> meetings)
    {
        column.Item().PaddingTop(3).Table(table =>
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
                header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Địa điểm").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Số người tham gia").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Nội dung sinh hoạt chuyên môn").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Hình ảnh (đính kèm link)").Bold().FontSize(10);
            });

            if (meetings.Any())
            {
                for (int i = 0; i < meetings.Count; i++)
                {
                    var m = meetings[i];
                    table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(m.MeetingTime.ToString("dd/MM/yyyy")).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(m.Location).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(m.ParticipantCount.ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(m.Content).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(m.ImageUrl ?? "").FontSize(10);
                }
            }
            else
            {
                table.Cell().ColumnSpan(6).Border(1).Padding(3).AlignCenter().Text("Không có dữ liệu").FontSize(10);
            }
        });
    }


    private void ComposePartB(ColumnDescriptor column, MonthlyReportDto report)
    {
        column.Item().PaddingTop(15).Text($"B. KẾ HOẠCH THÁNG {report.NextMonth}").Bold().FontSize(12);

        // I. Purpose and Significance
        column.Item().PaddingTop(10).Text("I. Mục đích và ý nghĩa:").Bold();
        column.Item().Text(report.NextMonthPlans.Purpose.Purpose ?? "");
        column.Item().Text(report.NextMonthPlans.Purpose.Significance ?? "");

        // II. School Events
        column.Item().PaddingTop(10).Text("II. Sự kiện cấp trường").Bold();
        ComposePlannedEvents(column, report.NextMonthPlans.PlannedEvents);

        // III. Competitions
        column.Item().PaddingTop(10).Text("III. Cuộc thi cấp thành phố/ vùng miền/ quốc gia").Bold();
        ComposePlannedCompetitions(column, report.NextMonthPlans.PlannedCompetitions);

        // IV. Communication Plan
        column.Item().PaddingTop(10).Text("IV. Kế hoạch truyền thông").Bold();
        ComposeCommunicationPlan(column, report.NextMonthPlans.CommunicationPlan);

        // V. Budget
        column.Item().PaddingTop(10).Text("V. Hỗ trợ kinh phí").Bold();
        ComposeBudget(column, report.NextMonthPlans.Budget);

        // VI. Facility
        column.Item().PaddingTop(10).Text("VI. Kế hoạch sử dụng cơ sở vật chất cho sự kiện/ sinh hoạt nội bộ").Bold();
        ComposeFacility(column, report.NextMonthPlans.Facility);

        // VII. Responsibilities
        column.Item().PaddingTop(10).Text("VII. Trách nhiệm của CLB:").Bold();
        column.Item().Text(report.NextMonthPlans.Responsibilities.CustomText ?? "");
    }

    private void ComposePlannedEvents(ColumnDescriptor column, List<PlannedEventDto> events)
    {
        if (events.Any())
        {
            int planIndex = 1;
            foreach (var evt in events)
            {
                column.Item().PaddingTop(5).Text($"{planIndex}. Tên sự kiện: {evt.EventName}");
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
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian tổ chức").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Địa điểm tổ chức").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Số lượng SV dự kiến").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Link đăng ký (nếu có)").Bold().FontSize(10);
                    });

                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.OrganizationTime.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(evt.Location).FontSize(10);
                    table.Cell().Border(1).Padding(3).AlignCenter().Text(evt.ExpectedStudents.ToString()).FontSize(10);
                    table.Cell().Border(1).Padding(3).Text(evt.RegistrationUrl ?? "").FontSize(10);
                });

                column.Item().PaddingTop(5).Text("3. Timeline chi tiết sự kiện:");
                column.Item().Border(1).Padding(5).Text(evt.Timeline ?? "").FontSize(10);

                column.Item().PaddingTop(5).Text("4. Khách mời/ Diễn giả sự kiện:");
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
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ & tên khách mời/diễn giả").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Ngày tham gia").Bold().FontSize(10);
                    });

                    for (int i = 0; i < evt.Guests.Count; i++)
                    {
                        var g = evt.Guests[i];
                        table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(g.FullName).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(g.ParticipationDate.ToString("dd/MM/yyyy")).FontSize(10);
                    }
                });

                planIndex++;
            }
        }
        else
        {
            column.Item().Text("1. Tên sự kiện: (Chưa có)");
        }
    }

    private void ComposePlannedCompetitions(ColumnDescriptor column, List<PlannedCompetitionDto> competitions)
    {
        if (competitions.Any())
        {
            foreach (var comp in competitions)
            {
                column.Item().PaddingTop(5).Text($"1. Tên cuộc thi: {comp.CompetitionName}");
                column.Item().Text($"2. Đơn vị có thẩm quyền tổ chức: {comp.AuthorizedUnit}");
                column.Item().Text($"3. Thời gian: {comp.CompetitionTime:dd/MM/yyyy}");
                column.Item().Text($"4. Địa điểm: {comp.Location}");
                column.Item().Text("5. Danh sách sinh viên tham gia dự thi");

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
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Họ & tên sinh viên").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("MSSV").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Email FPT").Bold().FontSize(10);
                        header.Cell().Border(1).Padding(3).AlignCenter().Text("Số điện thoại").Bold().FontSize(10);
                    });

                    for (int i = 0; i < comp.Participants.Count; i++)
                    {
                        var p = comp.Participants[i];
                        table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.FullName).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text(p.StudentCode).FontSize(10);
                        table.Cell().Border(1).Padding(3).Text(p.Email).FontSize(10);
                        table.Cell().Border(1).Padding(3).AlignCenter().Text("").FontSize(10);
                    }
                });
            }
        }
        else
        {
            column.Item().Text("1. Tên cuộc thi: (Chưa có)");
        }
    }

    private void ComposeCommunicationPlan(ColumnDescriptor column, List<CommunicationItemDto> items)
    {
        column.Item().AlignCenter().Text("BẢNG KẾ HOẠCH TRUYỀN THÔNG").Bold();
        column.Item().PaddingTop(3).Table(table =>
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
                header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Nội dung truyền thông\n(Bao gồm cả poster/hình ảnh và link đăng ký)").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian truyền thông").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Phụ trách").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Ghi chú\n(Tick vào nội dung cần gửi phòng IC-PDP hỗ trợ)").Bold().FontSize(10);
            });

            // Header row for "Truyền thông trước sự kiện"
            table.Cell().ColumnSpan(5).Border(1).Padding(3).Text("Truyền thông trước sự kiện").Bold().Italic().FontSize(10);

            int cIndex = 1;
            foreach (var com in items)
            {
                table.Cell().Border(1).Padding(3).AlignCenter().Text(cIndex.ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(com.Content).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(com.Time.ToString("dd/MM/yyyy")).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(com.ResponsiblePerson).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(com.NeedSupport ? "✓" : "").FontSize(10);
                cIndex++;
            }
        });
    }


    private void ComposeBudget(ColumnDescriptor column, BudgetDto budget)
    {
        column.Item().PaddingTop(3).Table(table =>
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
                header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Hạng mục").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Số lượng").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Đơn vị tính").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Đơn giá\n(Đã bao gồm VAT)").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Thành tiền\n(Đã bao gồm VAT)").Bold().FontSize(10);
            });

            // School Funding
            table.Cell().ColumnSpan(6).Border(1).Padding(3).Text("Chi phí hỗ trợ từ phía Nhà trường").Bold().Italic().FontSize(10);

            for (int i = 0; i < budget.SchoolFunding.Count; i++)
            {
                var item = budget.SchoolFunding[i];
                table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(item.Category).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(item.Quantity.ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(item.Unit).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignRight().Text(item.TotalPrice.ToString("N0")).FontSize(10);
            }

            table.Cell().ColumnSpan(5).Border(1).Padding(3).AlignRight().Text("Tổng:").Bold().FontSize(10);
            table.Cell().Border(1).Padding(3).AlignRight().Text($"{budget.SchoolTotal:N0} VNĐ").Bold().FontSize(10);

            table.Cell().ColumnSpan(6).Border(1).Padding(3).Text($"Bằng chữ: {budget.SchoolTotalInWords}").Italic().FontSize(10);

            // Club Funding
            table.Cell().ColumnSpan(6).Border(1).Padding(3).Text("Chi phí từ phía CLB").Bold().Italic().FontSize(10);

            for (int i = 0; i < budget.ClubFunding.Count; i++)
            {
                var item = budget.ClubFunding[i];
                table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(item.Category).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(item.Quantity.ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignCenter().Text(item.Unit).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N0")).FontSize(10);
                table.Cell().Border(1).Padding(3).AlignRight().Text(item.TotalPrice.ToString("N0")).FontSize(10);
            }

            table.Cell().ColumnSpan(5).Border(1).Padding(3).AlignRight().Text("Tổng:").Bold().FontSize(10);
            table.Cell().Border(1).Padding(3).AlignRight().Text($"{budget.ClubTotal:N0} VNĐ").Bold().FontSize(10);

            table.Cell().ColumnSpan(6).Border(1).Padding(3).Text($"Bằng chữ: {budget.ClubTotalInWords}").Italic().FontSize(10);
        });
    }

    private void ComposeFacility(ColumnDescriptor column, FacilityDto facility)
    {
        if (facility.ElectionTime.HasValue)
        {
            column.Item().Text($"Thời gian bầu BCN nhiệm kỳ: {facility.ElectionTime.Value:dd/MM/yyyy}");
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
                header.Cell().Border(1).Padding(3).AlignCenter().Text("STT").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Cơ sở vật chất").Bold().FontSize(10);
                header.Cell().Border(1).Padding(3).AlignCenter().Text("Thời gian sinh hoạt").Bold().FontSize(10);
            });

            for (int i = 0; i < facility.Items.Count; i++)
            {
                var f = facility.Items[i];
                table.Cell().Border(1).Padding(3).AlignCenter().Text((i + 1).ToString()).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(f.FacilityName).FontSize(10);
                table.Cell().Border(1).Padding(3).Text(f.UsageTime.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
            }
        });
    }

    private void ComposeSignature(ColumnDescriptor column, MonthlyReportDto report)
    {
        column.Item().PaddingTop(30).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
                columns.RelativeColumn(1);
            });

            // Titles
            table.Cell().AlignCenter().Text("Người phê duyệt").Bold();
            table.Cell().AlignCenter().Text("Người xem xét").Bold();
            table.Cell().AlignCenter().Text("Người lập bảng").Bold();

            // Spacing for signature
            table.Cell().PaddingTop(60).AlignCenter().Text(report.Footer.ApproverName ?? "").Bold();
            table.Cell().PaddingTop(60).AlignCenter().Text(report.Footer.ReviewerName ?? "").Bold();
            table.Cell().PaddingTop(60).AlignCenter().Text(report.Footer.CreatorName).Bold();
        });
    }

    private async Task<MonthlyReportDto> BuildMonthlyReportDtoAsync(int reportId)
    {
        var plan = await _reportRepo.GetByIdAsync(reportId);
        if (plan == null)
        {
            throw new InvalidOperationException($"Monthly report with ID {reportId} not found");
        }

        if (plan.ReportMonth == null || plan.ReportYear == null)
        {
            throw new InvalidOperationException("Invalid monthly report: missing month or year");
        }

        int reportMonth = plan.ReportMonth.Value;
        int reportYear = plan.ReportYear.Value;

        int nextMonth = reportMonth == 12 ? 1 : reportMonth + 1;
        int nextYear = reportMonth == 12 ? reportYear + 1 : reportYear;

        var club = await _context.Clubs
            .AsNoTracking()
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == plan.ClubId);

        var clubManager = await _context.ClubMembers
            .AsNoTracking()
            .Include(cm => cm.Student)
                .ThenInclude(s => s.User)
            .Where(cm => cm.ClubId == plan.ClubId && cm.RoleInClub == "Manager")
            .Select(cm => cm.Student.User)
            .FirstOrDefaultAsync();

        var dto = new MonthlyReportDto
        {
            Id = plan.Id,
            ClubId = plan.ClubId,
            ClubName = club?.Name ?? "",
            DepartmentName = club?.Category?.Name ?? "",
            Status = plan.Status,
            ReportMonth = reportMonth,
            ReportYear = reportYear,
            NextMonth = nextMonth,
            NextYear = nextYear,
            CreatedAt = plan.CreatedAt,
            SubmittedAt = plan.SubmittedAt,
            ApprovedAt = plan.ApprovedAt,
            RejectionReason = plan.RejectionReason
        };

        dto.Header = new HeaderDto
        {
            DepartmentName = club?.Category?.Name ?? "",
            MainTitle = $"BÁO CÁO HOẠT ĐỘNG THÁNG {reportMonth}",
            SubTitle = $"VÀ KẾ HOẠCH THÁNG {nextMonth}",
            ClubName = club?.Name ?? "",
            Location = "FPT University HCM",
            ReportDate = DateTime.Now,
            CreatorName = clubManager?.FullName ?? "",
            CreatorPosition = "Quản lý CLB"
        };

        dto.Footer = new FooterDto
        {
            CreatorName = clubManager?.FullName ?? "",
            CreatorPosition = "Quản lý CLB"
        };

        if (plan.ApprovedBy != null)
        {
            var approver = await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Id == plan.ApprovedById);

            if (approver != null)
            {
                dto.Footer.ApproverName = approver.FullName;
                dto.Footer.ApproverPosition = approver.Role?.RoleName ?? "Admin";
            }
        }

        dto.CurrentMonthActivities = new CurrentMonthActivitiesDto
        {
            SchoolEvents = await _dataAggregator.GetSchoolEventsAsync(plan.ClubId, reportMonth, reportYear),
            SupportActivities = await _dataAggregator.GetSupportActivitiesAsync(plan.ClubId, reportMonth, reportYear),
            Competitions = await _dataAggregator.GetCompetitionsAsync(plan.ClubId, reportMonth, reportYear),
            InternalMeetings = await _dataAggregator.GetInternalMeetingsAsync(plan.ClubId, reportMonth, reportYear)
        };

        dto.NextMonthPlans = await _dataAggregator.GetNextMonthPlansAsync(plan.ClubId, reportMonth, reportYear, nextMonth, nextYear);

        return dto;
    }
}
