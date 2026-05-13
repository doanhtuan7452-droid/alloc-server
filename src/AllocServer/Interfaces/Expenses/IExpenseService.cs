using AllocServer.DTOs.Expenses;
using AllocServer.Models;

namespace AllocServer.Interfaces.Expenses
{
    public interface IExpenseService
    {
        Task<PagedProjectExpensesResponse> GetProjectExpensesAsync(
            Project project,
            GetProjectExpensesQuery query);

        Task<ExpenseDetailResponse> CreateProjectExpenseAsync(
            int accountId,
            Project project,
            CreateExpenseRequest request);
    }
}
