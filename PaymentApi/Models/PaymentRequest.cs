using System.Collections.Generic;
using AuthorizeNet.Api.Contracts.V1;

namespace PaymentApi.Models
{
    public class PaymentRequest
    {
        public string Amount { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Zip { get; set; }
        public string Memo { get; set; }
    }
}
