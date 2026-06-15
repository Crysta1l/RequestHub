using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RequestHub.Data;
using System.Reflection.Metadata;
using System.Security.Claims;

namespace RequestHub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PdfController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PdfController(AppDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet("request/{id}")]
        public async Task<IActionResult> ExportRequestPdf(int id)
        {
            var userId = GetCurrentUserId();
            var request = await _context.AccessRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null) return NotFound();

            // Get history
            var history = await _context.RequestHistories
                .Where(h => h.RequestId == id)
                .OrderBy(h => h.PerformedAt)
                .ToListAsync();

            // Set QuestPDF license
            QuestPDF.Settings.License = LicenseType.Community;

            var pdf = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Text($"Заявка на доступ №{request.Id}")
                        .SemiBold().FontSize(20).AlignCenter();

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Text($"Дата создания: {request.CreatedAt:dd.MM.yyyy HH:mm}");
                        col.Item().Text($"Статус: {request.Status}");
                        col.Item().Text($"Приоритет: {request.Priority}");

                        col.Item().LineHorizontal(1);

                        col.Item().Text("Детали заявки").SemiBold().FontSize(14);
                        col.Item().Text($"Название: {request.Title}");
                        col.Item().Text($"Ресурс: {request.Resource}");
                        col.Item().Text($"Тип доступа: {request.AccessType}");
                        col.Item().Text($"Подразделение: {request.Department ?? "—"}");
                        col.Item().Text($"Обоснование: {request.Justification}");
                        col.Item().Text($"Основание: {request.Basis ?? "—"}");
                        col.Item().Text($"Срок действия: {(request.ExpiryDate.HasValue ? request.ExpiryDate.Value.ToString("dd.MM.yyyy") : "—")}");
                        col.Item().Text($"Ознакомлен: {(request.IsAcknowledged ? "Да" : "Нет")}");

                        if (history.Any())
                        {
                            col.Item().LineHorizontal(1);
                            col.Item().Text("История изменений").SemiBold().FontSize(14);

                            foreach (var h in history)
                            {
                                col.Item().Text($"{h.PerformedAt:dd.MM.yyyy HH:mm} — {h.Action}" +
                                    (h.OldStatus != null ? $" ({h.OldStatus} → {h.NewStatus})" : ""));
                            }
                        }
                    });

                    page.Footer().AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Страница ");
                            x.CurrentPageNumber();
                            x.Span(" из ");
                            x.TotalPages();
                        });
                });
            });

            var pdfBytes = pdf.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"request-{id}.pdf");
        }
    }
}