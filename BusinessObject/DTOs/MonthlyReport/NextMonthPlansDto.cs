namespace BusinessObject.DTOs.MonthlyReport;

public class NextMonthPlansDto
{
    // I. Purpose and Significance
    public PurposeDto Purpose { get; set; } = new();
    
    // II. School Events
    public List<PlannedEventDto> PlannedEvents { get; set; } = new();
    
    // III. Competitions
    public List<PlannedCompetitionDto> PlannedCompetitions { get; set; } = new();
    
    // IV. Communication Plan
    public List<CommunicationItemDto> CommunicationPlan { get; set; } = new();
    
    // V. Budget Support
    public BudgetDto Budget { get; set; } = new();
    
    // VI. Facility Usage
    public FacilityDto Facility { get; set; } = new();
    
    // VII. Club Responsibilities
    public ClubResponsibilitiesDto Responsibilities { get; set; } = new();
}
