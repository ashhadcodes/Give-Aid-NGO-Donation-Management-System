using Give_Aid_NGO_Donation_Management_System.Models.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Give_Aid_NGO_Donation_Management_System.Services.Payments
{
    public class PaymentServiceResolver : IPaymentServiceResolver
    {
        private readonly IEnumerable<IPaymentService> _paymentServices;

        public PaymentServiceResolver(IEnumerable<IPaymentService> paymentServices)
        {
            _paymentServices = paymentServices;
        }

        public IPaymentService GetPaymentService(PaymentProvider provider)
        {
            var service = _paymentServices.FirstOrDefault(s => s.Provider == provider);
            if (service == null)
            {
                throw climateNotSupported(provider);
            }
            return service;
        }

        private static NotSupportedException climateNotSupported(PaymentProvider provider)
        {
            return new NotSupportedException($"Payment provider '{provider}' is not supported or not registered.");
        }
    }
}
