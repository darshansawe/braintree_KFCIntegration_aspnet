using Braintree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BraintreeASPExample.Controllers
{
    public class CheckoutsController : Controller
    {
        public IBraintreeConfiguration config = new BraintreeConfiguration();

        public static readonly TransactionStatus[] transactionSuccessStatuses = {
                                                                                    TransactionStatus.AUTHORIZED,
                                                                                    TransactionStatus.AUTHORIZING,
                                                                                    TransactionStatus.SETTLED,
                                                                                    TransactionStatus.SETTLING,
                                                                                    TransactionStatus.SETTLEMENT_CONFIRMED,
                                                                                    TransactionStatus.SETTLEMENT_PENDING,
                                                                                    TransactionStatus.SUBMITTED_FOR_SETTLEMENT
                                                                                };

        public ActionResult New()
        {
            var gateway = config.GetGateway();
            var clientToken = gateway.ClientToken.Generate(
                    new ClientTokenRequest
                    {
                        CustomerId = "a68d1c1a-7c6c-46d3-9208-4fcf38727912"
                    }
                );
            ViewBag.ClientToken = clientToken;
            return View();
        }

        public ActionResult Create()
        {
            var gateway = config.GetGateway();
            decimal amount;

            try
            {
                amount = Convert.ToDecimal(Request["amount"]);
            }
            catch (FormatException e)
            {
                TempData["Flash"] = "Error: 81503: Amount is an invalid format.";
                return RedirectToAction("New");
            }

            var nonce = Request["payment_method_nonce"];
            //var nonce = "fake-processor-declined-visa-nonce";

            var request = new TransactionRequest
            {
                Amount = amount,
                PaymentMethodNonce = nonce,
                Descriptor = new DescriptorRequest
                {
                    Name = "KFC* LAB Aloha QSR",
                    Phone = "",
                    Url = "kfc.com.au"
                },
                CustomFields = new Dictionary<string, string>
                {
                    { "externalid", "7777" }, // LOCATED WITHIN ALOHA
                    { "ordertype", "Web Orders Catering" }, // SUBJECT TO KFC REQUIREMENTS
                    { "pickupdate", "1/10/2019" } //SUBJECT TO KFC REQUIREMENTS
                    
                },
                Options = new TransactionOptionsRequest
                {
                    SubmitForSettlement = true
                }
            };

            PaymentMethodNonce paymentMethodNonce = gateway.PaymentMethodNonce.Find(nonce);
            String type = paymentMethodNonce.Type;

            Result<Transaction> result = gateway.Transaction.Sale(request);
            if (result.IsSuccess())
            {
                Transaction transaction = result.Target;
                return RedirectToAction("Show", new { id = transaction.Id });
            }
            else if (result.Transaction != null)
            {
                return RedirectToAction("Show", new { id = result.Transaction.Id } );
            }
            else
            {
                string errorMessages = "";
                foreach (ValidationError error in result.Errors.DeepAll())
                {
                    errorMessages += "Error: " + (int)error.Code + " - " + error.Message + "\n";
                }
                TempData["Flash"] = errorMessages;
                return RedirectToAction("New");
            }

        }

        public ActionResult Show(String id)
        {
            var gateway = config.GetGateway();
            Transaction transaction = gateway.Transaction.Find(id);

            if (transactionSuccessStatuses.Contains(transaction.Status))
            {
                TempData["header"] = "Sweet Success!";
                TempData["icon"] = "success";
                TempData["message"] = "Your test transaction has been successfully processed. See the Braintree API response and try again.";
            }
            else
            {
                 TempData["header"] = "Transaction Failed";
                 TempData["icon"] = "fail";
                 TempData["message"] = "Your test transaction has a status of " + transaction.Status + ". See the Braintree API response and try again.";
             };

            ViewBag.Transaction = transaction;
            return View();
        }
    }
}
