using Give_Aid_NGO_Donation_Management_System.Models.Entities;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public interface IPaymentServiceResolver
    {
        IPaymentService GetPaymentService(PaymentProvider provider);
    }
}
