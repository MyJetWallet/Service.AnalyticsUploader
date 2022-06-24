using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AnaliticsEvents;
using Service.Bitgo.DepositDetector.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class DepositHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<DepositHandleJob> _logger;

		public DepositHandleJob(ILogger<DepositHandleJob> logger,
			ISubscriber<IReadOnlyList<Deposit>> subscriber,
			IAppsFlyerSender sender,
			IClientProfileService clientProfileService, IPersonalDataServiceGrpc personalDataServiceGrpc) :
				base(logger, personalDataServiceGrpc, clientProfileService, sender)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Deposit> messages)
		{
			foreach (Deposit message in messages)
			{
				string clientId = message.ClientId;

				_logger.LogInformation("Handle Deposit message, clientId: {clientId}.", clientId);

				await SendMessage(clientId, new RecieveDepositFromExternalWalletEvent
				{
					Amount = message.Amount,
					Currency = message.AssetSymbol,
					Network = message.Network
				});
			}
		}
	}
}