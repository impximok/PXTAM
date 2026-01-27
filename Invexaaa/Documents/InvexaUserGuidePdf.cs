using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Invexaaa.Documents
{
    public class InvexaUserGuidePdf : IDocument
    {
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header()
                    .Row(row =>
                    {
                        row.RelativeItem()
                           .Text("Invexa – Smart Inventory Intelligence System")
                           .FontSize(16)
                           .SemiBold();

                        row.ConstantItem(100)
                           .AlignRight()
                           .Text("User Guide");
                    });

                page.Content().PaddingVertical(20).Column(col =>
                {
                    col.Spacing(12);

                    Section(col, "Help & User Guide",
                        "This document explains how to use the Invexa Smart Inventory Intelligence System. " +
                        "All information reflects the system’s actual functionality and user permissions.");

                    Section(col, "1. Creating Items",
                        "Items are managed through the Items module. Only Admin and Manager roles are allowed " +
                        "to create items. When a new item is created, an inventory record is automatically created " +
                        "with zero quantity.");

                    Section(col, "2. Adding Stock",
                        "Stock is added through the Stock module using batch-based stock entry. Each stock batch " +
                        "requires an expiry date. Inactive items cannot be restocked.");

                    Section(col, "3. Inventory Status",
                        "Inventory health is calculated automatically:\n" +
                        "- Critical: Quantity equals zero\n" +
                        "- Low Stock: Quantity at or below reorder level\n" +
                        "- Reorder Required: Quantity below reorder point");

                    Section(col, "4. Expiry Tracking",
                        "Expiry status is calculated dynamically:\n" +
                        "- Expired: Expiry date before today\n" +
                        "- Near Expiry: Expiry date within 7 or 30 days depending on filter\n" +
                        "- Safe: Expiry date more than 30 days away");

                    Section(col, "5. Alerts Dashboard",
                        "The Alerts module highlights expired batches, near-expiry items, low stock items, and " +
                        "reorder-required items based on real-time inventory data.");

                    Section(col, "6. Roles & Permissions",
                        "Admin and Manager users can manage items and stock. Staff users have limited access. " +
                        "Inactive items cannot be adjusted or restocked.");
                });

                // FOOTER WITH PAGE NUMBERS
                page.Footer()
                    .AlignCenter()
                    .Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                        text.Span(" • Generated ");
                        text.Span(DateTime.Now.ToString("dd MMM yyyy"));
                    });
            });
        }

        private void Section(ColumnDescriptor col, string title, string body)
        {
            col.Item().Text(title).SemiBold();
            col.Item().Text(body);
        }
    }
}
