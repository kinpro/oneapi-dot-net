using System.Text;
using System.Net;
using System.Web;
using System.Collections.Generic;
using OneApi.Listeners;
using OneApi.Config;
using OneApi.Exceptions;
using OneApi.Model;

namespace OneApi.Client.Impl
{  

	public class HLRClientImpl : OneAPIBaseClientImpl, HLRClient
	{
		private const string DATA_CONNECTION_PROFILE_URL_BASE = "/terminalstatus/queries";
		private const string DATA_response_PROFILE_SUBSCRIPTION_URL_BASE = "/smsmessaging/hlr/subscriptions";
      
        private volatile IList<HLRNotificationsListener> hlrMessagePushListenerList = null;
        private PushServerSimulator hlrPushServerSimulator;

		//*************************DataresponseProfileClientImpl Initialization******************************************************************************************************************************************************
		public HLRClientImpl(Configuration configuration) : base(configuration)
		{
		}

		//*************************DataresponseProfileClientImpl public******************************************************************************************************************************************************		
		/// <summary>
		/// Get asynchronously the customer’s roaming status for a single network-connected mobile device  (HLR) </summary>
		/// <param name="address"> (mandatory) mobile device number being queried </param>
		/// <param name="notifyURL"> (mandatory) URL to receive the roaming status asynchronously </param>
        /// <param name="clientCorrelator"> (optional) Active only if notifyURL is specified, otherwise ignored. Uniquely identifies this request. If there is a communication failure during the request, using the same clientCorrelator when retrying the request helps the operator to avoid call the same request twice. </param>
        /// <param name="callbackData"> (optional) Active only if notifyURL is specified, otherwise ignored. This is custom data to pass back in notification to notifyURL, so you can use it to identify the request or any other useful data, such as a function name. </param>
        public void QueryHLRAsync(string address, string notifyURL, string clientCorrelator, string callbackData)
		{
			if (notifyURL == null || notifyURL.Length == 0)
			{
				throw new RequestException("'notifiyURL' parmeter is mandatory using asynchronous method.");
			}

			StringBuilder urlBuilder = new StringBuilder(DATA_CONNECTION_PROFILE_URL_BASE);
			urlBuilder.Append("/roamingStatus?address=");
			urlBuilder.Append(HttpUtility.UrlEncode(address));
			urlBuilder.Append("&includeExtendedData=true");
			urlBuilder.Append("&notifyURL=");
			urlBuilder.Append(HttpUtility.UrlEncode(notifyURL));

            if (clientCorrelator != null && clientCorrelator.Length > 0)
            {
                urlBuilder.Append("&clientCorrelator=");
                urlBuilder.Append(HttpUtility.UrlEncode(clientCorrelator));
            }

            if (callbackData != null && callbackData.Length > 0)
            {
                urlBuilder.Append("&callbackData=");
                urlBuilder.Append(HttpUtility.UrlEncode(callbackData));
            }

			HttpWebResponse response = ExecuteGet(AppendMessagingBaseUrl(urlBuilder.ToString()));
            validateResponse(response, RESPONSE_CODE_200_OK);
		}

        /// <summary>
		/// Get asynchronously the customer’s roaming status for a single network-connected mobile device  (HLR) </summary>
		/// <param name="address"> (mandatory) mobile device number being queried </param>	
        /// <param name="notifyURL"> (mandatory) URL to receive the roaming status asynchronously </param>
        public void QueryHLRAsync(string address, string notifyURL)
        {
             QueryHLRAsync(address, notifyURL, null, null);
        }

		/// <summary>
		/// Get synchronously the customer’s roaming status for a single network-connected mobile device (HLR) </summary>
		/// <param name="address"> (mandatory) mobile device number being queried </param>
        /// <returns> RoamingSync </returns>
        public RoamingSync QueryHLRSync(string address)
		{
			StringBuilder urlBuilder = new StringBuilder(DATA_CONNECTION_PROFILE_URL_BASE);
			urlBuilder.Append("/roamingStatus?address=");
			urlBuilder.Append(HttpUtility.UrlEncode(address));
			urlBuilder.Append("&includeExtendedData=true");

			HttpWebResponse response = ExecuteGet(AppendMessagingBaseUrl(urlBuilder.ToString()));
            return Deserialize<RoamingSync>(response, RESPONSE_CODE_200_OK, "roaming");
		}

