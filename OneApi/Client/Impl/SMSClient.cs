using OneApi.Config;
using OneApi.Model;

namespace OneApi.Client.Impl
{
    public class SMSClient
    {
        private CustomerProfileClient customerProfileClient = null;
        private SMSMessagingClient smsMessagingClient = null;
        private HLRClient hlrClient = null;
        private USSDClient ussdClient = null;
        private NetworksClient networksClient = null;
        private Configuration configuration = null;

        //*************************SMSClient initialization***********************************************************************************************************************************************
        /// <summary>
        /// Initialize SMS client using specified 'configuration' parameter </summary>
        /// <param name="configuration"> - parameter containing OneAPI configuration data </param>
        public SMSClient(Configuration configuration)
        {
            this.configuration = configuration;

            //Initialize Clients   
            customerProfileClient = new CustomerProfileClientImpl(configuration, onLogin, onLogout);
            smsMessagingClient = new SMSMessagingClientImpl(configuration);
            hlrClient = new HLRClientImpl(configuration);
            ussdClient = new USSDClientImpl(configuration);
            networksClient = new NetworksClientImpl(configuration);
        }

        /// <summary>
        /// Customer Profile client </summary>
        /// <returns> CustomerProfileClient </returns>
        public CustomerProfileClient CustomerProfileClient
        {
            get { return customerProfileClient; }
        }

        /// <summary>
        /// SMS Messaging client </summary>
        /// <returns> SMSMessagingClient </returns>
        public SMSMessagingClient SmsMessagingClient
        {
            get { return smsMessagingClient; }
        }

        /// <summary>
        /// HLR client </summary>
        /// <returns> HLRClient </returns>
        public HLRClient HlrClient
        {
            get { return hlrClient; }
        }

        /// <summary>
        /// USSD client
        /// </summary>
        public USSDClient UssdClient
        {
            get { return ussdClient; }
        }

        /// <summary>
        /// Networks client
        /// </summary>
        public NetworksClient NetworksClient
        {
            get { return networksClient; }
        }

        private void onLogin(LoginResponse response)
        {
            if ((response != null) && (response.IbAuthCookie.Length > 0))
            {
                configuration.Authentication.Type = OneApi.Model.Authentication.AuthType.IBSSO;
                configuration.Authentication.IbssoToken = response.IbAuthCookie;
            }
        }

        private void onLogout()
        {
            configuration.Authentication.IbssoToken = "";
        }
    }
}