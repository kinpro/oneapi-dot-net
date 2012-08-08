using System;
using System.Net;
using System.Text;
using System.IO;
using log4net.Config;
using OneApi.Config;
using OneApi.Client.Impl;
using OneApi.Exceptions;
using OneApi.Model;
using OneApi.Examples;
using OneApi.Listeners;

namespace OneApi.Scenarios
{

    /**
     * To run this example follow these 4 steps:
     *
     *  1.) Download 'Parseco C# library' - available at www.github.com/parseco   or   www.parseco.com/apis    
     *
     *  2.) Open 'OneApi.sln' in 'Visual Studio 2010' and locate 'OneApiExamples' project    
     *
     *  3.) Open 'Scenarios.PushInboundMessagesNotification' class to edit where you should populate the following fields: 
     *		'destinationAddress'    'notificationFormat'
     *		'username'              'notifyUrl'           
     *		'password'              'criteria' 
     *
     *  4.) Run the 'OneApiExample' project, where an a example list with ordered numbers will be displayed in the console. 
     *       There you will enter the appropriate example number in the console and press 'Enter' key 
     *       on which the result will be displayed in the Console.
     *
     *  Note: 'Inbound Message Notifications' push server is started automatically by adding 'InboundMessageNotificationsListener'
     *  using the 'AddPushInboundMessageListener' method. Default server port is 3001 and it can be changed by set the 
     *  'Configuration' property 'InboundMessagesPushServerSimulatorPort'. Used port should match the one used in the 'notifyUrl' property when 
     *  subscribing for the notifications using the 'SubscribeToInboundMessagesNotifications' method.
     **/

    public class PushInboundMessagesNotification 
    {
        private static string username = "FILL USERNAME HERE !!!";
        private static string password = "FILL PASSWORD HERE !!!";
        private static string destinationAddress = "";
        private static string notifyUrl = ""; //e.g. "http://127.0.0.1:3001/" 3001=Default port for 'Inbound Messages Notifications' server simulator
        private static string criteria = "";
        private static string notificationFormat = "JSON"; 
           
        public static void Execute()
        {
            //Configure in the 'app.config' which Logger levels are enabled(all levels are enabled in the example)
            //Check http://logging.apache.org/log4net/release/manual/configuration.html for more informations about the log4net configuration
            XmlConfigurator.Configure(new FileInfo("OneApiExamples.exe.config"));


            //Initialize Configuration object 
            Configuration configuration = new Configuration(username, password);           

            //Initialize SMSClient using the Configuration object
            SMSClient smsClient = new SMSClient(configuration);

            try
            {
                //Login user
                LoginResponse loginResponse = smsClient.CustomerProfileClient.Login();
                if (loginResponse.Verified == false)
                {
                    Console.WriteLine("User is not verified!");
                    return;
                }

                //Add listener(start push server and wait for the Inbound Message Notifications)    
                smsClient.SmsMessagingClient.AddPushInboundMessageNotificationsListener(new InboundMessageNotificationsListener(OnMessageReceived));

                //Subscribe to the Inbound Message notifications
                string subscriptionId = smsClient.SmsMessagingClient.SubscribeToInboundMessagesNotifications(new SubscribeToInboundMessagesRequest(destinationAddress, notifyUrl, criteria, notificationFormat, "", ""));
                Console.WriteLine("Subscription Id: " + subscriptionId);

                //Waiting 2 minutes for the  'Inbound Message Notification' before close the server connection and canceling subscription.   
                Console.WriteLine("Waiting 2 minutes for the Inbound Message Notifications.. after that subscription will be removed and push server connection will be closed."); 
                System.Threading.Thread.Sleep(120000);

                //Remove Inbound Message Notifications subscription
                smsClient.SmsMessagingClient.RemoveInboundMessagesNotificationsSubscription(subscriptionId);

                //Logout user
                smsClient.CustomerProfileClient.Logout();

                //Remove Inbound Message Notification Listeners and stop the server
                smsClient.SmsMessagingClient.RemovePushInboundMessageNotificationsListeners();    
            }
            catch (RequestException e)
            {
                Console.WriteLine("Request Exception: " + e.Message);
            }
        }

        //Handle pushed Inbound Messages Notification
        private static void OnMessageReceived(InboundSMSMessageList smsMessageList)
        {
            Console.WriteLine("Inbound Messages " + string.Join("Inbound Message: ", (Object[])smsMessageList.InboundSMSMessage));             
        }
    }
}