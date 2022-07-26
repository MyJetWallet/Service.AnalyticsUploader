using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using Service.AnalyticsUploader.Domain;
using Service.AnalyticsUploader.Domain.Models.AmplitudeEvents;
using Service.AnalyticsUploader.Domain.Models.AppsflyerEvents;
using Service.AnalyticsUploader.Domain.Models.Constants;
using Service.AnalyticsUploader.Domain.NoSql;
using Service.Bitgo.WithdrawalProcessor.Domain.Models;
using Service.ClientProfile.Grpc;
using Service.IndexPrices.Client;
using Service.PersonalData.Grpc;

namespace Service.AnalyticsUploader.Job
{
	public class WithdrawalHandleJob : MessageHandleJobBase
	{
		private readonly ILogger<WithdrawalHandleJob> _logger;

		public WithdrawalHandleJob(ILogger<WithdrawalHandleJob> logger,
			ISubscriber<IReadOnlyList<Withdrawal>> subscriber,
			IAppsFlyerSender appsFlyerSender,
			IPersonalDataServiceGrpc personalDataServiceGrpc,
			IClientProfileService clientProfileService,
			IIndexPricesClient converter,
			IAmplitudeSender amplitudeSender,
			IAnalyticIdToClientManager analyticIdToClientManager) :
				base(logger, personalDataServiceGrpc, clientProfileService, appsFlyerSender, amplitudeSender, converter, analyticIdToClientManager)
		{
			_logger = logger;
			subscriber.Subscribe(HandleEvent);
		}

		private async ValueTask HandleEvent(IReadOnlyList<Withdrawal> messages)
		{
			foreach (Withdrawal message in messages)
			{
				if (message.Status != WithdrawalStatus.Success)
					continue;

				string clientId = message.ClientId;

				_logger.LogInformation("Handle Withdrawal message, clientId: {clientId}.", message.ClientId);

				await ToAmplitude(message, clientId);
				await ToAppsflyer(message, clientId);
			}
		}

		private async Task ToAppsflyer(Withdrawal message, string clientId)
		{
			string network = message.Blockchain;
			string assetSymbol = message.AssetSymbol;
			decimal amount = message.Amount;

			if (message.IsInternal)
			{
				string destinationClientId = message.DestinationClientId;
				string destinationCuid = await GetExternalClientId(destinationClientId);
				if (destinationCuid == null)
				{
					_logger.LogError("DestinationCuid is null, skip uploading");
					return;
				}

				await SendAppsflyerMessage(clientId, new SendTransferByWalletInternalEvent
				{
					Amount = amount,
					Currency = assetSymbol,
					Receiver = destinationCuid,
					Network = network
				});

				await SendAppsflyerMessage(destinationCuid, new RecieveTransferFromInternalWalletEvent
				{
					Amount = amount,
					Currency = assetSymbol,
					Sender = clientId,
					Network = network
				});
			}
			else
			{
				await SendAppsflyerMessage(clientId, new SendTransferByWalletExternalEvent
				{
					Amount = amount,
					Currency = assetSymbol,
					Receiver = message.ToAddress,
					Network = network
				});
			}
		}

		private async Task ToAmplitude(Withdrawal message, string clientId)
		{
			decimal amount = message.FeeAmount;
			string asset = message.FeeAssetSymbol;

			await SendAmplitudeRevenueMessage(clientId, new RevenueEvent
			{
				RevenueVolumeInAsset = amount,
				RevenueAsset = asset,
				RevenueVolumeInUsd = GetAmountUsdValue(amount, asset),
				RevenueType = RevenueType.WithdrawalFee
			});
		}
	}
}