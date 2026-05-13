using AllocServer.DTOs.Timesheets;

namespace AllocServer.Interfaces.Timesheets
{
    public interface ITimesheetService
    {
        Task<PagedTimesheetsResponse> GetTimesheetsAsync(
            int accountId,
            GetTimesheetsQuery query);

        Task<(TimesheetDetailResponse Timesheet, bool Created)> UpsertTimesheetAsync(
            int accountId,
            CreateTimesheetRequest request);
    }
}
