using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using AADB2C.ActivationCode.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AADB2C.ActivationCode.Controllers
{
    [Route("api/[controller]/[action]")]
    public class IdentityController : Controller
    {
        private readonly AppSettingsModel AppSettings;
        Random rnd = new Random();

        // Demo: Inject an instance of an AppSettingsModel class into the constructor of the consuming class, 
        // and let dependency injection handle the rest
        public IdentityController(IOptions<AppSettingsModel> appSettings)
        {
            this.AppSettings = appSettings.Value;
        }

        [HttpPost(Name = "SendVerificationCode")]
        public async Task<ActionResult> SendVerificationCode()
        {
            string input = null;

            // If not data came in, then return
            if (this.Request.Body == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is null", HttpStatusCode.Conflict));
            }

            // Read the input claims from the request body
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                input = await reader.ReadToEndAsync();
            }

            // Check input content value
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is empty", HttpStatusCode.Conflict));
            }

            // Convert the input string into InputClaimsModel object
            InputClaimsModel inputClaims = InputClaimsModel.Parse(input);

            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            if (string.IsNullOrEmpty(inputClaims.email))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("User 'email' is null or empty", HttpStatusCode.Conflict));
            }

            try
            {
                int verificationCode = rnd.Next(11243, 99635);
                SendEmail(inputClaims, verificationCode);

                return StatusCode((int)HttpStatusCode.OK, new B2CResponseModel($"Verification code has been sent to your inbox. Please copy it to the input box below. If you didn't receive the email with the verification code, please click again on the 'Continue' button.", verificationCode.ToString(), HttpStatusCode.OK));
            }
            catch (Exception ex)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel($"General error (REST API): {ex.Message}", HttpStatusCode.Conflict));
            }
        }



        [HttpPost(Name = "verifyCode")]
        public async Task<ActionResult> verifyCode()
        {
            string input = null;

            // If not data came in, then return
            if (this.Request.Body == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is null", HttpStatusCode.Conflict));
            }

            // Read the input claims from the request body
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                input = await reader.ReadToEndAsync();
            }

            // Check input content value
            if (string.IsNullOrEmpty(input))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Request content is empty", HttpStatusCode.Conflict));
            }

            // Convert the input string into InputClaimsModel object
            InputClaimsModel inputClaims = InputClaimsModel.Parse(input);

            if (inputClaims == null)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("Can not deserialize input claims", HttpStatusCode.Conflict));
            }

            if (string.IsNullOrEmpty(inputClaims.systemCode))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("The 'systemCode' is null or empty", HttpStatusCode.Conflict));
            }

            if (string.IsNullOrEmpty(inputClaims.userCode))
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("The 'userCode' is null or empty", HttpStatusCode.Conflict));
            }


            if (inputClaims.userCode.Trim() != inputClaims.systemCode)
            {
                return StatusCode((int)HttpStatusCode.Conflict, new B2CResponseModel("That code is incorrect. Please try again.", HttpStatusCode.Conflict));
            }

            return StatusCode((int)HttpStatusCode.OK, new B2CResponseModel("", HttpStatusCode.OK));
        }

        public void SendEmail(InputClaimsModel inputClaims, int verificationCode)
        {

            // Generate link to next step
            string Body = string.Empty;

            string htmlTemplate = System.IO.File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "Template.html"));

      
            try
            {
                MailMessage mailMessage = new MailMessage();
                mailMessage.To.Add(inputClaims.email);
                mailMessage.From = new MailAddress(AppSettings.SMTPFromAddress);
                mailMessage.Subject = "Verification code";
                mailMessage.Body = string.Format(htmlTemplate, inputClaims.email, verificationCode.ToString());
                mailMessage.IsBodyHtml = true;
                SmtpClient smtpClient = new SmtpClient(AppSettings.SMTPServer, AppSettings.SMTPPort);
                smtpClient.Credentials = new NetworkCredential(AppSettings.SMTPUsername, AppSettings.SMTPPassword);
                smtpClient.EnableSsl = AppSettings.SMTPUseSSL;
                smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                smtpClient.Send(mailMessage);

                Console.WriteLine("Email sent");

            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}
