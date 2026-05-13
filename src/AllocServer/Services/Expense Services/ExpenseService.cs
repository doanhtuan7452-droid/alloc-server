using AllocServer.Data;
using AllocServer.DTOs.Expenses;
using AllocServer.Interfaces.Expenses;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Expense_Services
{
    public class ExpenseService : IExpenseService
    {
        private const decimal MinMoneyAmount = 0.01m;
        private const decimal MaxMoneyAmount = 9999999999999999.99m;

        private readonly ApplicationDbContext _context;

        public ExpenseService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedProjectExpensesResponse> GetProjectExpensesAsync(
            Project project,
            GetProjectExpensesQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var category = NormalizeOptionalString(query.Category);
            var search = NormalizeOptionalString(query.Search);

            ValidateDateRange(query.FromDate, query.ToDate, "fromDate", "toDate");
            ValidateAmountRange(query.MinAmount, query.MaxAmount);

            var expensesQuery = _context.Expenses
                .AsNoTracking()
                .Where(item => item.ProjectID == project.ProjectID);

            if (category != null)
            {
                expensesQuery = expensesQuery.Where(item => item.Category == category);
            }

            if (query.FromDate != null)
            {
                expensesQuery = expensesQuery.Where(item => item.ExpenseDate >= query.FromDate.Value);
            }

            if (query.ToDate != null)
            {
                expensesQuery = expensesQuery.Where(item => item.ExpenseDate <= query.ToDate.Value);
            }

            if (query.MinAmount != null)
            {
                expensesQuery = expensesQuery.Where(item => item.Amount >= query.MinAmount.Value);
            }

            if (query.MaxAmount != null)
            {
                expensesQuery = expensesQuery.Where(item => item.Amount <= query.MaxAmount.Value);
            }

            if (search != null)
            {
                expensesQuery = expensesQuery.Where(item =>
                    item.Description != null
                    && item.Description.Contains(search));
            }

            var totalItems = await expensesQuery.CountAsync();
            var items = await expensesQuery
                .OrderByDescending(item => item.ExpenseDate)
                .ThenByDescending(item => item.ExpenseID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new ExpenseListItemResponse
                {
                    ExpenseId = item.ExpenseID,
                    ProjectId = item.ProjectID,
                    ProjectName = project.ProjectName,
                    Category = item.Category,
                    Amount = item.Amount,
                    ExpenseDate = item.ExpenseDate,
                    Description = item.Description
                })
                .ToListAsync();

            return new PagedProjectExpensesResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        public async Task<ExpenseDetailResponse> CreateProjectExpenseAsync(
            int accountId,
            Project project,
            CreateExpenseRequest request)
        {
            var category = NormalizeOptionalString(request.Category);
            if (category == null)
            {
                throw new ArgumentException("Category la bat buoc.");
            }

            if (category.Length > 100)
            {
                throw new ArgumentException("Category toi da 100 ky tu.");
            }

            if (request.Amount < MinMoneyAmount || request.Amount > MaxMoneyAmount)
            {
                throw new ArgumentException("Amount phai tu 0.01 den 9999999999999999.99.");
            }

            if (request.ExpenseDate == null)
            {
                throw new ArgumentException("ExpenseDate la bat buoc.");
            }

            ValidateExpenseDate(project, request.ExpenseDate.Value);

            var expense = new Expense
            {
                ProjectID = project.ProjectID,
                Category = category,
                Amount = request.Amount,
                ExpenseDate = request.ExpenseDate.Value,
                Description = NormalizeNullableText(request.Description)
            };

            _context.Expenses.Add(expense);
            await _context.SaveChangesAsync();

            return MapExpense(expense, project.ProjectName);
        }

        private static void ValidateExpenseDate(Project project, DateOnly expenseDate)
        {
            if (expenseDate < project.StartDate)
            {
                throw new ArgumentException("ExpenseDate khong duoc truoc ngay bat dau du an.");
            }

            if (expenseDate > project.EndDate)
            {
                throw new ArgumentException("ExpenseDate khong duoc sau ngay ket thuc du an.");
            }
        }

        private static void ValidateDateRange(
            DateOnly? fromDate,
            DateOnly? toDate,
            string fromFieldName,
            string toFieldName)
        {
            if (fromDate != null && toDate != null && toDate < fromDate)
            {
                throw new ArgumentException($"{toFieldName} phai lon hon hoac bang {fromFieldName}.");
            }
        }

        private static void ValidateAmountRange(decimal? minAmount, decimal? maxAmount)
        {
            if (minAmount is < 0 || maxAmount is < 0)
            {
                throw new ArgumentException("Amount filter khong duoc am.");
            }

            if (minAmount > MaxMoneyAmount || maxAmount > MaxMoneyAmount)
            {
                throw new ArgumentException("Amount filter khong duoc vuot qua 9999999999999999.99.");
            }

            if (minAmount != null && maxAmount != null && maxAmount < minAmount)
            {
                throw new ArgumentException("maxAmount phai lon hon hoac bang minAmount.");
            }
        }

        private static ExpenseDetailResponse MapExpense(Expense expense, string projectName)
        {
            return new ExpenseDetailResponse
            {
                ExpenseId = expense.ExpenseID,
                ProjectId = expense.ProjectID,
                ProjectName = projectName,
                Category = expense.Category,
                Amount = expense.Amount,
                ExpenseDate = expense.ExpenseDate,
                Description = expense.Description
            };
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
