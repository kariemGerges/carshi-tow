using CarshiTow.Domain.Entities;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;

namespace CarshiTow.Infrastructure.Billing;

internal static class InvoicePdfBuilder
{
    public static byte[] BuildSimpleGstInvoice(Transaction tx, PhotoPack pack)
    {
        var doc = new PdfDocument();
        doc.Info.Title = "Tax Invoice — CRASHI-TOW";
        var page = doc.AddPage();
        page.Width = XUnit.FromPoint(612);
        page.Height = XUnit.FromPoint(792);

        using var gfx = XGraphics.FromPdfPage(page);
        var font = new XFont("Helvetica", 11, XFontStyle.Regular);
        var bold = new XFont("Helvetica", 12, XFontStyle.Bold);
        var y = 40d;
        const double left = 40;
        const double line = 16;

        void Line(string text, XFont? f = null)
        {
            gfx.DrawString(text, f ?? font, XBrushes.Black, new XRect(left, y, page.Width - 80, line), XStringFormats.TopLeft);
            y += line;
        }

        Line("CRASHI-TOW — Tax Invoice", bold);
        Line($"Invoice / transaction id: {tx.Id:D}");
        Line($"Issue date (UTC): {tx.CreatedAtUtc:yyyy-MM-dd HH:mm}Z");
        Line($"Insurer email: {tx.InsurerEmail}");
        Line($"Vehicle: {pack.VehicleRego} — {pack.VehicleMake} {pack.VehicleModel} ({pack.VehicleYear})");
        Line($"Photo pack id: {pack.Id:D}");
        y += line;
        Line($"Total charged (incl. GST where applicable): AUD {tx.TotalAmountCents / 100m:0.00}", bold);
        Line($"Platform fee component: AUD {tx.PlatformFeeCents / 100m:0.00}");
        Line($"Tow yard component: AUD {tx.TowYardAmountCents / 100m:0.00}");
        Line($"Stripe payment intent: {tx.StripePaymentIntentId}");

        using var ms = new MemoryStream();
        doc.Save(ms, false);
        return ms.ToArray();
    }
}
