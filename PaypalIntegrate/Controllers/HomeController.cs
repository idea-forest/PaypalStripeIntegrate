using Microsoft.AspNetCore.Mvc;
using PaypalIntegrate.Models;
using Stripe.Checkout;
using Stripe;
using System.Diagnostics;
using Microsoft.Extensions.Options;

namespace PaypalIntegrate.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        
        private readonly StripeSettings _stripeSettings;
        public string SessionId { get; set; } = string.Empty;
        
        public HomeController(ILogger<HomeController> logger, IOptions<StripeSettings> stripeSettings)
        {
            _logger = logger;
            _stripeSettings = stripeSettings.Value;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }


        public IActionResult CreateCheckoutSession(IFormCollection collection)
        {
            var amount = Int32.Parse(collection["amount"].ToString());
            var currency = "usd"; // Currency code
            var successUrl = "https://localhost:7033/Home/success";
            var cancelUrl = "https://localhost:7033/Home/cancel";
            StripeConfiguration.ApiKey = _stripeSettings.SecretKey;

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                    "card"
                },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = amount * 100,  // Amount in smallest currency unit (e.g., cents)
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = "Product Name",
                                Description = "Product Description"
                            }
                        },
                        Quantity = 1
                    }
                },
                Metadata = new Dictionary<string, string>() {
                    {  "userid" , "000000000000" }
                },
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl
            };

            var service = new SessionService();
            var session = service.Create(options);
            SessionId = session.Id;
            Console.WriteLine($"===================Processing {SessionId}");
            return Redirect(session.Url);
        }

        // This is your Stripe CLI webhook secret for testing your endpoint locally.
        const string endpointSecret = "whsec_715cf98099463aeaddb0a3975c67992e5f08820b3b5132b9a588c4ad6ec679ed";

        public async Task<IActionResult> WebHook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            try
            {
                var stripeEvent = EventUtility.ConstructEvent(json,
                    Request.Headers["Stripe-Signature"], endpointSecret);
                Console.WriteLine("event type: {0}", stripeEvent.Type);
                // Handle the event
                if (stripeEvent.Type == Events.PaymentIntentSucceeded)
                {
                    Console.WriteLine("=================================Successed");
                }else if(stripeEvent.Type == Events.CheckoutSessionCompleted)
                {
                    Console.WriteLine(json);
                }
                // ... handle other event types
                else if(stripeEvent.Type == Events.PaymentIntentCreated)
                {
                    
                }
                return Ok();
            }
            catch (StripeException e)
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> success()
        {

            return View("Index");
        }

        public IActionResult cancel()
        {
            return View("Index");
        }

    }
}
