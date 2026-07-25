using Grained.Application.Common.Exceptions;
using Grained.Application.Common.Interfaces;
using Grained.Application.Payments;
using Grained.Domain.Entities;
using Grained.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Grained.Application.Public;

// Anonymous public campaign view + donation. Only published, active campaigns of active churches.
public class PublicCampaignService(IApplicationDbContext db, IPaymentGateway gateway) : IPublicCampaignService
{
    public async Task<PublicCampaignDto?> GetCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        return await db.Campaigns.AsNoTracking()
            .Where(c => c.Id == campaignId && c.IsActive && c.IsPublished)
            .Select(c => new PublicCampaignDto(
                c.Id, c.Church.Name, c.Title, c.Description, c.TargetAmount,
                c.Donations.Where(d => d.Payment != null && d.Payment.Status == PaymentStatus.Paid).Sum(d => d.Amount),
                c.LogoImageId))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<DonationResultDto> DonateAsync(Guid campaignId, DonationModel model, CancellationToken ct = default)
    {
        var campaign = await db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.IsActive && c.IsPublished, ct)
            ?? throw new ValidationException("This campaign is not accepting donations.");

        if (model.Amount <= 0)
            throw new ValidationException("Enter a donation amount.");

        var amount = decimal.Round(model.Amount, 2);

        var payResult = await gateway.CreatePaymentAsync(
            new PaymentRequest(campaign.ChurchId, amount, "GBP", $"Donation: {campaign.Title}", model.DonorName.Trim(), model.DonorEmail.Trim()),
            ct);

        var payment = new Payment
        {
            ChurchId = campaign.ChurchId,
            Amount = amount,
            Currency = "GBP",
            Provider = payResult.Provider,
            ProviderReference = payResult.Reference,
            Status = payResult.Status,
            PayerName = model.DonorName.Trim(),
            PayerEmail = model.DonorEmail.Trim(),
            PaidAtUtc = payResult.Status == PaymentStatus.Paid ? DateTime.UtcNow : null
        };
        db.Payments.Add(payment);

        var donation = new Donation
        {
            CampaignId = campaign.Id,
            PaymentId = payment.Id,
            DonorName = model.DonorName.Trim(),
            DonorEmail = model.DonorEmail.Trim(),
            Amount = amount,
            Message = model.Message?.Trim(),
            IsNamePublic = model.IsNamePublic
        };
        db.Donations.Add(donation);

        await db.SaveChangesAsync(ct);

        // Recompute raised (includes this donation now that it's saved as Paid).
        var raised = await db.Donations
            .Where(d => d.CampaignId == campaign.Id && d.Payment != null && d.Payment.Status == PaymentStatus.Paid)
            .SumAsync(d => d.Amount, ct);

        return new DonationResultDto(donation.Id, amount, raised, payment.Status.ToString(), payResult.Reference);
    }
}
