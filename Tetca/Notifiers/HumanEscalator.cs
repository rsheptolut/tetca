using System;
using System.Net.Mail;
using System.Net;

namespace Tetca.Notifiers
{
    /// <summary>
    /// HumanEscalator class is responsible for sending email notifications.
    /// </summary>
    public class HumanEscalator
    {
        public DateTime? lastSent = null;

        /// <summary>
        /// Sends an email notification to the specified recipient.
        /// </summary>
        /// <param name="emails">
        /// A comma-separated list of email addresses to send the notification to. 
        /// </param>
        /// <param name="emailFrom">
        /// The email address from which the notification is sent. 
        /// </param>
        /// <param name="emailFromPass">
        /// The password for the sender's email account. 
        /// </param>
        /// <param name="text">
        /// The body of the email notification. 
        /// </param>
        public void SendNotification(string emails, string emailFrom, string emailFromPass, string text)
        {
            var message = new MailMessage(emailFrom, emails);
            message.Subject = $"{App.Name} notification";
            message.Body = text;
            message.IsBodyHtml = true;

            using var smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential(emailFrom, emailFromPass);
            smtp.EnableSsl = true;
            smtp.Send(message);
        }
    }
}
