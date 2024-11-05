using Microsoft.AspNetCore.Mvc;
using AuthorizeNet.Api.Controllers;
using AuthorizeNet.Api.Contracts.V1;
using AuthorizeNet.Api.Controllers.Bases;
using PaymentApi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PaymentApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentController> _logger; // Declare a logger

        // Inject IConfiguration and ILogger through the constructor
        public PaymentController(IConfiguration configuration, ILogger<PaymentController> logger)
        {
            _configuration = configuration;
            _logger = logger; // Initialize the logger
        }

        [HttpPost("get-token")]
        public IActionResult GetToken([FromBody] PaymentRequest request)
        {
            _logger.LogInformation("Received payment token request for {FirstName} with amount {Amount}", request.FirstName, request.Amount);

            if (string.IsNullOrEmpty(request.Amount) || string.IsNullOrEmpty(request.FirstName) || string.IsNullOrEmpty(request.LastName))
            {
                _logger.LogWarning("Validation failed: Amount, first name, and last name are required.");
                return BadRequest(new { success = false, message = "Amount, first name, and last name are required." });
            }

            // Set the environment for Authorize.Net API
            ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment = AuthorizeNet.Environment.SANDBOX; // Change to PRODUCTION when ready

            var merchantAuthentication = new merchantAuthenticationType()
            {
                name = _configuration["MerchantLoginId"],
                Item = _configuration["MerchantTransactionKey"],
                ItemElementName = ItemChoiceType.transactionKey
            };

            var transactionRequest = new transactionRequestType()
            {
                transactionType = transactionTypeEnum.authCaptureTransaction.ToString(),
                amount = decimal.Parse(request.Amount),
                billTo = new customerAddressType()
                {
                    firstName = request.FirstName,
                    lastName = request.LastName,
                    zip = request.Zip,
                    country = "USA"
                }
            };

            var userField = new AuthorizeNet.Api.Contracts.V1.userField()
            {
                name = "Memo",
                value = request.Memo ?? "none"
            };

            var userFieldsArray = new AuthorizeNet.Api.Contracts.V1.userField[] { userField };
            transactionRequest.userFields = userFieldsArray;

            var hostedPaymentSettingsArray = new[]
            {
                new settingType() { settingName = "hostedPaymentButtonOptions", settingValue = "{\"text\": \"Pay\"}" },
                new settingType() { settingName = "hostedPaymentOrderOptions", settingValue = "{\"show\": false}" },
                new settingType() { settingName = "hostedPaymentBillingAddressOptions", settingValue = "{\"show\": true}" },
                new settingType() { settingName = "hostedPaymentShippingAddressOptions", settingValue = "{\"show\": false}" },
                new settingType()
                {
                    settingName = "hostedPaymentReturnOptions",
                    settingValue = Newtonsoft.Json.JsonConvert.SerializeObject(new
                    {
                        url = _configuration["PaymentContinueUrl"],
                        cancelUrl = _configuration["PaymentCancelUrl"],
                        showReceipt = true
                    })
                }
            };

            var getRequest = new getHostedPaymentPageRequest()
            {
                merchantAuthentication = merchantAuthentication,
                transactionRequest = transactionRequest,
                hostedPaymentSettings = hostedPaymentSettingsArray
            };

            var controller = new getHostedPaymentPageController(getRequest);
            controller.Execute();

            var apiResponse = controller.GetApiResponse();

            if (apiResponse != null && apiResponse.messages.resultCode == messageTypeEnum.Ok)
            {
                _logger.LogInformation("Successfully retrieved payment token: {Token}", apiResponse.token);
                return Ok(new { success = true, token = apiResponse.token });
            }
            else if (apiResponse != null)
            {
                _logger.LogError("Error retrieving payment token: {Error}", apiResponse.messages.message[0].text);
                return StatusCode(500, new { success = false, error = apiResponse.messages.message[0].text });
            }

            _logger.LogError("Null response received from payment API.");
            return StatusCode(500, new { success = false, error = "Null response received" });
        }
    }
}