        /// <summary>
        /// Convert JSON to RoamingNotification </summary>
        /// <returns> RoamingNotification </returns>
        public RoamingNotification ConvertJsonToRoamingNotification(string json)
        {
            return ConvertJsonToObject<RoamingNotification>(json, "terminalRoamingStatusList");
        }

		/// <summary>
		/// Start subscribing to HLR delivery notifications over OneAPI </summary>
		/// <param name="subscribeToHLRDeliveryNotificationsRequest"> </param>
		/// <returns> string - Subscription Id </returns>
		public string SubscribeToHLRDeliveryNotifications(SubscribeToHLRDeliveryNotificationsRequest subscribeToHLRDeliveryNotificationsRequest)
		{
			HttpWebResponse response = ExecutePost(AppendMessagingBaseUrl(DATA_response_PROFILE_SUBSCRIPTION_URL_BASE), subscribeToHLRDeliveryNotificationsRequest);
            DeliveryReceiptSubscription deliveryReceiptSubscription = Deserialize<DeliveryReceiptSubscription>(response, RESPONSE_CODE_201_CREATED, "deliveryReceiptSubscription");
            return GetIdFromResourceUrl(deliveryReceiptSubscription.ResourceURL);
        }

		/// <summary>
		/// Get HLR delivery notifications subscriptions by subscription id </summary>
		/// <param name="subscriptionId"> </param>
		/// <returns> DeliveryReportSubscription[] </returns>
		public DeliveryReportSubscription[] GetHLRDeliveryNotificationsSubscriptionsById(string subscriptionId)
		{
			StringBuilder urlBuilder = (new StringBuilder(DATA_response_PROFILE_SUBSCRIPTION_URL_BASE)).Append("/");
			urlBuilder.Append(HttpUtility.UrlEncode(subscriptionId));

			HttpWebResponse response = ExecuteGet(AppendMessagingBaseUrl(urlBuilder.ToString()));
            return Deserialize<DeliveryReportSubscription[]>(response, RESPONSE_CODE_200_OK, "deliveryReceiptSubscriptions");
		}

		/// <summary>
		/// Stop subscribing to HLR notifications over OneAPI </summary>
		/// <param name="subscriptionId"> (mandatory) contains the subscriptionId of a previously created HLR delivery receipt subscription </param>
		public void RemoveHLRDeliveryNotificationsSubscription(string subscriptionId)
		{
			StringBuilder urlBuilder = (new StringBuilder(DATA_response_PROFILE_SUBSCRIPTION_URL_BASE)).Append("/");
			urlBuilder.Append(HttpUtility.UrlEncode(subscriptionId));

			HttpWebResponse response = ExecuteDelete(AppendMessagingBaseUrl(urlBuilder.ToString()));
			validateResponse(response, RESPONSE_CODE_204_NO_CONTENT);
		}

        /// <summary>
        /// Add OneAPI PUSH 'HLR' Notifications listener and start push server simulator
        /// <param name="listener"> - (new HLRNotificationsListener) </param>
        public void AddPushHlrNotificationsListener(HLRNotificationsListener listener)
        {
            if (listener == null)
            {
                return;
            }

            if (hlrMessagePushListenerList == null)
            {
                hlrMessagePushListenerList = new List<HLRNotificationsListener>();
            }

            hlrMessagePushListenerList.Add(listener);

            StartPushServerSimulator();

            if (LOGGER.IsInfoEnabled)
            {
                LOGGER.Info("Listener is successfully added, push server is started and is waiting for HLR Notifications");
            }
        }

        /// <summary>
        /// Returns HLR PUSH Notifications Listeners list
        /// </summary>
        public IList<HLRNotificationsListener> HlrPushListeners
        {
            get
            {
                return hlrMessagePushListenerList;
            }
        }

        /// <summary>
        /// Remove PUSH HLR Notifications listeners and stop server
        /// </summary>
        public void RemovePushHlrNotificationsListeners()
        {
            StopPushServerSimulator();
            hlrMessagePushListenerList = null;
           
            if (LOGGER.IsInfoEnabled)
            {
                LOGGER.Info("Hlr Notifications Listeners are successfully removed.");
            }
        }

        private void StartPushServerSimulator()
        {
            if (hlrPushServerSimulator == null)
            {
                hlrPushServerSimulator = new PushServerSimulator(this);
            }

            hlrPushServerSimulator.Start(Configuration.HlrPushServerSimulatorPort);
        }

        private void StopPushServerSimulator()
        {
            if (hlrPushServerSimulator != null)
            {
                hlrPushServerSimulator.Stop(); 
            }
        }
    }
}