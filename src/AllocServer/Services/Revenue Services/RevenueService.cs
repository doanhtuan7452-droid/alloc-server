using AllocServer.Data;
using AllocServer.DTOs.Revenues;
using AllocServer.Interfaces.Revenues;
using AllocServer.Models;
using Microsoft.EntityFrameworkCore;

namespace AllocServer.Services.Revenue_Services
{
    public class RevenueService : IRevenueService
    {
        private const decimal MaxMoneyAmount = 9999999999999999.99m;

        private static readonly HashSet<string> AllowedRevenueTypes = new(StringComparer.Ordinal)
        {
            "Fixed Price",
            "Time & Material",
            "Milestone"
        };

        private static readonly HashSet<string> AllowedRevenueStatuses = new(StringComparer.Ordinal)
        {
            "Pending",
            "Received"
        };

        private readonly ApplicationDbContext _context;

        public RevenueService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PagedProjectRevenuesResponse> GetProjectRevenuesAsync(
            Project project,
            GetProjectRevenuesQuery query)
        {
            var page = Math.Max(query.Page, 1);
            var pageSize = Math.Clamp(query.PageSize, 1, 100);
            var revenueType = NormalizeRevenueType(query.Type);
            var status = NormalizeRevenueStatus(query.Status);

            if (!string.IsNullOrWhiteSpace(query.Type) && revenueType == null)
            {
                throw new ArgumentException("InvalidRevenueType");
            }

            if (!string.IsNullOrWhiteSpace(query.Status) && status == null)
            {
                throw new ArgumentException("InvalidRevenueStatus");
            }

            ValidateDateRange(query.ExpectedFromDate, query.ExpectedToDate);
            ValidateAmountRange(query.MinAmount, query.MaxAmount);

            var revenuesQuery = _context.Revenues
                .AsNoTracking()
                .Where(item => item.ProjectID == project.ProjectID);

            if (revenueType != null)
            {
                revenuesQuery = revenuesQuery.Where(item => item.RevenueType == revenueType);
            }

            if (status != null)
            {
                revenuesQuery = revenuesQuery.Where(item => item.Status == status);
            }

            if (query.ExpectedFromDate != null)
            {
                revenuesQuery = revenuesQuery.Where(item =>
                    item.ExpectedDate != null
                    && item.ExpectedDate >= query.ExpectedFromDate.Value);
            }

            if (query.ExpectedToDate != null)
            {
                revenuesQuery = revenuesQuery.Where(item =>
                    item.ExpectedDate != null
                    && item.ExpectedDate <= query.ExpectedToDate.Value);
            }

            if (query.MinAmount != null)
            {
                revenuesQuery = revenuesQuery.Where(item => item.Amount >= query.MinAmount.Value);
            }

            if (query.MaxAmount != null)
            {
                revenuesQuery = revenuesQuery.Where(item => item.Amount <= query.MaxAmount.Value);
            }

            var totalItems = await revenuesQuery.CountAsync();
            var items = await revenuesQuery
                .OrderByDescending(item => item.ExpectedDate ?? DateOnly.MinValue)
                .ThenByDescending(item => item.RevenueID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(item => new RevenueListItemResponse
                {
                    RevenueId = item.RevenueID,
                    ProjectId = item.ProjectID,
                    ProjectName = project.ProjectName,
                    Type = item.RevenueType,
                    Amount = item.Amount,
                    ExpectedDate = item.ExpectedDate,
                    Status = item.Status
                })
                .ToListAsync();

            return new PagedProjectRevenuesResponse
            {
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize),
                Items = items
            };
        }

        private static void ValidateDateRange(DateOnly? expectedFromDate, DateOnly? expectedToDate)
        {
            if (expectedFromDate != null
                && expectedToDate != null
                && expectedToDate < expectedFromDate)
            {
                throw new ArgumentException("InvalidExpectedDateRange");
            }
        }

        private static void ValidateAmountRange(decimal? minAmount, decimal? maxAmount)
        {
            if (minAmount is < 0 || maxAmount is < 0)
            {
                throw new ArgumentException("AmountFilterNegative");
            }

            if (minAmount > MaxMoneyAmount || maxAmount > MaxMoneyAmount)
            {
                throw new ArgumentException("AmountFilterExceedsMax");
            }

            if (minAmount != null && maxAmount != null && maxAmount < minAmount)
            {
                throw new ArgumentException("InvalidAmountRange");
            }
        }

        private static string? NormalizeRevenueType(string? type)
        {
            var normalized = NormalizeOptionalString(type);
            if (normalized == null)
                return null;

            var compact = normalized
                .Replace(" ", string.Empty)
                .Replace("&", "AND")
                .ToUpperInvariant();

            var candidate = compact switch
            {
                "FIXEDPRICE" => "Fixed Price",
                "TIMEANDMATERIAL" => "Time & Material",
                "MILESTONE" => "Milestone",
                _ => normalized
            };

            return AllowedRevenueTypes.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeRevenueStatus(string? status)
        {
            var normalized = NormalizeOptionalString(status);
            if (normalized == null)
                return null;

            var candidate = normalized.ToUpperInvariant() switch
            {
                "PENDING" => "Pending",
                "RECEIVED" => "Received",
                _ => normalized
            };

            return AllowedRevenueStatuses.Contains(candidate) ? candidate : null;
        }

        private static string? NormalizeOptionalString(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }
    }
}
